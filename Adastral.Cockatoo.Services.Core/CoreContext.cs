using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NLog;
using Adastral.Cockatoo.Common;
using FluentScheduler;
using System.Data;
using System.Reflection;
using kate.shared.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Sentry;

namespace Adastral.Cockatoo.Services;

public class CoreContext : ICoreContext
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public static CoreContext? Instance { get; private set; }
    public static JsonSerializerOptions SerializerOptions => BaseService.SerializerOptions;
    /// <inheritdoc />
    public string Id { get; private set; }
    public CockatooConfig Config { get; private set; }
    public CoreContext()
    {
        if (Instance != null)
        {
            throw new Exception("An instance of CoreContext exists already.");
        }
        Id = Guid.NewGuid().ToString();
        StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Instance = this;
        JobManager.Initialize();
        if (FeatureFlags.SentryEnable)
        {
            if (string.IsNullOrEmpty(FeatureFlags.SentryDSN))
            {
                Console.Error.WriteLine($"[CoreContext] Not initializing Sentry since {nameof(FeatureFlags)}.{nameof(FeatureFlags.SentryDSN)} is null/empty");
            }
            else
            {
                SentrySdk.Init(options =>
                {
                    options.Dsn = FeatureFlags.SentryDSN;
                    #if DEBUG
                    options.Debug = true;
                    #else
                    options.Debug = false;
                    #endif
                    options.AutoSessionTracking = true;
                    // A fixed sample rate of 1.0 - 100% of all transactions are getting sent
                    options.TracesSampleRate = 1.0f;
                    // A sample rate for profiling - this is relative to TracesSampleRate
                    options.ProfilesSampleRate = 1.0f;
                });
            }
        }
        Config = new(true);
        Config.Read();
        JobManager.AddJob(() =>
        {
            var logger = LogManager.GetLogger("Job_ConfigRead");
            var slug = $"{GetType().Name}-Job_ConfigRead";
            logger.Debug($"{slug}|Running..");
            var checkInId = SentrySdk.CaptureCheckIn(slug, CheckInStatus.InProgress);
            try
            {
                Config.Read();
                SentrySdk.CaptureCheckIn(slug, CheckInStatus.Ok, checkInId);
                logger.Debug($"{slug}|Job completed! ({checkInId})");
            }
            catch (Exception ex)
            {
                logger.Error($"{slug}|Failed to run scheduled task CockatooConfig.Read\n{ex}");
                SentrySdk.CaptureException(ex);
                SentrySdk.CaptureCheckIn(slug, CheckInStatus.Error, checkInId);
            }
        }, schedule =>
        {
            schedule.ToRunOnceAt(DateTime.Now.AddSeconds(5))
                .AndEvery(5).Minutes();
        });
    }

    /// <inheritdoc />
    public async Task MainAsync(string[] args, Func<IServiceCollection, Task> beforeServiceBuild, IServiceCollection? customServiceCollection = null)
    {
        if (StartTimestamp == 0)
            StartTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var objectSerializer = new ObjectSerializer(type => ObjectSerializer.DefaultAllowedTypes(type) || (type.FullName?.StartsWith("Adastral.Cockatoo") ?? false));
        BsonSerializer.RegisterSerializer(objectSerializer);

        InitMongoClient();
        InitServices(beforeServiceBuild, customServiceCollection, customServiceCollection == null);

        if (AlternativeMain != null)
        {
            await AlternativeMain(args);
        }
    }

    /// <summary>
    /// Find all Types in all assemblies in <see cref="AppDomain.CurrentDomain"/>, and all the <paramref name="additionalAssemblies"/> passed through.
    /// </summary>
    /// <param name="additionalAssemblies">Array of assemblies to also look through for allowed types</param>
    private static Dictionary<StartupStepGroupKind, List<ICoreStartupStep>> FindStartupSteps(params Assembly[] additionalAssemblies)
    {
        var result = new Dictionary<StartupStepGroupKind, List<ICoreStartupStep>>();
        foreach (var k in GeneralHelper.GetEnumList<StartupStepGroupKind>())
            result[k] = [];
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Concat(additionalAssemblies))
        {
            foreach (var type in asm.GetTypes())
            {
                if (!type.IsClass || !type.IsPublic || !typeof(ICoreStartupStep).IsAssignableFrom(type))
                    continue;
                var attr = type.GetCustomAttribute<StartupStepAttribute>();
                if (attr == null)
                {
                    continue;
                }

                var instance = Activator.CreateInstance(type);
                result[attr.StepGroup].Add((ICoreStartupStep)instance!);
            }
        }

        return result;
    }

    /// <summary>
    /// When not null, it is called in <see cref="MainAsync"/>.
    /// </summary>
    public Func<string[], Task>? AlternativeMain { get; set; }
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    /// <remarks>
    /// Created after <see cref="InjectServices"/> is called in <see cref="MainAsync"/>
    /// </remarks>
    public IServiceProvider? Services { get; private set; }
    /// <summary>
    /// When <see cref="InjectServices" path="/param[@name='buildServiceCollection']"/> is <see langword="false"/>, then this function is used to build the service collection.
    /// </summary>
    public Func<IServiceCollection, IServiceProvider>? BuildServiceCollectionAction { get; set; }
    public T GetRequiredService<T>() where T : notnull
    {
        if (Services == null)
        {
            throw new NoNullAllowedException($"Called before CoreContext was initialized!");
        }
        return Services.GetRequiredService<T>();
    }
    public IEnumerable<T> GetServices<T>() where T : notnull
    {
        if (Services == null)
        {
            throw new NoNullAllowedException($"Called before CoreContext was initialized!");
        }
        return Services.GetServices<T>();
    }
    public IEnumerable<T> GetRequiredServicesImplementing<T>() where T : notnull
    {
        if (Services == null)
        {
            throw new NoNullAllowedException($"Called before CoreContext was initialized!");
        }
        foreach (var x in RegisteredBaseControllers)
        {
            if (typeof(T).IsAssignableFrom(x))
            {
                yield return (T)Services.GetRequiredService(x);
            }
        }
    }

    /// <summary>
    /// UTC of <see cref="DateTimeOffset.ToUnixTimeSeconds()"/>
    /// </summary>
    public long StartTimestamp { get; private set; } = 0;
    public MongoClient? MongoDB { get; private set; }

    /// <summary>
    /// Initialize MongoDB Client (<see cref="MongoDB"/>)
    /// </summary>
    private void InitMongoClient()
    {
        try
        {
            _log.Debug("Connecting to MongoDB");
            var connectionSettings = MongoClientSettings.FromConnectionString(Config.MongoDB.ConnectionString);
            connectionSettings.AllowInsecureTls = true;
            connectionSettings.MaxConnectionPoolSize = 500;
            connectionSettings.WaitQueueSize = 2000;
            connectionSettings.MaxConnectionPoolSize = 8192;
            connectionSettings.LinqProvider = LinqProvider.V3;
            MongoDB = new MongoClient(connectionSettings);
            MongoDB.StartSession();
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to connect to MongoDB Server\n{ex}");
            OnQuit(1);
        }
    }
    public IMongoDatabase? GetDatabase()
    {
        return MongoDB?.GetDatabase(Config.MongoDB.DatabaseName);
    }
    #region Services
    private void InitServices(Func<IServiceCollection, Task> beforeServiceBuild, IServiceCollection? customServiceCollection = null, bool buildServiceCollection = true)
    {
        InjectServices(customServiceCollection ?? new ServiceCollection(), beforeServiceBuild, buildServiceCollection);
    }
    /// <summary>
    /// Initialize all service-related stuff.
    /// </summary>
    /// <param name="services">Service Collection to use. Can be overridden with the <c>customServiceCollection</c> parameter in <see cref="MainAsync"/></param>
    /// <param name="beforeBuild">Function to run before the Service Collection is built, and before <see cref="RegisteredBaseControllers"/> is populated.</param>
    /// <param name="buildServiceCollection">When <see langword="false"/>, then <see cref="BuildServiceCollectionAction"/> is used to convert <paramref name="services"/> into an instance of <see cref="IServiceProvider"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="buildServiceCollection"/> is <see langword="false"/> and <see cref="BuildServiceCollectionAction"/> is <see langword="null"/></exception>
    private void InjectServices(IServiceCollection services, Func<IServiceCollection, Task> beforeBuild, bool buildServiceCollection)
    {
        var mongoDb = GetDatabase();
        if (mongoDb == null)
        {
            _log.Error($"FATAL ERROR!!! CoreContext.GetDatabase() returned null!");
            OnQuit(1);
        }
        services
            .AddSingleton(this)
            .AddSingleton(Config)
            .AddSingleton(mongoDb!)
            .AddSingleton(MongoDB!);

        beforeBuild(services).Wait();
        if (services.All(v => v.ServiceType != typeof(IDistributedCache)))
        {
            services.AddDistributedMemoryCache();
        }

        RegisteredBaseControllers = [];
        foreach (var item in services)
        {
            if (item.ServiceType.IsAssignableTo(typeof(BaseService)) &&
                !RegisteredBaseControllers.Contains(item.ServiceType))
            {
                RegisteredBaseControllers.Add(item.ServiceType);
            }
        }
        if (buildServiceCollection)
        {
            Services = services.BuildServiceProvider();
        }
        else
        {
            if (BuildServiceCollectionAction == null)
            {
                throw new ArgumentException($"{nameof(BuildServiceCollectionAction)} is null when argument {nameof(buildServiceCollection)} is true! Usage of {nameof(CoreContext)} was not implemented properly");
            }
            Services = BuildServiceCollectionAction(services);
        }
        RunServiceInit();
    }
    private List<Type> RegisteredBaseControllers { get; set; } = [];

    private void RunServiceInit()
    {
        AllBaseServices((item) =>
        {
            item.InitializeAsync().Wait();
            return Task.CompletedTask;
        });
        JobManager.Start();
    }
    /// <summary>
    /// For every registered class that extends <see cref="BaseService"/>, call <paramref name="func"/> with the argument as the target controller.
    /// </summary>
    private void AllBaseServices(Func<BaseService, Task> func)
    {
        var taskList = new List<Task>();
        var ins = new List<BaseService>();
        foreach (var service in RegisteredBaseControllers)
        {
            var svc = Services?.GetServices(service);
            foreach (var item in svc ?? [])
            {
                if (item != null && item.GetType().IsAssignableTo(typeof(BaseService)))
                {
                    ins.Add((BaseService)item);
                }
            }
        }
        foreach (var item in ins)
        {
            taskList.Add(new Task(delegate
            {
                func((BaseService)item).Wait();
            }));
        }
        foreach (var i in taskList)
            i.Start();
        Task.WaitAll(taskList.ToArray());
    }
    #endregion
    public void OnQuit(int code)
    {
        Environment.Exit(code);
    }
}
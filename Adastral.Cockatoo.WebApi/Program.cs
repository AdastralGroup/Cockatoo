using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using NLog;
using NLog.Extensions.Logging;
using Logger = NLog.Logger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.Data;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.Services;
using Adastral.Cockatoo.Services.WebApi;
using Adastral.Cockatoo.Services.WebApi.Controllers;
using Adastral.Cockatoo.Services.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Adastral.Cockatoo.WebApi;

public class Program
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    public static string Version => typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
    public static void Main(string[] args)
    {
        _log.Debug("Creating CoreContext");
        var core = new CoreContext();
        Console.WriteLine($"Public URL: {core.Config.PublicUrl}");
        InitializeWebApplication(core, args);
        core.AlternativeMain = async (a) =>
        {
            if (WebApp == null)
            {
                throw new NoNullAllowedException($"{nameof(WebApplication)} is null");
            }
            _log.Debug("Running Server");
            await WebApp.StartAsync();
            await Task.Delay(-1);
        };
        if (WebAppBuilder == null)
        {
            throw new NoNullAllowedException($"{nameof(WebAppBuilder)} is null");
        }
        core.MainAsync(args, (s) =>
        {
            AttributeHelper.InjectControllerAttributes(typeof(BaseService).Assembly, s); // Adastral.Cockatoo.Common
            AttributeHelper.InjectControllerAttributes(typeof(CoreContext).Assembly, s); // Adastral.Cockatoo.Services.Core
            AttributeHelper.InjectControllerAttributes(typeof(S3Service).Assembly, s); // Adastral.Cockatoo.Services
            AttributeHelper.InjectControllerAttributes(typeof(BaseRepository<>).Assembly, s); // Adastral.Cockatoo.DataAccess
            AttributeHelper.InjectControllerAttributes(typeof(EndpointMessages).Assembly, s); // Adastral.Cockatoo.Services.WebApi.Models
            AttributeHelper.InjectControllerAttributes(typeof(AuthRequiredAttribute).Assembly, s); // Adastral.Cockatoo.Services.WebApi
            AttributeHelper.InjectControllerAttributes(typeof(Program).Assembly, s); // Adastral.Cockatoo.WebApi
            return Task.CompletedTask;
        }, WebAppBuilder.Services).Wait();
    }
    private static WebApplication? WebApp = null;
    private static WebApplicationBuilder? WebAppBuilder = null;

    private static void InitializeWebApplication(CoreContext core, string[] args)
    {
        _log.Debug("Creating WebApplication Builder");
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        // Add services to the container.
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options => {
            options.Cookie.IsEssential = true;
            options.Cookie.Name = ".Cockatoo.WebApi.Session";
            options.IdleTimeout = TimeSpan.FromDays(2);
        });
        var pluginAssembly = typeof(AdminGroupApiV1Controller).Assembly;
        builder.Services.AddMvc(options =>
        {
            options.EnableEndpointRouting = false;
        })
                .AddApplicationPart(pluginAssembly);
        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        // add swagger-related stuff
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAuthorization();
        var authBuilder = builder.Services.AddAuthentication(
                options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
            .AddCookie(
                options =>
                {
                    options.LoginPath = "/signin";
                    options.LogoutPath = "/signout";
                    options.ReturnUrlParameter = "redirect";
                });

        // initialize nlog logger
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Trace);
        });
        builder.Services.AddSingleton<ILoggerProvider, NLogLoggerProvider>();
        if (core.Config.Redis.Enable)
        {
            if (string.IsNullOrEmpty(core.Config.Redis.ConnectionString))
            {
                throw new NoNullAllowedException($"{nameof(core.Config.Redis.ConnectionString)} is required when Redis is enabled.");
            }
            builder.Services.AddStackExchangeRedisCache((opts) =>
            {
                opts.Configuration = core.Config.Redis.ConnectionString.Replace("\"", "");
                if (!string.IsNullOrEmpty(core.Config.Redis.InstanceName))
                {
                    opts.InstanceName = core.Config.Redis.InstanceName;
                }
            });
        }
        else
        {
            builder.Services.AddDistributedMemoryCache();
        }
        if (FeatureFlags.SentryEnable)
        {
            if (string.IsNullOrEmpty(FeatureFlags.SentryDSN))
            {
                throw new Exception($"Missing FeatureFlag {nameof(FeatureFlags.SentryDSN)} when {nameof(FeatureFlags.SentryEnable)} is enabled!");
            }
            builder.WebHost.UseSentry(FeatureFlags.SentryDSN);
        }
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.MaxRequestBodySize = long.MaxValue;
            serverOptions.AllowSynchronousIO = true;
        });
        WebAppBuilder = builder;

        core.BuildServiceCollectionAction = (col) => {
            if (col == builder.Services)
            {
                var app = builder.Build();
                WebApp = app;
                // enable swagger when in development or when it's enabled
                if (app.Environment.IsDevelopment() || core.Config.AspNET.SwaggerEnable)
                {
                    Console.WriteLine($"Enabled Swagger (core.Config.AspNET.SwaggerEnable={core.Config.AspNET.SwaggerEnable})");
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseRouting();
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                if (FeatureFlags.SentryEnable && app.Environment.IsDevelopment() == false)
                {
                    if (string.IsNullOrEmpty(FeatureFlags.SentryDSN))
                    {
                        throw new Exception($"Missing FeatureFlag {nameof(FeatureFlags.SentryDSN)} when {nameof(FeatureFlags.SentryEnable)} is enabled!");
                    }
                    app.UseSentryTracing();
                }

                app.UseAuthorization();
                app.UseCookiePolicy();
                app.UseMvc();
                app.UseSession();

                return app.Services;
            }
            else
            {
                throw new ArgumentException($"Does not equal {nameof(builder)}.{nameof(builder.Services)}!", nameof(col));
            }
        };
    }
}
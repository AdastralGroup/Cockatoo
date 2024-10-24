using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Common;

/// <summary>
/// This will be created when <see cref="ICoreContext.MainAsync"/> is called.
/// </summary>
/// <remarks>
/// Implement this on a public class, with <see cref="StartupStepAttribute"/> on that as well.
/// </remarks>
public interface ICoreStartupStep
{
    /// <summary>
    /// Called when <see cref="ICoreContext.MainAsync"/> is called.
    /// </summary>
    public Task Initialize(ICoreContext context);
    
    /// <summary>
    /// Called when <see cref="ICoreContext"/> is populating <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <remarks>
    /// When <see cref="StartupStepGroupKind"/> </remarks>
    public Task SetupServices(IServiceCollection services);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class StartupStepAttribute(StartupStepGroupKind group = StartupStepGroupKind.BeforeBuild)
    : Attribute
{
    public StartupStepGroupKind StepGroup { get; private set; } = group;
}
public enum StartupStepGroupKind : byte
{
    /// <summary>
    /// Associated <see cref="ICoreStartupStep"/> is related to the creation of databases used.
    /// </summary>
    Database = 0x00,
    /// <summary>
    /// Instance of class extending <see cref="ICoreStartupStep"/> will be used after detected services are found,
    /// but before the <see cref="IServiceCollection"/> is built into <see cref="IServiceProvider"/>
    /// </summary>
    BeforeBuild = 0xff,
}

public interface ICoreContext
{
    public string Id { get; }
    public IServiceProvider? Services { get; }
    public T GetRequiredService<T>() where T : notnull;
    /// <summary>
    /// Unix Timestamp when this instance was created at (UTC, Seconds)
    /// </summary>
    public long StartTimestamp { get; }

    /// <summary>
    /// Should be called as soon as possible.
    /// </summary>
    /// <param name="args">Program Launch Arguments</param>
    /// <param name="beforeServiceBuild">
    /// Delegate to call before <see cref="IServiceCollection"/> is built, but after everything is detected
    /// </param>
    /// <param name="customServiceCollection">
    /// Instead of creating a new instance of <see cref="IServiceCollection"/>, provide one that might have stuff in
    /// it already.
    /// </param>
    public Task MainAsync(string[] args,
        Func<IServiceCollection, Task> beforeServiceBuild,
        IServiceCollection? customServiceCollection = null);
}
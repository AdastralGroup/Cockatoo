using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class TaskMutexService : BaseService
{
    private readonly TaskMutexRepository _repo;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public TaskMutexService(IServiceProvider services)
        : base(services)
    {
        _repo = services.GetRequiredService<TaskMutexRepository>();
    }

    /// <summary>
    /// Wait until the Type & Name for the Task Mutex is released or deleted.
    /// </summary>
    /// <param name="timeout">When provided, it will only wait up to the amount provided (in milliseconds) before returning. When <see langword="null"/> or <c>-1</c>, it will wait forever.</param>
    public async Task<bool> Wait(Type type, string name, Dictionary<string, object>? opts = null, long? timeout = null)
    {
        var start = DateTimeOffset.UtcNow;
        int count = 0;
        long timePassed = 0;
        while (true)
        {
            var model = await _repo.GetLockModel(type, name, opts);
            if (model == null)
            {
                break;
            }

            await Task.Delay(100);
            if (timeout != null)
            {
                timePassed += 100;
                if (timeout > 0 && timeout < timePassed)
                {
                    _log.Debug($"Took too long! (timeout: {timeout}ms, timePassed: {timePassed}ms)");
                    break;
                }
            }
            count++;
        }
        _log.Debug($"Took {CockatooHelper.FormatDuration(start)} (type: {type}, name: {name})");
        return count == 0;
    }
    public async Task<TaskMutexModel> Lock(Type type, string name, Dictionary<string, object>? opts = null)
    {
        var model = new TaskMutexModel()
        {
            TaskClassType = type.ToString(),
            TaskName = name,
            Options = opts,
            Released = false
        };
        model = await _repo.InsertOrUpdate(model);
        return model;
    }
    public async Task Unlock(TaskMutexModel model)
    {
        var m = await _repo.GetById(model.Id);
        if (m == null)
            return;
        m.Released = true;
        await _repo.InsertOrUpdate(model);
    }
}
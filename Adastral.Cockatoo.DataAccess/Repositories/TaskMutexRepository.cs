using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class TaskMutexRepository : BaseRepository<TaskMutexModel>
{
    public TaskMutexRepository(IServiceProvider services)
        : base(TaskMutexModel.CollectionName, services)
    {}
    static TaskMutexRepository()
    {
        CreatedAtDescending = Builders<TaskMutexModel>
            .Sort
            .Descending(v => v.CreatedAt);
        UpdatedAtDescending = Builders<TaskMutexModel>
            .Sort
            .Descending(v => v.UpdatedAt);
    }
    private static readonly SortDefinition<TaskMutexModel> CreatedAtDescending;
    private static readonly SortDefinition<TaskMutexModel> UpdatedAtDescending;
    /// <summary>
    /// Try and get an instance of <see cref="TaskMutexModel"/> where the <paramref name="type"/> and <paramref name="taskName"/> matches, and the lock isn't released.
    /// </summary>
    /// <remarks>
    /// When <paramref name="options"/> isn't <see langword="null"/> and all the keys in it are in a model and all of those values match, then it will return that.
    /// </remarks>
    public async Task<TaskMutexModel?> GetLockModel(Type type, string taskName, Dictionary<string, object>? options = null)
    {
        var typeName = type.ToString();
        var filter = Builders<TaskMutexModel>
            .Filter
            .Where(v => v.TaskClassType == typeName && v.TaskName == taskName && v.Released == false);
        var result = await BaseFind(filter, UpdatedAtDescending);

        if (options == null)
        {
            return result?.FirstOrDefault();
        }
        var filtered = (result?.ToList() ?? []).Where((v) =>
        {
            int r = options!.Keys.Count;
            int c = 0;
            foreach (var (key, value) in options)
            {
                if (v.Options?.TryGetValue(key, out var x) ?? false)
                {
                    if (x.Equals(value))
                    {
                        c++;
                    }
                }
            }
            return c == r;
        });
        return filtered.FirstOrDefault();
    }
    public async Task<TaskMutexModel?> GetById(string id)
    {
        var filter = Builders<TaskMutexModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<TaskMutexModel>> GetAll()
    {
        var filter = Builders<TaskMutexModel>
            .Filter
            .Empty;
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            return 0;
        var filter = Builders<TaskMutexModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result?.DeletedCount ?? 0;        
    }
    public async Task<TaskMutexModel> InsertOrUpdate(TaskMutexModel model)
    {
        model.UpdatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<TaskMutexModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsResult = await BaseFind(filter);
        var exists = existsResult.Any();
        if (exists)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            model.CreatedAt = model.UpdatedAt;
            await collection.InsertOneAsync(model);
        }
        return model;
    }
}
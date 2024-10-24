using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class UserPreferencesRepository : BaseRepository<UserPreferencesModel>
{
    private readonly UserRepository _userRepo;
    public UserPreferencesRepository(IServiceProvider services)
        : base(UserPreferencesModel.CollectionName, services)
    {
        _userRepo = services.GetRequiredService<UserRepository>();
    }

    public async Task<UserPreferencesModel?> GetById(string userId)
    {
        var filter = Builders<UserPreferencesModel>
            .Filter
            .Where(v => v.UserId == userId);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public Task<List<UserPreferencesModel>> GetAllUsingFile(StorageFileModel model)
        => GetAllUsingFile(model.Id);
    public async Task<List<UserPreferencesModel>> GetAllUsingFile(string storageFileId)
    {
        var filter = Builders<UserPreferencesModel>
            .Filter
            .Where(v => v.AvatarStorageFileId != null && v.AvatarStorageFileId == storageFileId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<long> Delete(params string[] userIds)
    {
        var filter = Builders<UserPreferencesModel>
            .Filter
            .In(v => v.UserId, userIds);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

        var res = await collection.DeleteManyAsync(filter);
        return res.DeletedCount;
    }

    public async Task InsertOrUpdate(UserPreferencesModel model)
    {
        if (string.IsNullOrEmpty(model.UserId))
            throw new ArgumentException($"Property {nameof(model.UserId)} is required", nameof(model));
        var userModel = await _userRepo.GetById(model.UserId);
        if (userModel == null)
            throw new NoNullAllowedException($"Could not find User with Id {model.UserId} (document not found)");
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<UserPreferencesModel>
            .Filter
            .Where(v => v.UserId == model.UserId);
        var exists = await BaseFind(filter);
        if (exists?.Any() ?? false)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}
using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class UserSessionRepository : BaseRepository<UserSessionModel>
{
    private readonly UserRepository _userRepo;
    public UserSessionRepository(IServiceProvider services)
        : base(UserSessionModel.CollectionName, services)
    {
        _userRepo = services.GetRequiredService<UserRepository>();
    }
    private static SortDefinition<UserSessionModel> CreatedAtDescending;
    static UserSessionRepository()
    {
        CreatedAtDescending = Builders<UserSessionModel>
            .Sort
            .Descending(v => v.CreatedAt);
    }
    public async Task<UserSessionModel?> GetById(string sessionId)
    {
        var filter = Builders<UserSessionModel>
            .Filter
            .Where(v => v.Id == sessionId);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<UserSessionModel>> GetManyForUser(string userId, bool includeDeleted = false)
    {
        userId = userId.Trim().ToLower();
        var filter = Builders<UserSessionModel>
            .Filter
            .Where(v => v.UserId == userId);
        if (!includeDeleted)
        {
            filter &= Builders<UserSessionModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await BaseFind(filter, CreatedAtDescending);
        return result?.ToList() ?? [];
    }

    public async Task<List<UserSessionModel>> GetAll(bool includeDeleted = false)
    {
        var filter = Builders<UserSessionModel>
            .Filter
            .Empty;
        if (includeDeleted == false)
        {
            filter &= Builders<UserSessionModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await BaseFind(filter, CreatedAtDescending);
        return result?.ToList() ?? [];
    }
    public async Task<List<UserSessionModel>> GetAllWithAspNetSession(string aspNetSessionId)
    {
        var filter = Builders<UserSessionModel>
            .Filter
            .Where(v => v.AspNetSessionId == aspNetSessionId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<long> Delete(params string[] userSessionIds)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }
        var filter = Builders<UserSessionModel>
            .Filter
            .In(v => v.Id, userSessionIds);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<UserSessionModel> InsertOrUpdate(UserSessionModel model)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }

        if (string.IsNullOrEmpty(model.UserId))
        {
            throw new NoNullAllowedException($"{nameof(UserSessionModel)}.{nameof(model.UserId)} is required");
        }
        var userModel = await _userRepo.GetById(model.UserId);
        if (userModel == null)
        {
            throw new NoNullAllowedException($"Could not get User with Id {model.UserId}");
        }

        var filter = Builders<UserSessionModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var exists = await BaseCount(filter);
        if (exists > 0)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            model.CreatedAt = new MongoDB.Bson.BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
        }
        return model;
    }
}
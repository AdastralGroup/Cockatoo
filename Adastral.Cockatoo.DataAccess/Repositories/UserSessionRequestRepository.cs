using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class UserSessionRequestRepository : BaseRepository<UserSessionRequestModel>
{
    private readonly UserSessionRepository _userSessionRepo;
    public UserSessionRequestRepository(IServiceProvider services)
        : base(UserSessionRequestModel.CollectionName, services)
    {
        _userSessionRepo = services.GetRequiredService<UserSessionRepository>();
    }

    public async Task<UserSessionRequestModel?> GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var filter = Builders<UserSessionRequestModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<UserSessionRequestModel>> GetAllForSession(string sessionId, CancellationToken? token = null)
    {
        sessionId = sessionId.Trim().ToLower();
        if (string.IsNullOrEmpty(sessionId)) return [];
        var filter = Builders<UserSessionRequestModel>
            .Filter
            .Where(v => v.UserSessionId == sessionId);
        var result = await BaseFind(filter, token: token);
        return result?.ToList() ?? [];
    }
    public async Task<List<string>> GetIpsForSession(string sessionId, CancellationToken? token = null)
    {
        sessionId = sessionId.Trim().ToLower();
        if (string.IsNullOrEmpty(sessionId)) return [];
        var filter = Builders<UserSessionRequestModel>
            .Filter
            .Where(v => v.UserSessionId == sessionId);
        var result = await BaseFind(filter, token: token);
        return (result?.ToList() ?? []).Select(v => v.IpAddress).Where(v => v != null).Cast<string>().ToList();
    }

    public async Task<long> Count(string requestId)
    {
        requestId = requestId.Trim().ToLower();
        if (string.IsNullOrEmpty(requestId)) return 0;
        var filter = Builders<UserSessionRequestModel>
            .Filter
            .Where(v => v.Id == requestId);
        return await BaseCount(filter);
    }
    public async Task<bool> Exists(string requestId)
    {
        return await Count(requestId) > 0;
    }

    public async Task<UserSessionRequestModel> InsertOrUpdate(UserSessionRequestModel model)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }
        var userSessionModel = await _userSessionRepo.GetById(model.UserSessionId);
        if (userSessionModel == null)
        {
            throw new NoNullAllowedException($"Could not get User Session with Id {model.UserSessionId}");
        }

        var filter = Builders<UserSessionRequestModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var exists = await BaseCount(filter) > 0;
        if (exists)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            model.CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
        }
        return model;
    }
}
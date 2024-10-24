using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class UserRepository : BaseRepository<UserModel>
{
    private readonly ServiceAccountRepository _serviceAccRepo;
    public UserRepository(IServiceProvider services)
        : base(UserModel.CollectionName, services)
    {
        _serviceAccRepo = services.GetRequiredService<ServiceAccountRepository>();
    }

    public async Task<UserModel?> GetByOAuthId(string oauthUserId)
    {
        var filter = Builders<UserModel>
            .Filter
            .Where(v => v.OAuthUserId == oauthUserId);
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }

    public async Task<UserModel?> GetByEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return null;
        var result = await GetAllByEmail(email);
        return result?.FirstOrDefault();
    }

    public async Task<bool> Exists(string id)
    {
        var collection = GetCollection();
        if (collection == null)
            return false;
        var filter = Builders<UserModel>
            .Filter
            .Where(v => v.Id == id);
        var count = await collection.CountDocumentsAsync(filter);
        return count > 0;
    }

    public async Task<UserModel?> GetById(string id)
    {
        id = id.ToLower();
        var filter = Builders<UserModel>
            .Filter
            .Where(v => v.Id == id);
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }
    public async Task<List<UserModel>> GetManyById(params string[] ids)
    {
        var filter = Builders<UserModel>
            .Filter
            .In(v => v.Id, ids);
        var res = await BaseFind(filter);
        return res?.ToList() ?? [];
    }
    public async Task<List<UserModel>> GetAllByEmail(string email)
    {
        var filter = Builders<UserModel>
            .Filter
            .Where(v => v.Email == email);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<List<UserModel>> GetAll()
    {
        var filter = Builders<UserModel>
            .Filter
            .Empty;
        var res = await BaseFind(filter);
        return res?.ToList() ?? [];
    }

    public async Task<List<UserModel>> GetAllBySAOwner(string ownerUserId)
    {
        var children = await _serviceAccRepo.GetAllForOwner(ownerUserId);
        if (children.Count < 1)
            return [];

        var filter = Builders<UserModel>
            .Filter
            .In(v => v.Id, children.Select(v => v.UserId));
        filter &= Builders<UserModel>
            .Filter
            .Where(v => v.IsServiceAccount == true);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    /// <summary>
    /// Insert or Update the <paramref name="model"/> provided.
    /// </summary>
    /// <exception cref="NoNullAllowedException">Thrown when <see cref="BaseRepository{UserModel}.GetCollection()"/> returns <see langword="null"/></exception>
    public async Task InsertOrUpdate(UserModel model)
    {
        model.Id = model.Id.ToLower();
        // set created at when it's not set, assuming this is a new user.
        if (model.CreatedAtTimestamp == "0" || string.IsNullOrEmpty(model.CreatedAtTimestamp))
        {
            model.SetCreatedAtTimestamp();
        }

        var filter = Builders<UserModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsRes = await BaseFind(filter);

        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        if (await existsRes.AnyAsync())
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}
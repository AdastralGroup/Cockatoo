using Adastral.Cockatoo.Common;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Data;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.DataAccess.Repositories
{
    [CockatooDependency]
    public class ServiceAccountTokenRepository : BaseRepository<ServiceAccountTokenModel>
    {
        public ServiceAccountTokenRepository(IServiceProvider services)
            : base(ServiceAccountTokenModel.CollectionName, services)
        {
        }

        public async Task<ServiceAccountTokenModel?> GetById(string id)
        {
            var filter = Builders<ServiceAccountTokenModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await BaseFind(filter);
            return result?.FirstOrDefault();
        }
        public async Task<ServiceAccountTokenModel?> GetByToken(string token, bool enforceExpiry = true)
        {
            var filter = Builders<ServiceAccountTokenModel>
                .Filter
                .Where(v => v.Token == token);
            if (enforceExpiry)
            {
                var current = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                var x = Builders<ServiceAccountTokenModel>
                    .Filter
                    .Where(v => v.ExpiresAtTimestamp != null);
                x |= Builders<ServiceAccountTokenModel>
                    .Filter
                    .Where(v => v.ExpiresAtTimestamp < current);
                filter &= x;
            }
            var result = await BaseFind(filter);
            return result?.FirstOrDefault();
        }
        public async Task<List<ServiceAccountTokenModel>> GetForAccount(string serviceAccountId, bool includeExpired = true)
        {
            var filter = Builders<ServiceAccountTokenModel>
                .Filter
                .Where(v => v.ServiceAccountId == serviceAccountId);
            if (includeExpired == false)
            {
                var current = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                var x = Builders<ServiceAccountTokenModel>
                    .Filter
                    .Where(v => v.ExpiresAtTimestamp != null);
                x |= Builders<ServiceAccountTokenModel>
                    .Filter
                    .Where(v => v.ExpiresAtTimestamp < current);
                filter &= x;
            }
            var result = await BaseFind(filter);
            return result?.ToList() ?? [];
        }
        public async Task<List<ServiceAccountTokenModel>> GetAllCreatedWithSession(string userSessionId, bool includeExpired = false)
        {
            var filter = Builders<ServiceAccountTokenModel>
                .Filter
                .Where(v => v.CreatedWithSessionId == userSessionId);
            if (!includeExpired)
            {
                var current = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                var x = Builders<ServiceAccountTokenModel>
                    .Filter
                    .Where(v => v.ExpiresAtTimestamp != null);
                x |= Builders<ServiceAccountTokenModel>
                    .Filter
                    .Where(v => v.ExpiresAtTimestamp < current);
                filter &= x;
            }
            var result = await BaseFind(filter);
            return result?.ToList() ?? [];
        }
        public async Task<long> Delete(params string[] ids)
        {
            var collection = GetCollection();
            if (collection == null)
                return 0;
            var filter = Builders<ServiceAccountTokenModel>
                .Filter
                .In(v => v.Id, ids);
            var result = await collection.DeleteManyAsync(filter);
            return result?.DeletedCount ?? 0;
        }
        public async Task<ServiceAccountTokenModel> InsertOrUpdate(ServiceAccountTokenModel model)
        {
            if (string.IsNullOrEmpty(model.ServiceAccountId))
            {
                throw new ArgumentException($"Property {nameof(model.ServiceAccountId)} is required", nameof(model));
            }
            var collection = GetCollection();
            if (collection == null)
            {
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            }
            var filter = Builders<ServiceAccountTokenModel>
                .Filter
                .Where(v => v.Id == model.Id);
            var existsResult = await collection.CountDocumentsAsync(filter);
            if (existsResult < 1)
            {
                model.CreatedAtTimestamp = new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                await collection.InsertOneAsync(model);
            }
            else
            {
                await collection.ReplaceOneAsync(filter, model);
            }
            return model;
        }
    }
}

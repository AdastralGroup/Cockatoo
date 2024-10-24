using Adastral.Cockatoo.Common;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.DataAccess.Repositories
{
    [CockatooDependency]
    public class ServiceAccountRepository : BaseRepository<ServiceAccountModel>
    {
        public ServiceAccountRepository(IServiceProvider services)
            : base(ServiceAccountModel.CollectionName, services)
        { }

        public async Task<ServiceAccountModel?> GetById(string userId)
        {
            var filter = Builders<ServiceAccountModel>
                .Filter
                .Where(v => v.UserId == userId);
            var result = await BaseFind(filter);
            return result?.FirstOrDefault();
        }
        public async Task<List<ServiceAccountModel>> GetAllForOwner(string ownerUserId)
        {
            var filter = Builders<ServiceAccountModel>
                .Filter
                .Where(v => v.OwnerUserId == ownerUserId);
            var result = await BaseFind(filter);
            return result?.ToList() ?? [];
        }
        public async Task<long> Delete(params string[] userIds)
        {
            var collection = GetCollection();
            if (collection == null)
                return 0;
            var filter = Builders<ServiceAccountModel>
                .Filter
                .In(v => v.UserId, userIds);
            var result = await collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }
        public async Task InsertOrUpdate(ServiceAccountModel model)
        {
            if (string.IsNullOrEmpty(model.UserId))
            {
                throw new ArgumentException($"Property {nameof(model.UserId)} is required", nameof(model));
            }
            if (string.IsNullOrEmpty(model.OwnerUserId))
            {
                throw new ArgumentException($"Property {nameof(model.OwnerUserId)} is required", nameof(model));
            }
            var collection = GetCollection();
            if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var existsFilter = Builders<ServiceAccountModel>
                .Filter
                .Where(v => v.UserId == model.UserId);
            var existsResult = await collection.CountDocumentsAsync(existsFilter);
            if (existsResult < 1)
            {
                await collection.InsertOneAsync(model);
            }
            else
            {
                await collection.ReplaceOneAsync(existsFilter, model);
            }
        }
    }
}

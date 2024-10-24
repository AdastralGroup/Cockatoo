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
    public class PermissionGroupRepository : BaseRepository<PermissionGroupModel>
    {
        public PermissionGroupRepository(IServiceProvider services)
            : base(PermissionGroupModel.CollectionName, services)
        { }

        public async Task<PermissionGroupModel?> GetById(string id)
        {
            var filter = Builders<PermissionGroupModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await BaseFind(filter);
            return result?.FirstOrDefault();
        }

        public async Task<List<PermissionGroupModel>> GetAll()
        {
            var filter = Builders<PermissionGroupModel>
                .Filter
                .Empty;
            var res = await BaseFind(filter);
            return res?.ToList() ?? [];
        }
        public async Task<long> GetAllCount()
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var filter = Builders<PermissionGroupModel>.Filter.Empty;
            var count = await collection.CountDocumentsAsync(filter);
            return count;
        }

        /// <summary>
        /// Get all of the Permission Groups that are in the <paramref name="ids"/> provided, and order descending by
        /// <see cref="PermissionGroupModel.Priority"/>
        /// </summary>
        /// <returns>Will not return <see langword="null"/>.</returns>
        public async Task<List<PermissionGroupModel>> GetManyById(params string[] ids)
        {
            var filter = Builders<PermissionGroupModel>
                .Filter
                .In(v => v.Id, ids);
            var order = Builders<PermissionGroupModel>
                .Sort
                .Descending(v => v.Priority);
            var result = await BaseFind(filter, order);
            return result?.ToList() ?? [];
        }
        public async Task<List<PermissionGroupModel>> GetManyByName(params string[] names)
        {
            var filter = Builders<PermissionGroupModel>
                .Filter
                .In(v => v.Name, names);
            var order = Builders<PermissionGroupModel>
                .Sort
                .Descending(v => v.Priority);
            var result = await BaseFind(filter, order);
            return result?.ToList() ?? [];
        }

        public async Task<bool> Exists(string id)
        {
            var filter = Builders<PermissionGroupModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await BaseCount(filter);
            return result > 0;
        }

        public async Task<long> Delete(params string[] ids)
        {
            var collection = GetCollection();
            if (collection == null)
                return 0;
            var filter = Builders<PermissionGroupModel>
                .Filter
                .In(v => v.Id, ids);
            var res = await collection.DeleteManyAsync(filter);
            return res?.DeletedCount ?? 0;
        }

        public async Task InsertOrUpdate(PermissionGroupModel model)
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var filter = Builders<PermissionGroupModel>
                .Filter
                .Where(v => v.Id == model.Id);
            var existsResult = await collection.CountDocumentsAsync(filter);
            if (existsResult > 0)
            {
                await collection.FindOneAndReplaceAsync(filter, model);
            }
            else
            {
                await collection.InsertOneAsync(model);
            }
        }
    }
}

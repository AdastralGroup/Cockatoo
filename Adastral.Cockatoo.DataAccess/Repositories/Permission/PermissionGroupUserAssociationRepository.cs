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
    public class PermissionGroupUserAssociationRepository : BaseRepository<PermissionGroupUserAssociationModel>
    {
        public PermissionGroupUserAssociationRepository(IServiceProvider services)
            : base(PermissionGroupUserAssociationModel.CollectionName, services)
        { }

        public Task<PermissionGroupUserAssociationModel?> Get(PermissionGroupUserAssociationModel model)
            => GetById(model.Id);
        public async Task<PermissionGroupUserAssociationModel?> GetById(string id)
        {
            var filter = Builders<PermissionGroupUserAssociationModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await BaseFind(filter);
            return result.FirstOrDefault();
        }

        public async Task<long> CountByGroupId(string groupId)
        {
            var collection = GetCollection();
            if (collection == null)
                return 0;
            var filter = Builders<PermissionGroupUserAssociationModel>
                .Filter
                .Where(v => v.PermissionGroupId == groupId);
            var result = await collection.CountDocumentsAsync(filter);
            return result;
        }

        public Task<List<PermissionGroupUserAssociationModel>> GetAllWithUser(UserModel model)
            => GetAllWithUser(model.Id);
        public async Task<List<PermissionGroupUserAssociationModel>> GetAllWithUser(string userId)
        {
            var filter = Builders<PermissionGroupUserAssociationModel>
                .Filter
                .Where(v => v.UserId == userId);
            var result = await BaseFind(filter);
            return result?.ToList() ?? [];
        }

        public Task<List<PermissionGroupUserAssociationModel>> GetAllWithGroup(PermissionGroupModel model)
            => GetAllWithGroup(model.Id);
        public async Task<List<PermissionGroupUserAssociationModel>> GetAllWithGroup(string groupId)
        {
            var filter = Builders<PermissionGroupUserAssociationModel>
                .Filter
                .Where(v => v.PermissionGroupId == groupId);
            var result = await BaseFind(filter);
            return result.ToList() ?? [];
        }

        public Task<List<PermissionGroupUserAssociationModel>> GetAllWithUserAndGroup(UserModel user, PermissionGroupModel group)
            => GetAllWithUserAndGroup(user.Id, group.Id);
        public async Task<List<PermissionGroupUserAssociationModel>> GetAllWithUserAndGroup(string userId, string groupId)
        {
            var filter = Builders<PermissionGroupUserAssociationModel>
                .Filter
                .Where(v => v.UserId == userId && v.PermissionGroupId == groupId);
            var result = await BaseFind(filter);
            return result.ToList() ?? [];
        }

        public Task<long> Delete(params PermissionGroupUserAssociationModel[] models)
            => Delete(models.Select(v => v.Id).Distinct().ToArray());
        public async Task<long> Delete(params string[] ids)
        {
            var collection = GetCollection();
            if (collection == null)
                return 0;
            var filter = Builders<PermissionGroupUserAssociationModel>
                .Filter
                .In(v => v.Id, ids.Distinct());
            var result = await collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }

        public async Task InsertOrUpdate(PermissionGroupUserAssociationModel model)
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var filter = Builders<PermissionGroupUserAssociationModel>
                .Filter
                .Where(v => v.Id == model.Id);
            var existsRes = await collection.CountDocumentsAsync(filter);
            if (existsRes > 0)
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

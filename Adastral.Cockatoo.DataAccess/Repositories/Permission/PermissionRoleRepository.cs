using Adastral.Cockatoo.Common;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adastral.Cockatoo.DataAccess.Models;
using kate.shared.Helpers;
using NLog;

namespace Adastral.Cockatoo.DataAccess.Repositories
{
    [CockatooDependency]
    public class PermissionRoleRepository : BaseRepository<PermissionRoleModel>
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        public PermissionRoleRepository(IServiceProvider services)
            : base(PermissionRoleModel.CollectionName, services)
        {
            var deleteCount = RemoveInvalidItems().Result;
            if (deleteCount > 0)
            {
                _log.Info($"Deleted {deleteCount} documents with an invalid {nameof(PermissionKind)}");
            }
        }

        private async Task<long> RemoveInvalidItems()
        {
            var collection = GetCollection<Dictionary<string, object?>>();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var allowed = GeneralHelper.GetEnumList<PermissionKind>().Select(v => v.ToString());
            var filterIn = Builders<Dictionary<string, object?>>
                .Filter
                .In(nameof(PermissionRoleModel.Kind), allowed);
            var filter = Builders<Dictionary<string, object?>>
                .Filter
                .Not(filterIn);
            var result = await collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }
        
        public async Task<PermissionRoleModel?> GetById(string id)
        {
            var filter = Builders<PermissionRoleModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await BaseFind(filter);
            return result.FirstOrDefault();
        }

        public async Task<List<PermissionRoleModel>> GetForGroup(string? groupId, PermissionKind? byKind = null, bool? byAllow = null)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return [];
            }
            var filter = Builders<PermissionRoleModel>
                .Filter
                .Where(v => v.PermissionGroupId == groupId);
            if (byKind != null)
            {
                filter &= Builders<PermissionRoleModel>
                    .Filter
                    .Where(v => v.Kind == byKind);
            }
            if (byAllow != null)
            {
                filter &= Builders<PermissionRoleModel>
                    .Filter
                    .Where(v => v.Allow == byAllow);
            }
            var result = await BaseFind(filter);
            return result?.ToList() ?? [];
        }
        public async Task<long> GetCountByGroup(string groupId)
        {
            var collection = GetCollection();
            if (collection == null)
                return 0;
            var filter = Builders<PermissionRoleModel>
                .Filter
                .Where(v => v.PermissionGroupId == groupId);
            var count = await collection.CountDocumentsAsync(filter);
            return count;
        }

        public async Task<List<PermissionRoleModel>> GetManyForManyGroups(List<PermissionGroupModel> models)
        {
            var filter = Builders<PermissionRoleModel>
                .Filter
                .In(v => v.PermissionGroupId, models.Select(v => v.Id));
            var result = await BaseFind(filter);
            // order in ascending by priority so the deny/allow of higher priority
            // groups actually take affect the ordering, so it's easier to calculate
            // the "real" permissions for applying all the groups on a user.
            var data = result?.ToEnumerable()
                .OrderBy(v => models.First(x => x.Id == v.PermissionGroupId).Priority)
                .ThenBy(v => v.Allow ? 0 : 1)
                .ToList();
            return data ?? [];
        }

        public Task<long> Delete(params PermissionRoleModel[] models)
            => Delete(models.Select(v => v.Id).ToArray());

        public async Task<long> Delete(params string[] ids)
        {
            var collection = GetCollection();
            if (collection == null)
                return 0;
            var filter = Builders<PermissionRoleModel>
                .Filter
                .In(v => v.Id, ids);
            var result = await collection.DeleteManyAsync(filter);
            return result?.DeletedCount ?? 0;
        }

        public async Task InsertOrUpdateMany(ICollection<PermissionRoleModel> models)
        {
            var taskList = new List<Task>();
            for (int i = 0; i < models.Count; i++)
            {
                var index = i;
                taskList.Add(new Task(delegate
                {
                    InsertOrUpdate(models.ElementAt(index)).Wait();
                }));
            }

            foreach (var i in taskList)
                i.Start();
            await Task.WhenAll(taskList);
        }
        public async Task InsertOrUpdate(PermissionRoleModel model)
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var filter = Builders<PermissionRoleModel>
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

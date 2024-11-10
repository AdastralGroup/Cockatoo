using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;
using NLog;
using System.Data;

namespace Adastral.Cockatoo.DataAccess.Repositories
{
    [CockatooDependency]
    public class ClientApiKeyProviderRepository : BaseRepository<ClientApiKeyProviderModel>
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        public ClientApiKeyProviderRepository(IServiceProvider services)
            : base(ClientApiKeyProviderModel.CollectionName, services)
        {
            var collection = GetCollection();
            if (collection != null)
            {
                if (!collection.Indexes.List().Any())
                {
                    var dataKeys = Builders<ClientApiKeyProviderModel>
                        .IndexKeys
                        .Ascending(v => v.Id)
                        .Ascending(v => v.IsActive);
                    collection.Indexes.CreateOne(new CreateIndexModel<ClientApiKeyProviderModel>(dataKeys));
                    _log.Info($"Added indexes");
                }
            }
        }

        public async Task<ClientApiKeyProviderModel?> GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            var filter = Builders<ClientApiKeyProviderModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await BaseFind(filter);
            return result?.FirstOrDefault();
        }
        
        public async Task<List<ClientApiKeyProviderModel>> GetAll()
        {
            var filter = Builders<ClientApiKeyProviderModel>
                .Filter
                .Empty;
            var result = await BaseFind(filter);
            return result?.ToList() ?? [];
        }

        public async Task<long> Delete(params string[] ids)
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var filter = Builders<ClientApiKeyProviderModel>
                .Filter
                .In(v => v.Id, ids);
            var result = await collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }
        public async Task<long> Count()
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var filter = Builders<ClientApiKeyProviderModel>
                .Filter.Empty;
            var result = await collection.CountDocumentsAsync(filter);
            return result;
        }
        public async Task<long> CountWhereId(string id)
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
            var filter = Builders<ClientApiKeyProviderModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await collection.CountDocumentsAsync(filter);
            return result;
        }

        public async Task<ClientApiKeyProviderModel> InsertOrUpdate(ClientApiKeyProviderModel model)
        {
            var collection = GetCollection();
            if (collection == null)
                throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

            var existingIdCount = await CountWhereId(model.Id);
            if (existingIdCount < 1)
            {
                _log.Info($"Inserted document with Id {model.Id}");
                await collection.InsertOneAsync(model);
                return model;
            }
            else
            {
                var updateDefinition = Builders<ClientApiKeyProviderModel>
                    .Update
                    .Set(v => v.DisplayName, model.DisplayName)
                    .Set(v => v.BrandIconFileId, model.BrandIconFileId)
                    .Set(v => v.IsActive, model.IsActive)
                    .Set(v => v.PublicKeyXml, model.PublicKeyXml)
                    .Set(v => v.PrivateKeyXml, model.PrivateKeyXml);
                var updateFilter = Builders<ClientApiKeyProviderModel>
                    .Filter
                    .Where(v => v.Id == model.Id);
                var result = await collection.UpdateManyAsync(updateFilter, updateDefinition);
                _log.Info($"Updated {result.ModifiedCount} records with Id {model.Id}");
                return model;
            }
        }
    }
}

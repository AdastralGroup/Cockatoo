using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;
using NLog;

namespace Adastral.Cockatoo.DataAccess.Repositories
{
    [CockatooDependency]
    public class ClientApiKeyRepository : BaseRepository<ClientApiKeyModel>
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        public ClientApiKeyRepository(IServiceProvider services)
            : base(ClientApiKeyModel.CollectionName, services)
        {
            var collection = GetCollection();
            if (collection != null)
            {
                if (!collection.Indexes.List().Any())
                {
                    var dataKeys = Builders<ClientApiKeyModel>
                        .IndexKeys
                        .Ascending(v => v.Id)
                        .Ascending(v => v.Token)
                        .Ascending(v => v.UserId);
                    var adminKeys = Builders<ClientApiKeyModel>
                        .IndexKeys
                        .Ascending(v => v.DisabledByUserId);
                    collection.Indexes.CreateOne(new CreateIndexModel<ClientApiKeyModel>(dataKeys));
                    collection.Indexes.CreateOne(new CreateIndexModel<ClientApiKeyModel>(adminKeys));
                    _log.Info($"Added indexes");
                }
            }
        }

        public async Task<ClientApiKeyModel?> GetById(string id)
        {
            var filter = Builders<ClientApiKeyModel>
                .Filter
                .Where(v => v.Id == id);
            var result = await BaseFind(filter);
            return result?.FirstOrDefault();
        }

        public async Task<List<ClientApiKeyModel>?> GetManyByUser(string userId, bool includeDisabled = false)
        {
            if (string.IsNullOrEmpty(userId))
                return [];
            var filter = Builders<ClientApiKeyModel>
                .Filter
                .Where(v => v.UserId == userId);
            if (!includeDisabled)
            {
                filter &= Builders<ClientApiKeyModel>
                    .Filter
                    .Where(v => v.Enabled);
            }
            var result = await BaseFind(filter);
            return result?.ToList() ?? [];
        }

        public async Task<ClientApiKeyModel?> GetByToken(string token, bool includeDisabled = false)
        {
            if (string.IsNullOrEmpty(token))
                return null;
            var filter = Builders<ClientApiKeyModel>
                .Filter
                .Where(v => v.Token == token);
            if (!includeDisabled)
            {
                filter &= Builders<ClientApiKeyModel>
                    .Filter
                    .Where(v => v.Enabled);
            }
            var result = await BaseFind(filter);
            return result?.FirstOrDefault();
        }
    }
}

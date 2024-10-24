using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.IO;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class BullseyeCacheService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly BullseyeAppRepository _bullAppRepo;
    private readonly BullseyeAppRevisionRepository _bullAppRevRepo;
    private readonly BullseyePatchRepository _bullPatchRepo;
    private readonly ApplicationDetailRepository _appDetailRepo;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly StorageService _storageService;
    private readonly BullseyeV1CacheRepository _bullV1CacheRepo;
    private readonly BullseyeV2CacheRepository _bullV2CacheRepo;
    public BullseyeCacheService(IServiceProvider services)
        : base(services)
    {
        _bullAppRepo = services.GetRequiredService<BullseyeAppRepository>();
        _bullAppRevRepo = services.GetRequiredService<BullseyeAppRevisionRepository>();
        _bullPatchRepo = services.GetRequiredService<BullseyePatchRepository>();

        _appDetailRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _storageService = services.GetRequiredService<StorageService>();

        _bullV1CacheRepo = services.GetRequiredService<BullseyeV1CacheRepository>();
        _bullV2CacheRepo = services.GetRequiredService<BullseyeV2CacheRepository>();
    }

    /// <summary>
    /// Result for <see cref="GenerateCache"/>
    /// </summary>
    public class GenerateCacheResult
    {
        /// <summary>
        /// Generated Cache Model for <see cref="BullseyeV1"/>
        /// </summary>
        public BullseyeV1CacheModel V1 { get; set; } = new();
        /// <summary>
        /// Generated Cache Model for <see cref="BullseyeV2"/>
        /// </summary>
        public BullseyeV2CacheModel V2 { get; set; } = new();
        /// <summary>
        /// Was a new record added in <see cref="BullseyeAppRepository"/>
        /// </summary>
        public bool IsNewBullseyeApp { get; set; }
    }
    /// <summary>
    /// Generate cache
    /// </summary>
    /// <param name="appId"><see cref="BullseyeAppModel.ApplicationDetailModelId"/></param>
    /// <param name="publishedOnly">When set to <see langword="true"/>, then only revisions that are live will be used.</param>
    /// <param name="setLiveState">When not <see langword="null"/>, the IsLive value for <see cref="BullseyeV1CacheModel"/>/<see cref="BullseyeV2CacheModel"/> will be set to it.</param>
    public async Task<GenerateCacheResult> GenerateCache(string appId, bool publishedOnly, bool? setLiveState = null)
    {
        var appDetail = await _appDetailRepo.GetById(appId);
        if (appDetail == null)
        {
            throw new Exception($"Could not find {nameof(ApplicationDetailModel)} with Id {appId}");
        }
        var bullApp = await _bullAppRepo.GetById(appId);
        bool isNewApp = bullApp == null;
        bullApp ??= new();
        bullApp.ApplicationDetailModelId = appId;
        if (isNewApp)
        {
            await _bullAppRepo.InsertOrUpdate(bullApp);
        }

        var v1 = new BullseyeV1();
        var v2 = new BullseyeV2()
        {
            Name = appDetail.AppVarData.Mod.SourceModName,
            SchemaVersion = 2
        };
        v2.SetLastUpdated();
        var revisions = await _bullAppRevRepo.GetAllForApp(appId, publishedOnly ? true : null);
        var revisionDict = revisions.ToDictionary(v => v.Id, v => v);
        BullseyeAppRevisionModel? highestVersion = null;
        var toPatchIds = new List<(string, string)>();
        foreach (var item in revisions.OrderBy(v => v.Version))
        {
            var v1VersionInfo = new BullseyeV1VersionInfo();
            var v2VersionInfo = new BullseyeV2VersionInfo();
            if (string.IsNullOrEmpty(item.Tag) == false)
            {
                v2VersionInfo.Tag = item.Tag;
            }

            var archiveFile = await _storageFileRepo.GetById(item.ArchiveStorageFileId);
            if (string.IsNullOrEmpty(item.SignatureStorageFileId) == false)
            {
                var signatureFile = await _storageFileRepo.GetById(item.SignatureStorageFileId);
                if (signatureFile != null)
                {
                    v1VersionInfo.SignatureUrl = _storageService.GetUrl(signatureFile);
                    v2VersionInfo.SignatureFilename = v1VersionInfo.SignatureUrl;
                }
            }

            if (string.IsNullOrEmpty(item.PeerToPeerStorageFileId) == false)
            {
                var torrentFile = await _storageFileRepo.GetById(item.PeerToPeerStorageFileId);
                if (torrentFile != null)
                {
                    v1VersionInfo.TorrentUrl = _storageService.GetUrl(torrentFile);
                    v2VersionInfo.TorrentFilename = v1VersionInfo.TorrentUrl;
                }
            }
            if (archiveFile != null)
            {
                v1VersionInfo.Filename = _storageService.GetUrl(archiveFile);
                v2VersionInfo.Filename = _storageService.GetUrl(archiveFile);
                v2VersionInfo.FileSize = archiveFile.GetSize();
            }
            v2VersionInfo.ExtractedSize = item.GetExtractedArchiveSize();


            if (string.IsNullOrEmpty(item.PreviousRevisionId) == false)
            {
                var previous = revisions.Where(v => v.Id == item.PreviousRevisionId).FirstOrDefault();
                if (previous != null)
                {
                    v2VersionInfo.PreviousVersionKey = previous.GetVersion().ToString();
                }
            }

            if (highestVersion == null || highestVersion.GetVersion() < item.GetVersion())
            {
                highestVersion = item;
            }

            var versionId = item.GetVersion().ToString();
            v1.Versions[versionId] = v1VersionInfo;
            v2.Versions[versionId] = v2VersionInfo;
            v1.Patches[versionId] = new();
            v2.Patches[versionId] = new();
            toPatchIds.Add((item.Id, versionId));
        }
        if (string.IsNullOrEmpty(bullApp.LatestRevisionId) == false)
        {
            var latest = await _bullAppRevRepo.GetById(bullApp.LatestRevisionId);
            if (latest != null)
            {
                highestVersion = latest;
            }
            else
            {
                _log.Warn($"Couldn't find latest revision {bullApp.LatestRevisionId}");
            }
        }
        foreach (var (toPatchId, targetVersionId) in toPatchIds)
        {
            var upgradeSources = await _bullPatchRepo.GetAllRevisionTo(toPatchId);
            foreach (var source in upgradeSources)
            {
                var v1Item = new BullseyeV1PatchInfo();
                var v2Item = new BullseyeV2PatchInfo();
                if (!revisionDict.TryGetValue(source.FromRevisionId, out var sourceRevision))
                {
                    continue;
                }

                var patchFile = await _storageFileRepo.GetById(source.StorageFileId);
                v1Item.TorrentUrl = null;
                v2Item.TorrentFilename = null;
                if (string.IsNullOrEmpty(source.PeerToPeerStorageFileId) == false)
                {
                    var torrentFile = await _storageFileRepo.GetById(source.PeerToPeerStorageFileId);
                    if (torrentFile != null)
                    {
                        v1Item.TorrentUrl = _storageService.GetUrl(torrentFile);
                        v2Item.TorrentFilename = _storageService.GetUrl(torrentFile);
                    }
                }
                if (patchFile != null)
                {
                    v1Item.Filename = _storageService.GetUrl(patchFile);
                    v1Item.TemporarySpaceRequired = patchFile.GetSize() ?? 0;
                    v2Item.Filename = v1Item.Filename;
                    v2Item.FileSize = v1Item.TemporarySpaceRequired;
                    v2Item.TemporarySpaceRequired = v2Item.FileSize * 2;
                }

                if (toPatchId == highestVersion?.Id)
                {
                    v1.Patches[sourceRevision.GetVersion().ToString()] = v1Item;
                }
                v2.Patches[targetVersionId][sourceRevision.GetVersion().ToString()] = v2Item;
            }
        }

        if (highestVersion != null)
        {
            v2.LatestVersion = highestVersion.Version;
        }

        var v1Cache = new BullseyeV1CacheModel()
        {
            IsLive = setLiveState == null
                ? publishedOnly
                : (bool)setLiveState,
            TargetAppId = appId,
        };
        var v2Cache = new BullseyeV2CacheModel()
        {
            IsLive = v1Cache.IsLive,
            TargetAppId = appId
        };
        v1Cache.SetContent(v1);
        v2Cache.SetContent(v2);

        await _bullV1CacheRepo.InsertOrUpdate(v1Cache);
        await _bullV2CacheRepo.InsertOrUpdate(v2Cache);
        return new()
        {
            V1 = v1Cache,
            V2 = v2Cache,
            IsNewBullseyeApp = isNewApp
        };
    }

    private static readonly Mutex GetLatestV1GenerateMutex = new();
    public async Task<BullseyeV1> GetLatestV1(string appId, bool? liveState = null)
    {
        var cacheModel = await _bullV1CacheRepo.GetForApp(appId, liveState);
        if (cacheModel == null)
        {
            GetLatestV1GenerateMutex.WaitOne();
            // generate new cache, and include non-live patches when liveState isn't null and it's false
            var res = await GenerateCache(appId, liveState == null ? true : (bool)liveState, liveState);
            GetLatestV1GenerateMutex.ReleaseMutex();
            return res.V1.GetContent()!;
        }
        return cacheModel.GetContent()!;
    }
    private static readonly Mutex GetLatestV2GenerateMutex = new();
    public async Task<BullseyeV2> GetLatestV2(string appId, bool? liveState = null)
    {
        var cacheModel = await _bullV2CacheRepo.GetByAppId(appId, liveState);
        if (cacheModel == null)
        {
            GetLatestV2GenerateMutex.WaitOne();
            // generate new cache, and include non-live patches when liveState isn't null and it's false
            var res = await GenerateCache(appId, liveState == null ? true : (bool)liveState, liveState);
            GetLatestV2GenerateMutex.ReleaseMutex();
            return res.V2.GetContent()!;
        }
        return cacheModel.GetContent()!;
    }
}
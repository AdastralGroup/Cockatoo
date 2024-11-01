using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.AutoUpdaterDotNet;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class StorageService : BaseService
{
    private readonly StorageFileRepository _storageFileRepo;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationImageRepository _appImageRepo;
    private readonly BullseyeAppRevisionRepository _bullseyeAppRevisionRepo;
    private readonly BullseyePatchRepository _bullseyePatchRepo;
    private readonly BlogPostAttachmentRepository _blogPostAttachmentRepo;
    private readonly UserPreferencesRepository _userPrefRepo;
    private readonly AUDNRevisionRepository _audnRevisionRepo;
    private readonly CockatooConfig _config;
    public StorageService(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<CockatooConfig>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _appImageRepo = services.GetRequiredService<ApplicationImageRepository>();
        _bullseyeAppRevisionRepo = services.GetRequiredService<BullseyeAppRevisionRepository>();
        _bullseyePatchRepo = services.GetRequiredService<BullseyePatchRepository>();
        _blogPostAttachmentRepo = services.GetRequiredService<BlogPostAttachmentRepository>();
        _userPrefRepo = services.GetRequiredService<UserPreferencesRepository>();
        _audnRevisionRepo = services.GetRequiredService<AUDNRevisionRepository>();
    }

    public string GetUrl(StorageFileModel model)
    {
        string location = $"api/v1/File/{model.Id}/Content";
        if (_config.Storage.FileApi.UseDirect == false)
        {
            location = model.Location;
        }
        while (location.Contains("//"))
        {
            location = location.Replace("//", "/");
        }

        location = _config.Storage.FileApi.Endpoint + (_config.Storage.FileApi.Endpoint.EndsWith('/') ? "" : "/") + location;

        return location;
    }

    public async Task<string?> GetUrl(ApplicationImageModel appImageModel)
    {
        if (appImageModel.IsManagedFile)
        {
            if (appImageModel.ManagedFileId == null)
            {
                throw new NoNullAllowedException(
                    $"{nameof(appImageModel.ManagedFileId)} for {appImageModel.Id} is null when it is required when {appImageModel.IsManagedFile} is true.");
            }
            var fileModel = await _storageFileRepo.GetById(appImageModel.ManagedFileId);
            if (fileModel == null)
            {
                throw new NoNullAllowedException(
                    $"Could not find {nameof(StorageFileModel)} with Id of {appImageModel.ManagedFileId} for {nameof(ApplicationImageModel)} {appImageModel.Id}");
            }

            return GetUrl(fileModel);
        }
        else
        {
            return appImageModel.Url;
        }
    }

    public async Task<string?> GetHash(ApplicationImageModel appImageModel)
    {
        if (appImageModel.IsManagedFile)
        {
            if (appImageModel.ManagedFileId == null)
            {
                throw new NoNullAllowedException(
                    $"{nameof(appImageModel.ManagedFileId)} for {appImageModel.Id} is null when it is required when {appImageModel.IsManagedFile} is true.");
            }
            var fileModel = await _storageFileRepo.GetById(appImageModel.ManagedFileId);
            if (fileModel == null)
            {
                throw new NoNullAllowedException(
                    $"Could not find {nameof(StorageFileModel)} with Id of {appImageModel.ManagedFileId} for {nameof(ApplicationImageModel)} {appImageModel.Id}");
            }

            return fileModel.Sha256Hash;
        }
        else
        {
            return appImageModel.Sha256Hash;
        }
    }

    public async Task<MemoryStream> GetContent(StorageFileModel model)
    {
        if (_config.Storage.S3.Enable)
        {
            var s3 = _services.GetRequiredService<S3Service>();
            return await s3.GetContent(model);
        }
        else
        {
            var location = ParseLocation(model);
            var bytes = await File.ReadAllBytesAsync(location);
            var ms = new MemoryStream(bytes);
            return ms;
        }
    }

    public async Task<Stream> GetStream(StorageFileModel model)
    {
        if (_config.Storage.S3.Enable)
        {
            var s3 = _services.GetRequiredService<S3Service>();
            return await s3.GetContentAsStream(model);
        }
        else
        {
            var location = ParseLocation(model);
            return File.Open(location, FileMode.Open);
        }
    }
    private string ParseLocation(StorageFileModel model)
    {
        var location = Path.Combine(
            _config.Storage.Local.Location,
            model.Location);
        var relative = Path.GetRelativePath(_config.Storage.Local.Location, location);
        if (relative.StartsWith("./") == false)
        {
            _log.Error($"Path {location} with Id {model.Id} tried to escape!");
        }
        if (location.StartsWith(_config.Storage.Local.Location) == false)
        {
            throw new Exception($"Location attempted to escape\n" +
                $"{nameof(location)}: {location}\n" +
                $"_config.Storage.Local.Location: {_config.Storage.Local.Location})");
        }
        if (!File.Exists(location))
            throw new Exception("NotFound");
        return location!;
    }

    public async Task<StorageFileModel> UploadFile(Stream content, string filename, long? length = null)
    {
        filename = Path.GetFileName(filename);
        var model = new StorageFileModel
        {
            ContentType = MimeTypes.GetMimeType(filename)
        };
        model.Location = $"{model.Id}/{filename}";

        content.Seek(0, SeekOrigin.Begin);
        if (_config.Storage.S3.Enable)
        {
            _log.Trace($"Uploading object to S3");
            var s3 = _services.GetRequiredService<S3Service>();
            var s3Obj = await s3.UploadObject(content, model.Location, length);
            model.Sha256Hash = s3Obj.ChecksumSHA256;
            if (string.IsNullOrEmpty(model.Sha256Hash))
            {
                content.Seek(0, SeekOrigin.Begin);
                model.Sha256Hash = CockatooHelper.GetSha256Hash(content);
            }
            model.SetSize(s3Obj.ContentLength);
        }
        else
        {
            var location = Path.Combine(_config.Storage.Local.Location, model.Location);
            using (var f = File.Open(location, FileMode.OpenOrCreate))
            {
                await content.CopyToAsync(f);
            }
            using (var f = File.Open(location, FileMode.Open))
            {
                model.Sha256Hash = CockatooHelper.GetSha256Hash(f);
            }
            model.SetSize(content.Length);
        }
        await _storageFileRepo.InsertOrUpdate(model);
        return model;
    }

    /// <summary>
    /// Delete a Storage File from the filesystem/S3 and the database.
    /// </summary>
    /// <param name="model">Instance of <see cref="StorageFileModel"/> to use.</param>
    /// <exception cref="NoNullAllowedException">Thrown when <see cref="S3Service.GetObject"/> returns <see langword="null"/> (only when <see cref="FeatureFlags.UseS3"/></exception>
    public async Task Delete(StorageFileModel model)
    {
        if (_config.Storage.S3.Enable)
        {
            var s3 = _services.GetRequiredService<S3Service>();
            var obj = await s3.GetObject(model);
            if (obj == null)
            {
                throw new NoNullAllowedException($"Failed to find provided model in S3 Bucket. ({nameof(S3Service)}.{nameof(s3.GetObject)} returned null)");
            }
            await s3.DeleteObject(model);
        }
        else
        {
            var location = ParseLocation(model);
            File.Delete(location);
        }
        await _storageFileRepo.Delete(model.Id);
    }

    /// <summary>
    /// Get a list of documents that are using the file specified.
    /// </summary>
    public async Task<List<object>> GetFileReferences(StorageFileModel file)
    {
        var result = new List<object>();

        var taskList = new List<Task>();
        // Application Image
        taskList.Add(new Task(
            () =>
            {
                var data = _appImageRepo.GetAllUsingFile(file).Result;
                lock (result)
                {
                    result.AddRange(data.Cast<object>());
                }
            }));
        #region Bullseye
        // Revision
        taskList.Add(new Task(
            () =>
            {
                var data = _bullseyeAppRevisionRepo.GetAllUsingFile(file).Result;
                lock (result)
                {
                    result.AddRange(data.Cast<object>());
                }
            }));
        // Patch
        taskList.Add(new Task(
            () =>
            {
                var data = _bullseyePatchRepo.GetAllUsingFile(file).Result;
                lock (result)
                {
                    result.AddRange(data.Cast<object>());
                }
            }));
        #endregion
        // Blog Post Attachment
        taskList.Add(new Task(
            () =>
            {
                var data = _blogPostAttachmentRepo.GetAllUsingFile(file).Result;
                lock (result)
                {
                    result.AddRange(data.Cast<object>());
                }
            }));
        // AutoUpdater.NET Revision
        taskList.Add(new Task(
            () =>
            {
                var data = _audnRevisionRepo.GetAllUsingFile(file).Result;
                lock (result)
                {
                    result.AddRange(data.Cast<object>());
                }
            }));
        // User Preferences
        taskList.Add(new Task(
            () =>
            {
                var data = _userPrefRepo.GetAllUsingFile(file).Result;
                lock (result)
                {
                    result.AddRange(data.Cast<object>());
                }
            }));

        foreach (var i in taskList)
            i.Start();
        await Task.WhenAll(taskList);

        return result;
    }
}
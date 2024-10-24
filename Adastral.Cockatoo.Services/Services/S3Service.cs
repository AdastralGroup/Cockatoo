using System.Diagnostics;
using System.Net;
using Amazon;
using Amazon.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class S3Service : BaseService
{
    private readonly IAmazonS3 _s3Client;
    private readonly CockatooConfig _config;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    public S3Service(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<CockatooConfig>();
        if (string.IsNullOrEmpty(_config.Storage.S3.ServiceUrl))
        {
            throw new Exception($"Property {nameof(_config.Storage.S3.ServiceUrl)} in {_config.Storage.S3.GetType()} is required.");
        }

        var conf = new AmazonS3Config()
        {
            ServiceURL = _config.Storage.S3.ServiceUrl,
        };
        if (_config.Storage.S3.NotUsingAWS)
        {
            conf.ForcePathStyle = true;
            if (!string.IsNullOrEmpty(_config.Storage.S3.AuthenticationRegion))
            {
                conf.AuthenticationRegion = _config.Storage.S3.AuthenticationRegion;
            }

            if (_config.Storage.S3.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                conf.UseHttp = true;
            }
        }
        _s3Client = new AmazonS3Client(_config.Storage.S3.AccessKeyId, _config.Storage.S3.AccessSecretKey, conf);
    }

    public override async Task InitializeAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.Storage.S3.BucketName))
            {
                throw new Exception($"Feature Flag {nameof(_config.Storage.S3.BucketName)} is required.");
            }

            _log.Debug($"Fetching buckets, just to make sure that the bucket {_config.Storage.S3.BucketName} exists.");
            var buckets = await _s3Client.ListBucketsAsync();
            bool bucketExists = false;
            foreach (var item in buckets.Buckets)
            {
                if (item.BucketName == _config.Storage.S3.BucketName)
                {
                    bucketExists = true;
                    break;
                }
            }

            if (!bucketExists)
            {
                _log.Info($"Bucket {_config.Storage.S3.BucketName} does not exist, creating in current region");
                await S3Bucket.CreateBucketAsync(_s3Client, _config.Storage.S3.BucketName);
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to initialize S3 Client\n{ex}");
            SentrySdk.CaptureException(ex);
        }
    }

    /// <summary>
    /// Download into memory the S3 Object then return it as a Memory Stream.
    /// </summary>
    public async Task<MemoryStream> GetContent(StorageFileModel model)
    {
        // Create a GetObject request
        var request = new GetObjectRequest
        {
            BucketName = _config.Storage.S3.BucketName,
            Key = model.Location,
        };

        // Issue request and remember to dispose of the response
        using GetObjectResponse response = await _s3Client.GetObjectAsync(request);
        if (response.HttpStatusCode == HttpStatusCode.NotFound)
            throw new Exception("NotFound");
        try
        {
            // Save object to local file
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            return ms;
        }
        catch (AmazonS3Exception ex)
        {
            _log.Error($"Failed to get {nameof(StorageFileModel)} {model.Id}. {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get the response stream from <see cref="GetObject(StorageFileModel)"/>
    /// </summary>
    public async Task<Stream> GetContentAsStream(StorageFileModel model)
    {
        GetObjectResponse response = await GetObject(model);
        return response.ResponseStream;
    }

    /// <summary>
    /// Get an object from S3 with the <see cref="StorageFileModel"/> provided.
    /// </summary>
    public Task<GetObjectResponse> GetObject(StorageFileModel model)
    {
        return GetObject(model.Location);
    }
    private async Task<GetObjectResponse> GetObject(string location)
    {
        // Create a GetObject request
        var request = new GetObjectRequest
        {
            BucketName = _config.Storage.S3.BucketName,
            Key = location,
        };

        // Issue request and remember to dispose of the response
        GetObjectResponse response = await _s3Client.GetObjectAsync(request);
        if (response.HttpStatusCode == HttpStatusCode.NotFound)
            throw new Exception("NotFound");
        return response;
    }

    /// <summary>
    /// Delete an object from S3 with the <see cref="StorageFileModel"/> provided.
    /// </summary>
    public async Task<DeleteObjectResponse> DeleteObject(StorageFileModel model)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _config.Storage.S3.BucketName,
            Key = model.Location
        };

        DeleteObjectResponse response = await _s3Client.DeleteObjectAsync(request);
        if (response.HttpStatusCode == HttpStatusCode.NotFound)
            throw new Exception("NotFound");
        return response;
    }

    /// <summary>
    /// Write the <paramref name="stream"/> temporarily to disk, then upload to S3, then once that successfully completed, then delete the temp file.
    /// </summary>
    public async Task FileWriteThenUploadMultipartObject(Stream stream, string location)
    {
        var tmpFileLocation = Path.GetTempFileName();
        _log.Debug($"[location={location}] Writing to {tmpFileLocation}");
        using (var tf = File.Open(tmpFileLocation, FileMode.OpenOrCreate))
        {
            await stream.CopyToAsync(tf);
        }
        await UploadMultipartObject(tmpFileLocation, location);
        _log.Debug($"[location={location}] Deleting temporary file {tmpFileLocation}");
        File.Delete(tmpFileLocation);
    }

    /// <summary>
    /// Upload a file via the location to S3.
    /// </summary>
    public async Task UploadMultipartObject(string diskLocation, string objectLocation)
    {
        var fileTransferUtility = new TransferUtility(_s3Client);
        var fileTransferUtilityRequest = new TransferUtilityUploadRequest
        {
            BucketName = _config.Storage.S3.BucketName,
            PartSize = 6291456, // 6 MB.
            Key = objectLocation,
            FilePath = diskLocation,
            ContentType = MimeTypes.GetMimeType(Path.GetFileName(objectLocation)),
            ChecksumAlgorithm = ChecksumAlgorithm.SHA256,
        };
        _log.Debug($"[objectLocation={objectLocation}] Uploading to AWS");
        await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
    }

    /// <summary>
    /// Upload an object into S3
    /// </summary>
    /// <remarks>
    /// When <see cref="Stream.Length"/> is equal to <c>0</c> or it doesn't equal <paramref name="length"/> (when provided), then <see cref="FileWriteThenUploadMultipartObject"/> will be used.
    /// </remarks>
    public async Task<GetObjectResponse> UploadObject(Stream stream, string location, long? length = null)
    {
        if (stream.Length == 0 || (length != null && length != stream.Length))
        {
            _log.Debug($"Writing to disk then uploading, since stream length ({stream.Length}) does not match the length provided ({length})");
            await FileWriteThenUploadMultipartObject(stream, location);
        }
        else
        {
            var fileTransferUtility = new TransferUtility(_s3Client);
            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = _config.Storage.S3.BucketName,
                InputStream = stream,
                PartSize = 6291456, // 6 MB.
                Key = location,
                ContentType = MimeTypes.GetMimeType(Path.GetFileName(location)),
            };
            _log.Debug($"[location={location}] Uploading to AWS with {nameof(TransferUtility)}");
            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
        }

        await Task.Delay(3000);

        return await GetObject(location);
    }

}
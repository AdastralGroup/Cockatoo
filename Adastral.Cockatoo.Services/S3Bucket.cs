using Amazon.Runtime.SharedInterfaces;
using Amazon.S3;
using Amazon.S3.Model;
using NLog;

namespace Adastral.Cockatoo.Services;

public static class S3Bucket
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Shows how to create a new Amazon S3 bucket.
    /// </summary>
    /// <param name="client">An initialized Amazon S3 client object.</param>
    /// <param name="bucketName">The name of the bucket to create.</param>
    /// <returns>A boolean value representing the success or failure of
    /// the bucket creation process.</returns>
    public static async Task<bool> CreateBucketAsync(IAmazonS3 client, string bucketName, S3Region? region = null)
    {
        try
        {
            var request = new PutBucketRequest
            {
                BucketName = bucketName
            };
            if (region != null)
            {
                request.BucketRegion = region;
            }
            else
            {
                request.UseClientRegion = true;
            }

            var response = await client.PutBucketAsync(request);
            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (AmazonS3Exception ex)
        {
            _log.Error($"Failed to create bucket {bucketName}. {ex.Message}");
            throw;
        }
    }
}
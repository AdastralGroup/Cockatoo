using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationImageModel : BaseGuidModel
{
    public ApplicationImageModel()
        : base()
    {
        ApplicationId = "";
        Sha256Hash = "".PadRight(64, '0');
    }
    public static string CollectionName => "application_image";
    /// <summary>
    /// Url for the image.
    /// </summary>
    public string? Url { get; set; }
    /// <summary>
    /// <see cref="ApplicationDetailModel.Id"/>
    /// </summary>
    public string ApplicationId { get; set; } = "";
    /// <summary>
    /// What kind of image is this?
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ApplicationImageKind Kind { get; set; }
    /// <summary>
    /// Sha256 Hash of the content at <see cref="Url"/>
    /// </summary>
    public string Sha256Hash { get; set; }
    /// <summary>
    /// Is the file for this image managed by Cockatoo? If so, then <see cref="StorageFileRepository"/> should be used
    /// to get the Sha256 of the file.
    /// </summary>
    public bool IsManagedFile { get; set; }
    /// <summary>
    /// <see cref="StorageFileModel.Id"/> when <see cref="IsManagedFile"/> is set to <see langword="true"/>
    /// </summary>
    [BsonIgnoreIfDefault]
    [BsonIgnoreIfNull]
    public string? ManagedFileId { get; set; }

    /// <summary>
    /// Update <see cref="Sha256Hash"/> so it has the hashed content of the response from <see cref="Url"/>
    /// </summary>
    /// <returns>Was it successful or not. Will return <see langword="false"/> when the response isn't a success.</returns>
    public async Task<bool> UpdateHash()
    {
        var client = new HttpClient();
        var res = await client.GetAsync(Url);
        if (res.IsSuccessStatusCode == false) {
            return false;
        }
        var content = res.Content.ReadAsByteArrayAsync().Result;
        Sha256Hash = CockatooHelper.GetSha256Hash(content);
        return false;
    }

    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Since Unix Epoch (Seconds, UTC)</para>
    /// </summary>
    public string UpdatedAt { get; set; } = "0";
    public long GetUpdatedAt() => long.Parse(UpdatedAt);
    public void SetUpdatedAt()
    {
        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }
}
public enum ApplicationImageKind
{
    
    [BsonRepresentation(BsonType.String)]
    Icon,
    [BsonRepresentation(BsonType.String)]
    Star,
    [BsonRepresentation(BsonType.String)]
    Wordmark,
    [BsonRepresentation(BsonType.String)]
    Background
}
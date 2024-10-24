using System.Text.Json;
using Adastral.Cockatoo.Common;
using kate.shared.Helpers;
using MongoDB.Bson;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BullseyeV1CacheModel
    : BaseGuidModel
{
    public const string CollectionName = "bullseye_cache_v1";

    public BullseyeV1CacheModel()
        : base()
    {
        TargetAppId = "";
        Content = GeneralHelper.Base64Encode("{}");
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        IsLive = false;
    }

    /// <summary>
    /// Timestamp when this Cache Model was created (Unix Epoch, UTC, Seconds)
    /// </summary>
    public BsonTimestamp CreatedAt { get; set; }

    /// <summary>
    /// Is this current cached model live/published?
    /// </summary>
    public bool IsLive { get; set; }

    /// <summary>
    /// Target App that this cached model is for.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppModel"/>
    /// </remarks>
    public string TargetAppId { get; set; }

    /// <summary>
    /// JSON of <see cref="BullseyeV1"/> encoded in Base64.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Decode and deserialize <see cref="Content"/>
    /// </summary>
    public BullseyeV1? GetContent()
    {
        var decoded = GeneralHelper.Base64Decode(Content);
        var data = JsonSerializer.Deserialize<BullseyeV1>(decoded, BaseService.SerializerOptions);
        return data;
    }

    /// <summary>
    /// Serialize and Encode ase Base64.
    /// </summary>
    public void SetContent(BullseyeV1 model)
    {
        var data = JsonSerializer.Serialize(model, BaseService.SerializerOptions);
        Content = GeneralHelper.Base64Encode(data);
    }
}
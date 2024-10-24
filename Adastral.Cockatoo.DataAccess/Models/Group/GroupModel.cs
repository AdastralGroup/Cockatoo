using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Adastral.Cockatoo.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupModel : BaseGuidModel
{
    public const string CollectionName = "group";
    public GroupModel()
        : base()
    {
        Name = "";
        CreatedAt = new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        Priority = int.MaxValue;
    }

    /// <summary>
    /// Name of this Group.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Format name to something that is not blank
    /// </summary>
    /// <returns><see cref="BaseGuidModel.Id"/> when <see cref="Name"/> is null or empty.</returns>
    public string FormatName()
    {
        return string.IsNullOrEmpty(Name)
            ?
            Id
            : Name;
    }

    /// <summary>
    /// Unix Timestamp when this Group was created (UTC, Seconds)
    /// </summary>
    [Required]
    [BsonRequired]
    [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
    public BsonTimestamp CreatedAt { get; set; }


    /// <summary>
    /// Ordering Priority. <see cref="uint.MaxValue"/> is the most important, and <see cref="uint.MinValue"/> is the least important.
    /// </summary>
    public uint Priority { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// Base Model that uses a <see cref="Guid"/> for its <c>Id</c> property.
/// </summary>
public class BaseGuidModel
    : IBaseGuidModel
{
    /// <inheritdoc/>
    [Required]
    [JsonRequired]
    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; }

    /// <summary>
    /// Create an instance of BaseGuidModel.
    /// </summary>
    public BaseGuidModel()
    {
        Id = Guid.NewGuid().ToString().ToLower();
    }
}

public interface IBaseGuidModel
{
    /// <summary>
    /// Id/Primary Key as a <see cref="Guid"/> turned into a string.
    /// </summary>
    [Required]
    [JsonRequired]
    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class AdminGroupV1InsertGlobalPermissionResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    /// <summary>
    /// Instance of <see cref="GroupModel"/> that had a permission granted on.
    /// </summary>
    [Required]
    [JsonRequired]
    [JsonPropertyName("group")]
    public GroupModel Group { get; set; }
    /// <summary>
    /// Instance of <see cref="GroupPermissionGlobalModel"/> that was inserted into the database.
    /// </summary>
    [Required]
    [JsonRequired]
    [JsonPropertyName("role")]
    public GroupPermissionGlobalModel Permission { get; set; }
    /// <summary>
    /// Does this already exist? If so, then <see cref="Role"/> is just an existing document instead of a new one.
    /// </summary>
    [Required]
    [JsonRequired]
    [JsonPropertyName("exists")]
    public bool AlreadyExists { get; set; }
    /// <summary>
    /// Amount of time in milliseconds that this took.
    /// </summary>
    [Required]
    [JsonRequired]
    [JsonPropertyName("duration")]
    public long Duration { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class AdminGroupV1InsertApplicationPermissionResponse
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
    /// Application that the permission was granted for.
    /// </summary>
    [Required]
    [JsonRequired]
    [JsonPropertyName("application")]
    public ApplicationDetailModel Application { get; set; }
    /// <summary>
    /// Instance of <see cref="GroupPermissionApplicationModel"/> that was inserted into the database.
    /// </summary>
    [Required]
    [JsonRequired]
    [JsonPropertyName("permission")]
    public GroupPermissionApplicationModel Permission { get; set; }
    /// <summary>
    /// Does this already exist? If so, then <see cref="Permission"/> is just an existing document instead of a new one.
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
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class AdminGroupV1DenyGlobalPermissionRequest
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("group")]
    public string GroupId { get; set; }
    [Required]
    [JsonRequired]
    [JsonPropertyName("kind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PermissionKind Kind { get; set; }
}
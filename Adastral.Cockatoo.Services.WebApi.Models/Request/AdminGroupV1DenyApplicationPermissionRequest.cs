using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class AdminGroupV1DenyApplicationPermissionRequest
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("group")]
    public string GroupId { get; set; }
    
    [Required]
    [JsonRequired]
    [JsonPropertyName("application")]
    public string ApplicationId { get; set; }
    
    [Required]
    [JsonRequired]
    [JsonPropertyName("kind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ScopedApplicationPermissionKind Kind { get; set; }
}
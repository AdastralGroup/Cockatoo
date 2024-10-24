using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class AdminGroupV1CreateRequest
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("priority")]
    public uint Priority { get; set; } = 0;

    [JsonPropertyName("initialUsers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? InitialUserIds { get; set; } = null;

    [JsonPropertyName("initialPermissionGrant")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PermissionKind>? InitialPermissionGrant { get; set; } = null;
    [JsonPropertyName("initialPermissionDeny")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PermissionKind>? InitialPermissionDeny { get; set; } = null;

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, BaseService.SerializerOptions);
    }
}
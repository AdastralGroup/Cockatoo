using System.Text.Json;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class ManageBullseyeV1GenerateCacheRequest
{
    [JsonPropertyName("appId")]
    public string AppId { get; set; } = "";
    [JsonPropertyName("publishedOnly")]
    public bool PublishedOnly { get; set; } = false;
    [JsonPropertyName("setLiveState")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? SetLiveState { get; set; } = null;

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, BaseService.SerializerOptions);
    }
}
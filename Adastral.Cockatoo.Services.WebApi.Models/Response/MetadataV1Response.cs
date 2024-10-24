using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class MetadataV1Response
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    public string Version { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceId { get; set; }
    public string PublicUrl { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartnerUrl { get; set; }
}
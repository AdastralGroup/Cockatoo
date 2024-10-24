using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class AdminGroupV1ListResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    [JsonPropertyName("items")]
    public List<AdminGroupV1DetailResponse> Items { get; set; } = [];
}
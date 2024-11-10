using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ManageClientApiKeyProviderV1ListResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    
    public List<ManageClientApiKeyProviderV1DetailsResponse> Items { get; set; } = [];
}
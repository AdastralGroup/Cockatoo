using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;


public class Select2SearchResponse
{
    [JsonPropertyName("results")]
    public List<Select2SearchResponseItem> Results { get; set; } = [];
}
public class Select2SearchResponseItem
{
    [JsonPropertyName("id")]
    public object Id { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
    [JsonPropertyName("selected")]
    public bool Selected { get; set; } = false;
}
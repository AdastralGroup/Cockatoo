using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ParameterErrorResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    [Required]
    [JsonRequired]
    [JsonPropertyName("message")]
    public required string Message { get; set; }
    [Required]
    [JsonRequired]
    [JsonPropertyName("parameter")]
    public required string ParameterName { get; set; }
}
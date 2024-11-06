using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common.Helpers;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class PropertyErrorResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    [Required]
    [JsonRequired]
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [Required]
    [JsonRequired]
    [JsonPropertyName("prop")]
    public string PropertyName { get; set; }
    [JsonPropertyName("propType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PropertyParentType { get; set; }

    public PropertyErrorResponse WithParentType(Type type)
    {
        PropertyParentType = CockatooHelper.FormatTypeName(type);
        return this
    }
}
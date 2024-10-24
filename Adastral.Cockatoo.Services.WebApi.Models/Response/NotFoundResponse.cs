using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Amazon.S3.Model;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class NotFoundResponse
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
}

public class NotFoundException : Exception
{
    public string PropertyName { get; private set; }
    public string? PropertyParentType { get; private set; }

    public NotFoundException(NotFoundResponse data)
        : base(data.Message)
    {
        PropertyName = data.PropertyName;
        PropertyParentType = data.PropertyParentType;
    }

    public override string ToString()
    {
        var s = base.ToString();
        if (!string.IsNullOrEmpty(PropertyName))
        {
            var data = new string[2]
            {
                $"{nameof(PropertyName)}: {PropertyName}", ""
            };
            if (!string.IsNullOrEmpty(PropertyParentType))
            {
                data[2] = $"{nameof(PropertyParentType)}: {PropertyParentType}";
            }

            s += "\n" + string.Join("\n", data.Where(v => v.Length > 0));
        }

        return s;
    }
}
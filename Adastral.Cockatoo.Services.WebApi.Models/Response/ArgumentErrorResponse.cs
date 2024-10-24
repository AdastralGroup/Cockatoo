using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ArgumentErrorResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
    [JsonPropertyName("argument")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Argument { get; set; }

    public ArgumentErrorResponse()
        : this(null, null)
    {}
    public ArgumentErrorResponse(string message)
        : this(message, null)
    {}
    public ArgumentErrorResponse(string? message, string? argument)
    {
        Message = message;
        Argument = argument;
    }
}
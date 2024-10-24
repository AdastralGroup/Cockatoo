using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

/// <summary>
/// Response that is used to compare the before and after state of an object.
/// </summary>
public class ComparisonResponse<T>
{
    [JsonPropertyName("_type")]
    public string Type => GetType().Name;
    /// <summary>
    /// Value of the Type before the change was made.
    /// </summary>
    [JsonPropertyName("before")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Before { get; set; }
    /// <summary>
    /// Value of the Type after the change was made.
    /// </summary>
    [JsonPropertyName("after")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? After { get; set; }

    public ComparisonResponse()
    { }
    public ComparisonResponse(T? before, T? after)
    {
        Before = before;
        After = after;
    }
}
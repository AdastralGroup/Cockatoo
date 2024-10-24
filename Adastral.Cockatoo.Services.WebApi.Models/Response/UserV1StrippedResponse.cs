using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class UserV1StrippedResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";
    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; } = 0;
    public void FromModel(UserModel model)
    {
        Id = model.Id;
        DisplayName = model.DisplayName;
        CreatedAt = model.GetCreatedAtTimestamp();
    }
}
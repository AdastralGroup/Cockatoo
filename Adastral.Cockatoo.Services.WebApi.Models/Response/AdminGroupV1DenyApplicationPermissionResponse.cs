using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class AdminGroupV1DenyApplicationPermissionResponse : AdminGroupV1InsertApplicationPermissionResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public new string Type => GetType().Name;
}
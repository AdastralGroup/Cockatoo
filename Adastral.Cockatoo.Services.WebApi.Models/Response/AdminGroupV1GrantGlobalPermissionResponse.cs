using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class AdminGroupV1GrantGlobalPermissionResponse : AdminGroupV1InsertGlobalPermissionResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public new string Type => GetType().Name;
}
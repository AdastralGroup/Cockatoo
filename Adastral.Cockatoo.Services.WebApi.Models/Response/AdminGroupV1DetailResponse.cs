using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class AdminGroupV1DetailResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.Empty.ToString();

    /// <inheritdoc cref="GroupModel.Name"/>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Timestamp when this Permission Group was created (Seconds since UTC Epoch)
    /// </summary>
    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; } = 0;

    /// <inheritdoc cref="GroupModel.Priority"/>
    [JsonPropertyName("priority")]
    public uint Priority { get; set; } = 0;

    /// <summary>
    /// List of users that are in this group.
    /// </summary>
    [JsonPropertyName("users")]
    public List<UserV1StrippedResponse> Users { get; set; } = [];

    /// <summary>
    /// List of global permissions that are associated with this group.
    /// </summary>
    [JsonPropertyName("globalPermissions")]
    public List<GroupPermissionGlobalModel> GlobalPermissions { get; set; } = [];
    /// <summary>
    /// List of application-scoped permissions that are associated with this group.
    /// </summary>
    [JsonPropertyName("applicationPermissions")]
    public List<GroupPermissionApplicationModel> ApplicationPermissions { get; set; } = [];
}
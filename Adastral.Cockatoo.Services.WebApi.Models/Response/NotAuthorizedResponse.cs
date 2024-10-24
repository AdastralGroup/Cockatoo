using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class NotAuthorizedResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("missingPermissions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PermissionKind>? MissingPermissions { get; set; } = null;
}

public class NotAuthorizedException : Exception
{
    public ReadOnlyCollection<PermissionKind>? MissingPermissions { get; private set; }

    public NotAuthorizedException(NotAuthorizedResponse data)
        : base(data.Message)
    {
        MissingPermissions = data.MissingPermissions?.AsReadOnly();
    }

    public override string ToString()
    {
        var s = base.ToString();
        if (MissingPermissions?.Count > 0)
        {
            s += $"\nMissing permissions: {string.Join(", ", MissingPermissions.Select(v => v.ToString()))}";
        }

        return s;
    }
}
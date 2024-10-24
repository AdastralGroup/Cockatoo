using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

/// <summary>
/// Delete a Bullseye App and it's associated files (when <paramref name="IncludeResources"/> is <see langword="true"/>)
/// </summary>
/// <remarks>
/// Requires the <see cref="PermissionKind.BullseyeDeleteApp"/> permission.
/// </remarks>
public class ManageBullseyeV1DeleteRequest
{
    /// <summary>
    /// <see cref="BullseyeAppModel.ApplicationDetailModelId"/>
    /// </summary>
    [Required]
    [JsonPropertyName("id")]
    public string AppId { get; set; }
    /// <summary>
    /// When <see langword="true"/>, the files used by this app will be deleted.
    /// </summary>
    [DefaultValue(false)]
    [JsonPropertyName("includeResources")]
    public bool IncludeResources { get; set; } = false;
}
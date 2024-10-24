using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class ManageBullseyeV1UpdateRevisionRequest
{
    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyeAppRevisionModel.Id"/> to update.
    /// </summary>
    [Required]
    [JsonPropertyName("id")]
    public string RevisionId { get; set; } = "";

    /// <summary>
    /// When not <see langword="null"/>, then the value of <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyeAppRevisionModel.PreviousRevisionId"/>
    /// will be updated to this value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to set the value of <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyeAppRevisionModel.PreviousRevisionId"/> to <see langword="null"/>,
    /// then you can either set this value to an empty string <b>or</b>, set <see cref="DoUpdatePreviousRevisionId"/> to <see langword="true"/> and
    /// this to <see langword="null"/>.
    /// </para>
    /// <para>
    /// Requires <see cref="PermissionKind.BullseyeUpdatePreviousRevision"/>
    /// </para>
    /// </remarks>
    [DefaultValue(null)]
    [JsonPropertyName("previousRevisionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PreviousRevisionId { get; set; }

    /// <summary>
    /// When <see langword="true"/>, then the value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyeAppRevisionModel.PreviousRevisionId"/> will
    /// be set to <see langword="null"/>.
    /// </summary>
    [DefaultValue(null)]
    [JsonPropertyName("clearPreviousRevisionId")]
    public bool? ClearPreviousRevisionId { get; set; }

    /// <summary>
    /// When not <see langword="null"/>, then the value of <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyeAppRevisionModel.IsLive"/> will
    /// be updated to this value.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BullseyeUpdateRevisionLiveState"/>
    /// </remarks>
    [JsonPropertyName("isLive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public bool? IsLive { get; set; }
}
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class ManageBullseyeV1RegisterPatchRequest
{
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.FromRevisionId"/>
    /// </summary>
    [JsonPropertyName("from")]
    public string FromRevisionId { get; set; } = "";
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.ToRevisionId"/>
    /// </summary>
    [JsonPropertyName("to")]
    public string ToRevisionId { get; set; } = "";
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.StorageFileId"/>
    /// </summary>
    [JsonPropertyName("patch")]
    public string PatchFileId { get; set; } = "";
    /// <summary>
    /// Value for <see cref="Adastral.Cockatoo.DataAccess.Models.BullseyePatchModel.PeerToPeerStorageFileId"/> (optional)
    /// </summary>
    [JsonPropertyName("p2p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PeerToPeerFileId { get; set; }
}
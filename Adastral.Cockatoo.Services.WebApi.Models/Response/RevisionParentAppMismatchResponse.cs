using System.Text;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

/// <summary>
/// Used when BullseyeAppId for Revision A does not match Revision B
/// </summary>
public class RevisionParentAppMismatchResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;
    public class RevisionParentAppMismtchItem
    {
        [JsonPropertyName("_type")]
        public string Type => GetType().Name;
        /// <summary>
        /// <see cref="BullseyeAppRevisionModel.Id"/>
        /// </summary>
        [JsonPropertyName("revision")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RevisionId { get; set; }
        /// <summary>
        /// <see cref="ApplicationDetailModel.Id"/>
        /// </summary>
        [JsonPropertyName("app")]
        public string AppId { get; set; }
        /// <summary>
        /// Name of the property that the Revisio Id was defined on.
        /// </summary>
        [JsonPropertyName("prop")]
        public string PropertyName { get; set; }
        /// <summary>
        /// Type that the <see cref="PropertyName"/> is on
        /// </summary>
        [JsonPropertyName("propParent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PropertyParent { get; set; }
    }
    /// <summary>
    /// List of mismatches.
    /// </summary>
    [JsonPropertyName("items")]
    public List<RevisionParentAppMismtchItem> Items { get; set; } = [];

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    public string BuildMessage()
    {
        var i = string.Join(", ", Items.Select(v => $"{v.PropertyName}: {v.AppId}"));
        return $"Revisions provided are from different apps. ({i})";
    }
}
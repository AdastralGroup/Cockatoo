using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class UserV1CReateTokenRequest
{
    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.UserModel.Id"/>
    /// </summary>
    [Required]
    [JsonPropertyName("user")]
    public string UserId { get; set; } = "";
    /// <summary>
    /// Unix Timestamp when the token should expire. (UTC, Seconds)
    /// </summary>
    /// <remarks>
    /// Will never expire when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ExpiresAt { get; set; } = null;
}
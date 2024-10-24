using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.DataAccess.Models;

public class SouthbankV1
{
    [JsonPropertyName("sb_ver")]
    public string Version { get; set; } = "0.0.1";
    [JsonPropertyName("dl_url")]
    public string DownloadUrl { get; set; } = "";
    [JsonPropertyName("games")]
    public Dictionary<string, SouthbankV1GameItem> Games { get; set; } = new();
}
public class SouthbankV1GameItem
{
    /// <summary>
    /// Display name for the game. If you wish to get the game "id", the use the key in <see cref="SouthbankV1.Games"/>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    /// <summary>
    /// NOTE Seems like an enum casted into an int. Not sure what the values are -kate
    /// </summary>
    [JsonPropertyName("versioning")]
    public int VersionMethod { get; set; }
    [JsonPropertyName("belmont")]
    public SouthbankV1BelmontDetails BelmontDetails { get; set; } = new();
}
public class SouthbankV1BelmontDetails
{
    /// <summary>
    /// Icon Url
    /// </summary>
    [JsonPropertyName("icon")]
    public string? IconUrl { get; set; }
    /// <summary>
    /// Star Image Url (idk what this is, ask intcoms)
    /// </summary>
    [JsonPropertyName("star")]
    public string? StarUrl { get; set; }
    /// <summary>
    /// Wordmark Image Url
    /// </summary>
    [JsonPropertyName("wordmark")]
    public string? WordmarkUrl { get; set; }
    /// <summary>
    /// Background Image Url
    /// </summary>
    [JsonPropertyName("bg")]
    public string? BackgroundUrl { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("dark")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorDark { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("light")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorLight { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("main")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorMain { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("accent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorAccent { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("secondary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorSecondary { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("lightfg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorLightForeground { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("click")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorClick { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("click_t")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorClickT { get; set; }
}
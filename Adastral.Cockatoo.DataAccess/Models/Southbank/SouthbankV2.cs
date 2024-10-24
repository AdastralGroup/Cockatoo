using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.DataAccess.Models;

public class SouthbankV2
{
    [JsonPropertyName("sb_ver")]
    public string Version { get; set; } = "0.0.2";
    [JsonPropertyName("dl_url")]
    public string DownloadUrl { get; set; } = "";
    [JsonPropertyName("games")]
    public Dictionary<string, SouthbankV2GameItem> Games { get; set; } = new();
}
public class SouthbankV2GameItem
{
    /// <summary>
    /// Display name for the game. If you wish to get the game "id", the use the key in <see cref="SouthbankV2.Games"/>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    /// <summary>
    /// <c>0</c> for kachemak.
    /// </summary>
    [JsonPropertyName("versioning")]
    public int VersionMethod { get; set; } = 0;
    [JsonPropertyName("belmont")]
    public SouthbankV2BelmontDetails BelmontDetails { get; set; } = new();
}
public class SouthbankV2BelmontDetails
{
    /// <summary>
    /// <para>Icon Url</para>
    ///
    /// <para>This array will only have 2 items. 0th will be the url, 1st will be the sha256 hash.</para>
    /// </summary>
    [JsonPropertyName("icon")]
    [MaxLength(2)]
    [MinLength(2)]
    public string?[] IconUrl { get; set; } = new string?[2];
    /// <summary>
    /// <para>Star Image Url (idk what this is, ask intcoms</para>
    ///
    /// <para>This array will only have 2 items. 0th will be the url, 1st will be the sha256 hash.</para>
    /// </summary>
    [JsonPropertyName("star")]
    [MaxLength(2)]
    [MinLength(2)]
    public string?[] StarUrl { get; set; } = new string?[2];
    /// <summary>
    /// <para>Wordmark Image Url</para>
    ///
    /// <para>This array will only have 2 items. 0th will be the url, 1st will be the sha256 hash.</para>
    /// </summary>
    [JsonPropertyName("wordmark")]
    [MaxLength(2)]
    [MinLength(2)]
    public string?[] WordmarkUrl { get; set; } = new string?[2];
    /// <summary>
    /// <para>Background Image Url</para>
    ///
    /// <para>This array will only have 2 items. 0th will be the url, 1st will be the sha256 hash.</para>
    /// </summary>
    [JsonPropertyName("bg")]
    [MaxLength(2)]
    [MinLength(2)]
    public string?[] BackgroundUrl { get; set; } = new string?[2];
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
    [JsonPropertyName("secondary")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorSecondary { get; set; }
    /// <summary>
    /// Hex Color Code, like <c>#9020ff</c>
    /// </summary>
    [JsonPropertyName("accent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ColorAccent { get; set; }
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
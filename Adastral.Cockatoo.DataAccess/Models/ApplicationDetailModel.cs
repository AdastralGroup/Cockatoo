using System.ComponentModel;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationDetailModel : BaseGuidModel
{
    public static string CollectionName => "application_detail";

    /// <summary>
    /// Only applies when <see cref="Type"/> is set to <see cref="ApplicationDetailType.InternalApp"/>
    /// </summary>
    [JsonPropertyName("version")]
    public string? LatestVersion { get; set; }
    /// <summary>
    /// Not Required when <see cref="Type"/> is set to <see cref="ApplicationDetailType.InternalApp"/>
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
    [BsonRepresentation(BsonType.String)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("type")]
    public ApplicationDetailType Type { get; set; }
    /// <summary>
    /// Key for this game when generating <see cref="SouthbankV1"/> or <see cref="SouthbankV2"/>.
    /// When this is <see langword="null"/>, it will not be included when generating either of the previously mentioned southbank classes.
    /// </summary>
    public string? SouthbankAppId { get; set; }

    /// <summary>
    /// Required when <see cref="Type"/> is set to <see cref="ApplicationDetailType.Kachemak"/>
    /// </summary>
    [JsonPropertyName("appvar")]
    public AppVarModel AppVarData { get; set; } = new();
    /// <summary>
    /// When set to <see langword="true"/>, the requesting user must have authoritive access to get anything about it. This application will also be hidden when fetching all the available ones.
    /// </summary>
    [JsonIgnore]
    [DefaultValue(false)]
    public bool IsPrivate { get; set; } = false;
    /// <summary>
    /// When set to <see langword="true"/>, it will not be included in Southbank generation or when trying to view available applications when not logged in.
    /// </summary>
    [JsonIgnore]
    [DefaultValue(false)]
    public bool IsHidden { get; set; } = false;
    /// <summary>
    /// Is this application managed by Cockatoo? (e.g; all files are handled/managed by Cockatoo)
    /// </summary>
    [DefaultValue(false)]
    public bool Managed { get; set; } = false;
    /// <summary>
    /// Can this application be managed by Cockatoo?
    /// </summary>
    public static bool CanBeManaged(ApplicationDetailType type)
    {
        return type == ApplicationDetailType.Kachemak;
    }

    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Since Unix Epoch (Seconds, UTC)</para>
    /// </summary>
    public string UpdatedAt { get; set; } = "0";

    public long GetUpdatedAt()
    {
        return long.Parse(UpdatedAt);
    }

    public void SetUpdatedAt()
    {
        UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }
}

public enum ApplicationDetailType
{
    /// <summary>
    /// Use this when you are only using Cockatoo to internally keep track of an applications version.
    /// </summary>
    [Description("Use this when you are only using Cockatoo to internally keep track of an applications version.")]
    [BsonRepresentation(BsonType.String)]
    [EnumDisplayName("Internal Application")]
    InternalApp,
    /// <summary>
    /// Kachemak/Adastral/Cockatoo Versioning System.
    /// </summary>
    [Description("Kachemak/Adastral/Cockatoo Versioning System.")]
    [BsonRepresentation(BsonType.String)]
    Kachemak,
    /// <summary>
    /// <see cref="AppVarRemoteDetail.VersionsUrl"/> must be the Github Release Listing API Url, like <c>https://api.github.com/repositories/805393469/releases</c>
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    [Description("Remote Versions Url must be the Github Release Listing API Url, like `https://api.github.com/repositories/805393469/releases`.")]
    [EnumDisplayName("GitHub Releases")]
    GithubReleases,
    /// <summary>
    /// Uses the versioning system from <see href="https://github.com/ravibpatel/AutoUpdater.NET">AutoUpdater.NET</see>
    /// </summary>
    [Description("Uses the versioning system from [AutoUpdater.NET](https://github.com/ravibpatel/AutoUpdater.NET)")]
    [BsonRepresentation(BsonType.String)]
    [EnumDisplayName("AutoUpdater.NET")]
    AutoUpdaterDotNet
}

public class AppVarModel
{
    [JsonPropertyName("mod")]
    public AppVarModDetail Mod { get; set; } = new();
    [JsonPropertyName("remote")]
    public AppVarRemoteDetail Remote { get; set; } = new();
}

public class AppVarModDetail
{
    /// <summary>
    /// Source Mod Directory Name for this application
    /// </summary>
    [JsonPropertyName("sm_name")]
    public string SourceModName { get; set; } = "";
    /// <summary>
    /// Shortened name that is used as a prefix for an applications archives/patches, like <c>of</c> for Open Fortress, since it's patch filenames are like <c>of19-21.pwr</c>
    /// </summary>
    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = "";
    /// <summary>
    /// Stylized version of the Application Name to use in a Launcher or Updater.
    /// </summary>
    [JsonPropertyName("name_stylized")]
    public string NameStylized { get; set; } = "";

    /// <summary>
    /// Base Steam App ID that this Application depends on to run.
    /// </summary>
    [JsonPropertyName("base_appid")]
    [BsonRepresentation(BsonType.String)]
    public uint BaseAppId { get; set; } = 0;

    /// <summary>
    /// An array of required Steam App Ids that are required before launching this application.
    /// </summary>
    [JsonPropertyName("required_appids")]
    public uint[] RequiredAppIds { get; set; } = Array.Empty<uint>();

    /// <summary>
    /// Is Proton required for this application?
    /// </summary>
    [JsonPropertyName("require_proton")]
    public bool RequireProton { get; set; } = false;
}

public class AppVarRemoteDetail
{
    /// <summary>
    /// Base URL for any relative downloads that are defined in the versions file.
    /// </summary>
    [JsonPropertyName("base_url")]
    public string BaseUrl { get; set; } = "";
    /// <summary>
    /// Version Details URL. Must be bullseye-formatted when <see cref="ApplicationDetailType.Kachemak"/> is used.
    /// </summary>
    [JsonPropertyName("versions_url")]
    public string VersionsUrl { get; set; } = "";
}
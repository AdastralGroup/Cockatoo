using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Adastral.Cockatoo.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;

public class AUDNRevisionModel : BaseGuidModel
{
    public const string CollectionName = "application_revision_AutoUpdaterDotNet";
    public AUDNRevisionModel() : base()
    {
        ApplicationId = Guid.Empty.ToString();
        Version = "0.0.0.0";
        StorageFileId = Guid.Empty.ToString();
        IsEnabled = false;
        Mandatory = false;
        MandatoryKind = AUDNMandatoryKind.Normal;
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    
    /// <summary>
    /// Id for the <see cref="ApplicationDetailModel"/> that this instance is for.
    /// </summary>
    [Required]
    [BsonRequired]
    [JsonRequired]
    public string ApplicationId { get; set; }

    /// <summary>
    /// Version. Formatted like <c>X.X.X.X</c>
    /// </summary>
    [Required]
    [BsonRequired]
    [JsonRequired]
    public string Version { get; set; }

    /// <summary>
    /// Id for the Storage File that contains the update.
    /// </summary>
    [Required]
    [BsonRequired]
    [JsonRequired]
    public string StorageFileId { get; set; }

    /// <summary>
    /// When <see langword="true"/>, this revision is publicly available (unless the application is private, or can only be read by a specific organisation)
    /// </summary>
    [BsonIgnoreIfDefault]
    [DefaultValue(false)]
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// <b>Optional</b>: You can provide the path of the executable if it was changed in the update. It should be relative to the installation directory of the application.
    /// For example, if the new executable is located inside the bin folder of the installation directory, then you should provide it as shown below.
    /// <code>
    /// &lt;executable&gt;bin\AutoUpdaterTest.exe&lt;/executable&gt;
    /// </code>
    /// </summary>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// <b>Optional:</b> Launch arguments for the <see cref="ExecutablePath"/>
    /// </summary>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExecutableLaunchArguments { get; set; }
    /// <summary>
    /// <para><b>Optional:</b> You need to provide URL of the change log of your application between changelog tags.</para>
    /// If you don't provide the URL of the changelog then update dialog won't show the change log.
    /// </summary>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChangelogUrl { get; set; }
    /// <summary>
    /// <para>You can set this to true if you don't want user to skip this version.</para>
    /// This will ignore Remind Later and Skip options and hide both Skip and Remind Later button on update dialog.
    /// </summary>
    [DefaultValue(false)]
    public bool Mandatory { get; set; }
    /// <summary>
    /// You can provide mode attribute on mandatory element to change the behaviour of the mandatory flag.
    /// </summary>
    [DefaultValue(AUDNMandatoryKind.Normal)]
    public AUDNMandatoryKind MandatoryKind { get; set; }
    /// <summary>
    /// <para><b>Optional:</b> You can also provide minVersion attribute on mandatory element.</para>
    /// When you provide it, Mandatory option will be triggered only if the installed version of the app is less than the minimum version you specified here.
    /// </summary>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MandatoryMinimumVersion { get; set; }

    /// <summary>
    /// Timestamp when this model was created at (UTC, Seconds)
    /// </summary>
    [BsonRequired]
    [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
    public BsonTimestamp CreatedAt { get; set; }
    /// <summary>
    /// Timestamp when this model was last updated (UTC, Seconds)
    /// </summary>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
    public BsonTimestamp? UpdatedAt { get; set; }
}

public enum AUDNMandatoryKind
{
    /// <summary>
    /// In this mode, it ignores Remind Later and Skip values set previously and hide both buttons.
    /// </summary>
    [XmlEnum("0")]
    Normal = 0,
    /// <summary>
    /// Hide the Close button on update dialog
    /// </summary>
    [XmlEnum("1")]
    Forced = 1,
    /// <summary>
    /// In this mode, it will start downloading and applying update without showing standard update dialog in addition to
    /// Forced mode behaviour.
    /// </summary>
    [XmlEnum("2")]
    ForcedDownload = 2
}
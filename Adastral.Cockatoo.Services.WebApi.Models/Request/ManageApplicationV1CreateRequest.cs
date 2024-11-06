using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class ManageApplicationV1CreateRequest
{
    /// <summary>
    /// Name for the application. Must be unique and is required.
    /// </summary>
    [Required]
    [JsonRequired]
    public string Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApplicationDetailType? Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Private { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Hidden { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Managed { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CreateRequest_AppVar? AppVarData { get; set; }

    public class CreateRequest_AppVar
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CreateRequest_AppVarMod? Mod { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CreateRequest_AppVarRemote? ExternalMetadata { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<ApplicationImageKind, string>? ImageUrls { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<ApplicationImageKind, string>? ImageIds { get; set; }
        [DefaultValue(true)]
        public bool PrioritizeImageId { get; set; } = true;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<ApplicationColorKind, string>? Colors { get; set; }
    }
    public class CreateRequest_AppVarMod
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceModName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ShortName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? StylizedName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [BsonRepresentation(BsonType.String)]
        public uint? BaseAppId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint[]? RequiredAppIds { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? RequireProton { get; set; }

        public void InsertInto(AppVarModDetail target)
        {
            if (SourceModName != null)
            {
                target.SourceModName = SourceModName.Trim();
            }
            if (ShortName != null)
            {
                target.ShortName = ShortName.Trim();
            }
            if (StylizedName != null)
            {
                target.NameStylized = StylizedName.Trim();
            }
            if (BaseAppId != null)
            {
                target.BaseAppId = (uint)BaseAppId;
            }
            if (RequiredAppIds != null)
            {
                target.RequiredAppIds = RequiredAppIds;
            }
            if (RequireProton != null)
            {
                target.RequireProton = RequireProton == true;
            }
        }
    }
    public class CreateRequest_AppVarRemote
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? BaseUrl { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VersionsUrl { get; set; }

        public void InsertInto(AppVarRemoteDetail target)
        {
            if (BaseUrl != null)
            {
                target.BaseUrl = BaseUrl.Trim();
            }
            if (VersionsUrl != null)
            {
                target.VersionsUrl = VersionsUrl.Trim();
            }
        }
    }
}

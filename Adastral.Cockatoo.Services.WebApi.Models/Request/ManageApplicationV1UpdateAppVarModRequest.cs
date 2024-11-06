using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class ManageApplicationV1UpdateAppVarModRequest
{
    [Required]
    [JsonRequired]
    public string ApplicationId { get; set; }

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
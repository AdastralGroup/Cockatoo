using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ManageBullseyeV1DeleteRevisionResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [BsonIgnore]
    [XmlIgnore]
    [SoapIgnore]
    public string Type => GetType().Name;
    public string RequestRevisionId { get; set; } = "";
    public BullseyeAppRevisionModel? DeletedRevision { get; set; } = null;
    public ExceptionWebResponse? DeletedRevisionException { get; set; } = null;
    public ComparisonResponse<BullseyeAppModel>? BullseyeAppComparison { get; set; } = null;
    public ExceptionWebResponse? BullseyeAppComparisonException { get; set; } = null;
    /// <summary>
    /// List of all the files in the database that were deleted (will only be populated if <see cref="BullseyeDeleteAppRequest.DeleteStorageResources"/> is <see langword="true"/>)
    /// </summary>
    public List<StorageFileModel> DeletedFiles { get; set; } = [];
    /// <summary>
    /// <para><b>Dictionary of excetpions that were caught when deleting files.</b></para>
    /// <para><b>Key:</b> <see cref="StorageFileModel.Id"/></para>
    /// <para><b>Value:</b> <see cref="Exception"/> turned into <see cref="ExceptionWebResponse"/></para>
    /// </summary>
    public Dictionary<string, ExceptionWebResponse> DeleteFileExceptions { get; set; } = new();
    public List<BullseyePatchModel> DeletedPatches { get; set; } = [];
    public Dictionary<string, ExceptionWebResponse> DeletePatchExceptions { get; set; } = new();
    public ExceptionWebResponse? GenerateCacheException { get; set; } = null;
}
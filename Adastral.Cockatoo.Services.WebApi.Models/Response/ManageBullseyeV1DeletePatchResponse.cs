using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ManageBullseyeV1DeletePatchResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [BsonIgnore]
    [XmlIgnore]
    [SoapIgnore]
    public string Type => GetType().Name;

    public bool Success { get; set; }
    
    /// <summary>
    /// Requested <see cref="BullseyePatchModel.Id"/> to be deleted.
    /// </summary>
    public string RequestPatchId { get; set; } = "";
    
    /// <summary>
    /// Instance of <see cref="BullseyePatchModel"/> that was deleted from the database.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BullseyePatchModel? DeletedPatch { get; set; }
    /// <summary>
    /// Exception encountered while trying to delete <see cref="DeletedPatch"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExceptionWebResponse? DeletedPatchException { get; set; }
    
    /// <summary>
    /// List of all the files in the database that were deleted (will only be populated if <see cref="BullseyeDeleteAppRequest.DeleteStorageResources"/> is <see langword="true"/>)
    /// </summary>
    public List<StorageFileModel> DeletedFiles { get; set; } = [];
    
    /// <summary>
    /// <para><b>Dictionary of exceptions that were caught when deleting files.</b></para>
    /// <para><b>Key:</b> <see cref="StorageFileModel.Id"/></para>
    /// <para><b>Value:</b> <see cref="Exception"/> turned into <see cref="ExceptionWebResponse"/></para>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ExceptionWebResponse>? DeleteFileExceptions { get; set; }
}
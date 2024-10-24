using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ManageBullseyeV1DeleteResponse
{
    /// <summary>
    /// Request that was used to delete the Bullseye App
    /// </summary>
    public ManageBullseyeV1DeleteRequest Request { get; set; } = new();

    /// <summary>
    /// Instance of <see cref="BullseyeAppModel"/> in the database.
    /// </summary>
    public BullseyeAppModel? BullseyeAppModel { get; set; }
    /// <summary>
    /// Exception that was thrown when trying to fetch and delete <see cref="BullseyeAppModel"/> (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? BullseyeAppModelException { get; set; }
    /// <summary>
    /// Associated instance of <see cref="ApplicationDetailModel"/> for the provided Bullseye App.
    /// </summary>
    public ApplicationDetailModel? ApplicationDetailModel { get; set; }
    /// <summary>
    /// Exception that was thrown when trying to fetch <see cref="ApplicationDetailModel"/> (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? ApplicationDetailModelException { get; set; }
    /// <summary>
    /// List of all the files in the database that were deleted (will only be populated if <see cref="BullseyeDeleteAppRequest.DeleteStorageResources"/> is <see langword="true"/>)
    /// </summary>
    public List<StorageFileModel> DeletedFiles { get; set; } = [];
    /// <summary>
    /// List of all the instances of <see cref="BullseyeAppRevisionModel"/> that were deleted from the database.
    /// </summary>
    public List<BullseyeAppRevisionModel> DeletedRevisions { get; set; } = [];
    /// <summary>
    /// Dictionary of exceptions that were caught when finding revisions to delete.
    /// <para><b>Key:</b> <see cref="BullseyeAppRevisionModel.Id"/></para>
    /// <para><b>Value:</b> <see cref="Exception"/> turned into <see cref="ExceptionWebResponse"/></para>
    /// </summary>
    public Dictionary<string, ExceptionWebResponse> DeleteRevisionExceptions { get; set; } = new();
    /// <summary>
    /// <para><b>Dictionary of excetpions that were caught when deleting files.</b></para>
    /// <para><b>Key:</b> <see cref="StorageFileModel.Id"/></para>
    /// <para><b>Value:</b> <see cref="Exception"/> turned into <see cref="ExceptionWebResponse"/></para>
    /// </summary>
    public Dictionary<string, ExceptionWebResponse> DeleteFileExceptions { get; set; } = new();


    #region Bullseye Cache Models
    /// <summary>
    /// Latest instance of <see cref="BullseyeV1CacheModel"/> that was generated.
    /// </summary>
    public BullseyeV1CacheModel? LatestV1CacheModel { get; set; } = null;
    /// <summary>
    /// Latest instance of <see cref="BullseyeV1CacheModel"/> that was generated where <see cref="BullseyeV1CacheModel.IsLive"/> is <see langword="true"/>
    /// </summary>
    public BullseyeV1CacheModel? LatestLiveV1CacheModel { get; set; } = null;
    /// <summary>
    /// Latest instance of <see cref="BullseyeV2CacheModel"/> that was generated.
    /// </summary>
    public BullseyeV2CacheModel? LatestV2CacheModel { get; set; } = null;

    /// <summary>
    /// Latest instance of <see cref="BullseyeV2CacheModel"/> that was generated where <see cref="BullseyeV2CacheModel.IsLive"/> is <see langword="true"/>
    /// </summary>
    public BullseyeV2CacheModel? LatestLiveV2CacheModel { get; set; } = null;

    /// <summary>
    /// Exception caught when fetching data for <see cref="LatestV1CacheModel"/> (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? LatestV1CacheModelException { get; set; } = null;
    /// <summary>
    /// Exception caught when fetching data for <see cref="LatestLiveV1CacheModel"/> (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? LatestLiveV1CacheModelException { get; set; } = null;
    /// <summary>
    /// Exception caught when fetching data for <see cref="LatestV2CacheModel"/> (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? LatestV2CacheModelException { get; set; } = null;
    /// <summary>
    /// Exception caught when fetching data for <see cref="LatestLiveV2CacheModel"/> (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? LatestLiveV2CacheModelException { get; set; } = null;

    /// <summary>
    /// Exception caught when deleting all Bullseye v1 Cache Models for the App provided. (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? DeleteV1CacheModelException { get; set; } = null;
    /// <summary>
    /// Exception caught when deleting all Bullseye v2 Cache Models for the App provided. (<see langword="null"/> when there is none)
    /// </summary>
    public ExceptionWebResponse? DeleteV2CacheModelException { get; set; } = null;
    #endregion
}
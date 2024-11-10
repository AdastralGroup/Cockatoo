using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ManageClientApiKeyProviderV1DetailsResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;

    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.ClientApiKeyProviderModel.Id"/>
    /// </summary>
    public string Id { get; set; } = Guid.Empty.ToString();
    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.ClientApiKeyProviderModel.DisplayName"/>
    /// </summary>
    public string DisplayName { get; set; } = Guid.Empty.ToString();

    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.ClientApiKeyProviderModel.CreatedAt"/>
    /// </summary>
    public long CreatedAt { get; set; }
    
    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.ClientApiKeyProviderModel.BrandIconFileId"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BrandIconFileId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StorageFileModel? BrandIconFile { get; set; }

    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.ClientApiKeyProviderModel.IsActive"/>
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// <see cref="Adastral.Cockatoo.DataAccess.Models.ClientApiKeyProviderModel.PublicKeyXml"/>
    /// </summary>
    public string PublicKeyXml { get; set; } = "";
}
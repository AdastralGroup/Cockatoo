using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class ManageApplicationV1CreateResponse
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;

    [Required]
    [JsonRequired]
    public ApplicationDetailModel Application { get; set; }

    public List<ApplicationColorModel> Colors { get; set; } = [];
    public List<ApplicationImageModel> Images { get; set; } = [];
    public Dictionary<string, StorageFileModel> Files { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<InnerErrorResponseItem>? Errors { get; set; }

    public class InnerErrorResponseItem
    {
        public string Namespace { get; set; }
        public string Message { get; set; }

        public InnerErrorResponseItem()
        : this("", "")
        {}
        public InnerErrorResponseItem(string ns, string message)
        {
            this.Namespace = ns;
            Message = message;
        }
    }
}
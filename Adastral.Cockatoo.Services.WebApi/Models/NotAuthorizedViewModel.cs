using System.ComponentModel;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Services.WebApi.Models.Response;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public class NotAuthorizedViewModel : NotAuthorizedResponse
{
    [DefaultValue(false)]
    [JsonIgnore]
    public bool ShowLoginButton { get; set; } = false;
    [DefaultValue(true)]
    [JsonIgnore]
    public bool IncludeLayout { get; set; } = true;
}
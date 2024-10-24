using System.Net;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    public HttpRequestMessage ManageApplicationV1SubmitAUDNRevision(string appId, Version version, string filename, Stream content)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new ($"{Base}/api/v1/Manage/Application/{appId}/AutoUpdaterDotNet/SubmitRevision?version={WebUtility.UrlEncode(version.ToString())}&filename={WebUtility.UrlEncode(filename)}"),
            Content = new StreamContent(content)
        };
        InjectHeaders(msg);
        return msg;
    }
}
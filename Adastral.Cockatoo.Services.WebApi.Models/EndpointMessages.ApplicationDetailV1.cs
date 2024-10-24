namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    public HttpRequestMessage ApplicationDetailV1GetAvailable()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/ApplicationDetail/Available")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ApplicationDetailV1Get(string applicationId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/ApplicationDetail/Id/{applicationId}")
        };
        InjectHeaders(msg);
        return msg;
    }
}
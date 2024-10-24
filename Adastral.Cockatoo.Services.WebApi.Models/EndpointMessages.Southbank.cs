namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{

    public HttpRequestMessage SouthbankV1Get()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Southbank")
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage SouthbankV2Get()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v2/Southbank")
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage SouthbankV3Get()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v3/Southbank")
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage SouthbankV1RefreshCache()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Southbank/RefreshCache")
        };
        InjectHeaders(msg);
        return msg;
    }
}
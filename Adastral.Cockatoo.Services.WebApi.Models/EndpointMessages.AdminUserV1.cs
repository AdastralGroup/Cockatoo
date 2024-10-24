using System.Net.Http.Headers;
using System.Net.Http.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    
    public HttpRequestMessage AdminUserV1List()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Admin/User/List")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminUserV1DeleteToken(string tokenId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"{Base}/api/v1/Admin/User/Token/{tokenId}")
        };
        InjectHeaders(msg);
        return msg;
    }
    
    public HttpRequestMessage AdminUserV1CreateToken(UserV1CReateTokenRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/User/Token"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }
}
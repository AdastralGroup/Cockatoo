using System.Net.Http.Headers;
using System.Net.Http.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    public HttpRequestMessage UserV1GetSelf()
    {
        return UserV1Get("@me");
    }
    public HttpRequestMessage UserV1Get(string userId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/User/{userId}"),
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage UserV1CreateToken(UserV1CReateTokenRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/User/CreateToken"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }
}
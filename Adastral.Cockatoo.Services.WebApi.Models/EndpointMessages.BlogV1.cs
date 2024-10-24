using System.Net.Http.Headers;
using System.Net.Http.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    
    public HttpRequestMessage BlogPostV1Create()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Blog/Post/Create")
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage BlogPostV1Get(string blogPostId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new ($"{Base}/api/v1/Blog/Post/{blogPostId}")
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage BlogPostV1Delete(string blogPostId, bool deleteResources)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ($"{Base}/api/v1/Blog/Post/{blogPostId}?deleteResources=" + (deleteResources ? "true" : "false"))
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage BlogPostV1Update(string blogPostId, BlogPostV1UpdateRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Patch,
            RequestUri = new Uri($"{Base}/api/v1/Blog/Post/{blogPostId}"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage BlogPostV1GetTags(string blogPostId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Blog/Post/{blogPostId}/Tags")
        };
        InjectHeaders(msg);
        return msg;
    }
}
namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    public HttpRequestMessage BullseyeV1Get(string appId, bool? liveState = null)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Bullseye/{appId}" + (liveState == null ? "" : (bool)liveState ? "?liveState=true" : "?liveState=false"))
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage BullseyeV2Get(string appId, bool? liveState = null)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v2/Bullseye/{appId}" + (liveState == null ? "" : (bool)liveState ? "?liveState=true" : "?liveState=false"))
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage BullseyeV1GetBlogPostsForRevision(string appId, string revisionId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Bullseye/{appId}/Revision/{revisionId}/BlogPosts")
        };
        InjectHeaders(msg);
        return msg;
    }
}
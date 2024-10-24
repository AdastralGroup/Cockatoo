using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Response;

namespace Adastral.Cockatoo.Client;

public class BullseyeRestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal BullseyeRestClient(CockatooRestClient client)
    {
        _client = client;
    }
    
    public async Task<BullseyeV1> GetV1(string appId, bool? liveState = null)
    {
        var response = await _httpClient.SendAsync(_endpoint.BullseyeV1Get(appId, liveState));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<BullseyeV1>(response, responseText, true)!;
    }
    public async Task<BullseyeV2> GetV2(string appId, bool? liveState = null)
    {
        var response = await _httpClient.SendAsync(_endpoint.BullseyeV2Get(appId, liveState));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<BullseyeV2>(response, responseText, true)!;
    }
    public async Task<List<BlogPostV1Response>> GetRevisionBlogPosts(string appId, string revisionId)
    {
        var response = await _httpClient.SendAsync(_endpoint.BullseyeV1GetBlogPostsForRevision(appId, revisionId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<List<BlogPostV1Response>>(response, responseText, true)!;
    }
}
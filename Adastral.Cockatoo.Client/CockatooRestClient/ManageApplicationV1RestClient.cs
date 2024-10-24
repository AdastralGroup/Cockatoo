
using Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;
using Adastral.Cockatoo.Services.WebApi.Models;

namespace Adastral.Cockatoo.Client;

public class ManageApplicationV1RestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal ManageApplicationV1RestClient(CockatooRestClient client)
    {
        _client = client;
    }

    public async Task<AUDNRevisionModel> SubmitAUDNRevision(string appId, Version version, string filename, Stream content)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageApplicationV1SubmitAUDNRevision(appId, version, filename, content));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AUDNRevisionModel>(response, responseText, true)!;
    }
}
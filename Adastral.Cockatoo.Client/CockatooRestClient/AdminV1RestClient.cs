using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.Services.WebApi.Models;

namespace Adastral.Cockatoo.Client;

public class AdminV1RestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal AdminV1RestClient(CockatooRestClient client)
    {
        _client = client;

        Group = new(client);
        User = new(client);
    }

    public readonly AdminGroupV1RestClient Group;
    public readonly AdminUserV1RestClient User;
    public async Task DeleteToken(string tokenId)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminUserV1DeleteToken(tokenId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        if (response.IsSuccessStatusCode == false)
        {
            throw new APIClientRequestFailureException(response, responseText);
        }
    }
}
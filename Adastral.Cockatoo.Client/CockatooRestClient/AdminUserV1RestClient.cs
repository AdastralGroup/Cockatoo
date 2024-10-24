using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Client;

public class AdminUserV1RestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal AdminUserV1RestClient(CockatooRestClient client)
    {
        _client = client;
    }
    
    public async Task<List<UserModel>> List()
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminUserV1List());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<List<UserModel>>(response, responseText, true)!;
    }

    public async Task DeleteToken(string tokenId)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminUserV1DeleteToken(tokenId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            CockatooRestClient.ParseError(response, responseText);
            throw new APIClientRequestFailureException(response, responseText);
        }
    }

    public async Task<ServiceAccountTokenModel> CreateToken(UserV1CReateTokenRequest data)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminUserV1CreateToken(data));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<ServiceAccountTokenModel>(response, responseText, true)!;
    }
}
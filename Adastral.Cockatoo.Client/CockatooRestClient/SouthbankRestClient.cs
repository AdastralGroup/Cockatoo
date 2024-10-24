using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models;

namespace Adastral.Cockatoo.Client;

public class SouthbankRestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal SouthbankRestClient(CockatooRestClient client)
    {
        _client = client;
    }
    
    public async Task<SouthbankV1> GetV1()
    {
        var response = await _httpClient.SendAsync(_endpoint.SouthbankV1Get());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<SouthbankV1>(response, responseText, true)!;
    }

    public async Task<SouthbankV2> GetV2()
    {
        var response = await _httpClient.SendAsync(_endpoint.SouthbankV2Get());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<SouthbankV2>(response, responseText, true)!;
    }

    public async Task<SouthbankV3> GetV3()
    {
        var response = await _httpClient.SendAsync(_endpoint.SouthbankV3Get());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<SouthbankV3>(response, responseText, true)!;
    }

    public async Task RefreshCache()
    {
        var response = await _httpClient.SendAsync(_endpoint.SouthbankV1RefreshCache());
        var responseText = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            throw new APIClientRequestFailureException(response, responseText);
        }
    }
}
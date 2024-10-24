using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models;

namespace Adastral.Cockatoo.Client;

public class StorageFileV1RestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal StorageFileV1RestClient(CockatooRestClient client)
    {
        _client = client;
    }
    

    public async Task<StorageFileModel> Upload(string filename, Stream stream)
    {
        var response = await _httpClient.SendAsync(_endpoint.FileV1Upload(filename, stream));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<StorageFileModel>(response, responseText, true)!;
    }
    public async Task<StorageFileModel> GetDetails(string storageFileId)
    {
        var response = await _httpClient.SendAsync(_endpoint.FileV1GetModel(storageFileId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<StorageFileModel>(response, responseText, true)!;
    }
    public async Task Download(string storageFileId, string outputLocation)
    {
        var response = await _httpClient.SendAsync(_endpoint.FileV1GetContent(storageFileId));
        if (response.IsSuccessStatusCode)
        {
            using (var fs = new FileStream(outputLocation, FileMode.OpenOrCreate))
            {
                await response.Content.CopyToAsync(fs);
            }
        }
        else
        {
            var responseText = response.Content.ReadAsStringAsync().Result;
            throw new APIClientRequestFailureException(response, responseText);
        }
    }
}
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;

namespace Adastral.Cockatoo.Client;

public class ManageBullseyeV1RestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal ManageBullseyeV1RestClient(CockatooRestClient client)
    {
        _client = client;
    }


    public async Task<List<ApplicationDetailModel>> ListApps()
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1ListApps());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<List<ApplicationDetailModel>>(response, responseText, true)!;
    }
    public async Task<List<BullseyeAppRevisionModel>> GetRevisionsForApp(string appId)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1ListRevisionsForApp(appId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<List<BullseyeAppRevisionModel>>(response, responseText, true)!;
    }

    public async Task<ManageBullseyeV1GenerateCacheResponse> GenerateCache(ManageBullseyeV1GenerateCacheRequest request)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1GenerateCache(request));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<ManageBullseyeV1GenerateCacheResponse>(response, responseText, true)!;
    }

    public async Task<List<ManageBullseyeV1GenerateCacheResponse>> GenerateCacheAuto(string appId)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1GenerateCacheAuto(appId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<List<ManageBullseyeV1GenerateCacheResponse>>(response, responseText, true)!;
    }

    public async Task<BullseyePatchModel> RegisterPatch(ManageBullseyeV1RegisterPatchRequest request)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1RegisterPatch(request));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<BullseyePatchModel>(response, responseText, true)!;
    }

    public async Task<BullseyeAppRevisionModel> RegisterRevision(string appId,
        ManageBullseyeV1RegisterRevisionRequest request)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1RegisterRevision(appId ,request));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<BullseyeAppRevisionModel>(response, responseText, true)!;
    }

    public async Task<ComparisonResponse<BullseyeAppRevisionModel>> UpdateRevision(ManageBullseyeV1UpdateRevisionRequest request)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1UpdateRevision(request));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<ComparisonResponse<BullseyeAppRevisionModel>>(response, responseText, true)!;
    }

    public async Task<ComparisonResponse<BullseyeAppModel>> MarkRevisionAsLatest(string appId, string revisionId)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1MarkLatestRevision(appId, revisionId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<ComparisonResponse<BullseyeAppModel>>(response, responseText, true)!;
    }

    public async Task<ComparisonResponse<BullseyeAppModel>> UnmarkRevisionAsLatest(string appId)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1UnmarkLatestRevision(appId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<ComparisonResponse<BullseyeAppModel>>(response, responseText, true)!;
    }

    public async Task<ManageBullseyeV1DeleteResponse> DeleteApp(ManageBullseyeV1DeleteRequest request)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1DeleteApp(request));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<ManageBullseyeV1DeleteResponse>(response, responseText, true)!;
    }

    public async Task<BullseyeAppRevisionModel> GetRevision(string revisionId)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1GetRevision(revisionId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<BullseyeAppRevisionModel>(response, responseText, true)!;
    }

    public async Task<List<BullseyePatchModel>> GetRevisionPatches(string revisionId, BullseyePatchFilterKind direction = BullseyePatchFilterKind.EqualsId)
    {
        var request = _endpoint.ManageBullseyeV1GetRevisionPatches(revisionId);
        switch (direction)
        {
            case BullseyePatchFilterKind.EqualsFrom:
                request = _endpoint.ManageBullseyeV1GetRevisionPatchesFrom(revisionId);
                break;
            case BullseyePatchFilterKind.EqualsTo:
                request = _endpoint.ManageBullseyeV1GetRevisionPatchesTo(revisionId);
                break;
        }
        var response = await _httpClient.SendAsync(request);
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<List<BullseyePatchModel>>(response, responseText, true)!;
    }

    public async Task<ManageBullseyeV1DeletePatchResponse> DeletePatch(string patchId)
    {
        var response = await _httpClient.SendAsync(_endpoint.ManageBullseyeV1DeletePatch(patchId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<ManageBullseyeV1DeletePatchResponse>(response, responseText, true)!;
    }
}
public enum BullseyePatchFilterKind
{
    EqualsId,
    EqualsFrom,
    EqualsTo
}
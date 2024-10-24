using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;

namespace Adastral.Cockatoo.Client;

public class AdminGroupV1RestClient
{
    internal readonly CockatooRestClient _client;
    private HttpClient _httpClient => _client._httpClient;
    private EndpointMessages _endpoint => _client._endpoint;

    internal AdminGroupV1RestClient(CockatooRestClient client)
    {
        _client = client;
    }
    
    public async Task<AdminGroupV1DetailResponse> Create(AdminGroupV1CreateRequest data)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1Create(data));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1DetailResponse>(response, responseText, true)!;
    }
    public async Task<AdminGroupV1ListResponse> List()
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1List());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1ListResponse>(response, responseText, true)!;
    }
    public async Task<AdminGroupV1DetailResponse> Get(string groupId)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1Details(groupId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1DetailResponse>(response, responseText, true)!;
    }
    public async Task Delete(string groupId)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1Delete(groupId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            throw new APIClientRequestFailureException(response, responseText);
        }
    }

    public async Task AddUser(string groupId, string userId)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1AddUser(groupId, userId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            throw new APIClientRequestFailureException(response, responseText);
        }
    }

    public async Task<List<string>> RemoveUser(string groupId, string userId)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1RemoveUser(groupId, userId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<List<string>>(response, responseText, true)!;
    }
    
    #region Permission - Global
    public async Task<AdminGroupV1GrantGlobalPermissionResponse> GrantGlobalPermission(string groupId, PermissionKind kind)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1GrantGlobalPermission(groupId, kind));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1GrantGlobalPermissionResponse>(response, responseText, true)!;
    }
    public async Task<AdminGroupV1DenyGlobalPermissionResponse> DenyGlobalPermission(string groupId, PermissionKind kind)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1DenyGlobalPermission(groupId, kind));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1DenyGlobalPermissionResponse>(response, responseText, true)!;
    }
    public async Task<AdminGroupV1DetailResponse> RevokeGlobalPermission(string groupId, PermissionKind kind)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1RevokeGlobalPermission(groupId, kind));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1DetailResponse>(response, responseText, true)!;
    }
    #endregion

    #region Permission - Application
    public async Task<AdminGroupV1GrantApplicationPermissionResponse> GrantApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1GrantApplicationPermission(groupId, applicationId, kind));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1GrantApplicationPermissionResponse>(response, responseText, true)!;
    }

    public async Task<AdminGroupV1DenyApplicationPermissionResponse> DenyApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1DenyApplicationPermission(groupId, applicationId, kind));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1DenyApplicationPermissionResponse>(response, responseText, true)!;
    }

    public async Task<AdminGroupV1DetailResponse> RevokeApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        var response = await _httpClient.SendAsync(_endpoint.AdminGroupV1RevokeApplicationPermission(groupId, applicationId, kind));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return _client.CastOnSuccess<AdminGroupV1DetailResponse>(response, responseText, true)!;
    }
    #endregion
}
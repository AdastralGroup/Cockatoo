using System.Net.Http.Headers;
using System.Net.Http.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{

    public HttpRequestMessage AdminGroupV1Create(AdminGroupV1CreateRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/Create"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1List()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/List")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1Details(string groupId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/{groupId}")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1Delete(string groupId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/{groupId}")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1AddUser(string groupId, string userId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/{groupId}/User/{userId}")
        };
        InjectHeaders(msg);
        return msg;
    }
    
    public HttpRequestMessage AdminGroupV1RemoveUser(string groupId, string userId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/{groupId}/User/{userId}")
        };
        InjectHeaders(msg);
        return msg;
    }
    
    public HttpRequestMessage AdminGroupV1GrantGlobalPermission(string groupId, PermissionKind kind)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/PermissionGrant/Global"),
            Content = JsonContent.Create(new AdminGroupV1GrantGlobalPermissionRequest()
            {
                GroupId = groupId,
                Kind = kind
            }, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1DenyGlobalPermission(string groupId, PermissionKind kind)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/PermissionDeny/Global"),
            Content = JsonContent.Create(new AdminGroupV1DenyGlobalPermissionRequest()
            {
                GroupId = groupId,
                Kind = kind
            }, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1RevokeGlobalPermission(string groupId, PermissionKind kind)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/PermissionRevoke/Global"),
            Content = JsonContent.Create(new AdminGroupV1RevokeGlobalPermissionRequest()
            {
                GroupId = groupId,
                Kind = kind
            }, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1GrantApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/PermissionGrant/Application"),
            Content = JsonContent.Create(new AdminGroupV1GrantApplicationPermissionRequest()
            {
                GroupId = groupId,
                ApplicationId = applicationId,
                Kind = kind,
            }, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1DenyApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/PermissionDeny/Application"),
            Content = JsonContent.Create(new AdminGroupV1DenyApplicationPermissionRequest()
            {
                GroupId = groupId,
                ApplicationId = applicationId,
                Kind = kind,
            }, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage AdminGroupV1RevokeApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Admin/Group/PermissionRevoke/Application"),
            Content = JsonContent.Create(new AdminGroupV1RevokeApplicationPermissionRequest()
            {
                GroupId = groupId,
                ApplicationId = applicationId,
                Kind = kind,
            }, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }
}
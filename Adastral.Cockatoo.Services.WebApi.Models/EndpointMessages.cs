using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    private string _base = "";
    public string Base
    {
        get => _base;
        set
        {
            var x = value;
            while (x.EndsWith('/'))
            {
                x = x.Remove(x.Length - 1);
            }
            _base = x;
        }
    }
    public string Token { get; set; } = "";
    public string TokenHeaderName { get; set; } = "x-cockatoo-token";

    private void InjectHeaders(HttpRequestMessage msg)
    {
        msg.Headers.Add(TokenHeaderName, Token);
    }
    
    public HttpRequestMessage PermissionAdminApiV1DeleteRole(string roleId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ($"{Base}/api/v1/PermissionAdmin/Role/{roleId}"),
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage PermissionAdminApiV1DeleteGroupUserAssociation(string associationId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ($"{Base}/api/v1/PermissionAdmin/GroupUserAssocation/{associationId}"),
        };
        InjectHeaders(msg);
        return msg;
    }
}
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    public HttpRequestMessage ManageBullseyeV1ListApps()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/Apps")
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage ManageBullseyeV1ListRevisionsForApp(string appId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/App/{appId}/Revisions")
        };
        InjectHeaders(msg);
        return msg;
    }
    public HttpRequestMessage ManageBullseyeV1GenerateCache(ManageBullseyeV1GenerateCacheRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/App/{data.AppId}/Cache"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1GenerateCacheAuto(string appId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/App/{appId}/Cache/Auto"),
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1RegisterPatch(ManageBullseyeV1RegisterPatchRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/RegisterPatch"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1RegisterRevision(string appId, ManageBullseyeV1RegisterRevisionRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/RegisterRevision?appId={WebUtility.UrlEncode(appId)}"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1UpdateRevision(ManageBullseyeV1UpdateRevisionRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Patch,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/Revision"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1MarkLatestRevision(string appId, string revisionId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Patch,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/App/{appId}/LatestRevision/{revisionId}")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1UnmarkLatestRevision(string appId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"{Base}/api/v1/Manage/Bullseye/App/{appId}/LatestRevision")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1DeleteApp(ManageBullseyeV1DeleteRequest data)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ($"{Base}/api/v1/Manage/Bullseye/App"),
            Content = JsonContent.Create(data, MediaTypeHeaderValue.Parse("application/json"), BaseService.SerializerOptions)
        };
        InjectHeaders(msg);
        return msg;
    }
    
    public HttpRequestMessage ManageBullseyeV1DeletePatch(string patchId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Delete,
            RequestUri = new ($"{Base}/api/v1/Manage/Bullseye/Patch/{patchId}"),
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1GetRevision(string revisionId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new ($"{Base}/api/v1/Manage/Bullseye/Revision/{revisionId}"),
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1GetRevisionPatches(string revisionId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new ($"{Base}/api/v1/Manage/Bullseye/Revision/{revisionId}/Patches"),
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1GetRevisionPatchesTo(string revisionId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new ($"{Base}/api/v1/Manage/Bullseye/Revision/{revisionId}/Patches/WhereCanPatchTo"),
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage ManageBullseyeV1GetRevisionPatchesFrom(string revisionId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new ($"{Base}/api/v1/Manage/Bullseye/Revision/{revisionId}/Patches/WhereCanPatchFrom"),
        };
        InjectHeaders(msg);
        return msg;
    }
}
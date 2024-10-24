using System.Net;

namespace Adastral.Cockatoo.Services.WebApi.Models;

public partial class EndpointMessages
{
    public HttpRequestMessage FileV1Upload(string filename, Stream file)
    {
        var msg = new HttpRequestMessage()
        {
            RequestUri = new Uri($"{Base}/api/v1/File/Upload?filename={WebUtility.UrlEncode(filename)}"),
            Method = HttpMethod.Post,
            Content = new StreamContent(file),
        };
        Console.WriteLine($"[FileV1Upload] Content Length {msg.Content.Headers.ContentLength}");
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage FileV1GetModel(string storageFileId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new($"{Base}/api/v1/File/{storageFileId}/Details")
        };
        InjectHeaders(msg);
        return msg;
    }

    public HttpRequestMessage FileV1GetContent(string storageFileId)
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new($"{Base}/api/v1/File/{storageFileId}/Content")
        };
        InjectHeaders(msg);
        return msg;
    }
}
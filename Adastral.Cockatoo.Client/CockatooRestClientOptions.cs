using System.ComponentModel;

namespace Adastral.Cockatoo.Client;

public class CockatooRestClientOptions
{
    public Uri BaseUri { get; set; }
    public string Token { get; set; }
    public const string TokenHeaderNameDefault = "x-cockatoo-token";

    [DefaultValue(TokenHeaderNameDefault)]
    public string TokenHeaderName { get; set; } = TokenHeaderNameDefault;

    public CockatooRestClientOptions Clone()
    {
        return new(new Uri(BaseUri.ToString()), Token)
        {
            TokenHeaderName = TokenHeaderName
        };
    }
    public CockatooRestClientOptions(string baseUri, string token)
        : this(new Uri(baseUri), token)
    { }

    public CockatooRestClientOptions(Uri baseUri, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException($"Value is required", nameof(token));
        }
        BaseUri = baseUri;
        Token = token;
    }
}
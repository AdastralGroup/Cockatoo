namespace Adastral.Cockatoo.DataAccess;

public class APIClientRequestFailureException : Exception
{
    public HttpResponseMessage Response { get; private set; }
    public string Content { get; private set; }
    public APIClientRequestFailureException(HttpResponseMessage response, string content)
        : base($"Status Code: {response.StatusCode}\nContent;\n{content}")
    {
        Response = response;
        Content = content;
    }
}
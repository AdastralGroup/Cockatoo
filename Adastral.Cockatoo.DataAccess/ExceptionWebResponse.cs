namespace Adastral.Cockatoo.DataAccess;

public class ExceptionWebResponse
{
    /// <summary>
    /// Exception Message
    /// </summary>
    public string Message { get; set; }
    /// <summary>
    /// Stack trace. Only <see langword="null"/> when not provided.
    /// </summary>
    public string? Stack { get; set; }
    /// <summary>
    /// Full content when an Exception is provided to the constructor.
    /// </summary>
    public string? Content { get; set; }
    /// <summary>
    /// What type of Exception is this?
    /// </summary>
    public string Type { get; set; }

    public ExceptionWebResponse()
        : this("An unknown error occoured.", null, typeof(Exception))
    {}
    public ExceptionWebResponse(string message)
        : this(message, null, typeof(Exception))
    {}
    public ExceptionWebResponse(string message, string? stack, Type type)
    {
        Message = message;
        Stack = stack;
        Content = null;
        Type = type.ToString();
    }
    public ExceptionWebResponse(Exception exception)
        : this(exception.Message, exception.StackTrace, exception.GetType())
    {
        Content = exception.ToString();
    }
}
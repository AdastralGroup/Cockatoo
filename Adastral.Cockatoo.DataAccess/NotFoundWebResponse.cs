namespace Adastral.Cockatoo.DataAccess;

public class NotFoundWebResponse
{
    public string ClassType { get; set; }
    public string PropertyName { get; set; }
    public object? ExpectedValue { get; set; }
    public string Message { get; set; }

    public NotFoundWebResponse()
        : this(typeof(object),
            "",
            null,
            "Not Found")
    {
    }

    public NotFoundWebResponse(
        Type type,
        string propertyName,
        object? expectedValue,
        string message)
    {
        ClassType = type.ToString();
        PropertyName = propertyName;
        ExpectedValue = expectedValue;
        Message = message;
    }
}
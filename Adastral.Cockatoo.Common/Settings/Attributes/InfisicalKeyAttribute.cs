namespace Adastral.Cockatoo.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public class InfisicalKeyAttribute : Attribute
{
    public string Key { get; private set; }
    public InfisicalKeyAttribute(string key)
        : base()
    {
        Key = key;
    }
}
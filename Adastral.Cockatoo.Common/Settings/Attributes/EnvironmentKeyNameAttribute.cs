namespace Adastral.Cockatoo.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public class EnvironmentKeyNameAttribute : Attribute
{
    public string Key { get; private set; }
    public EnvironmentKeyNameAttribute(string key)
        : base()
    {
        Key = key;
    }
}
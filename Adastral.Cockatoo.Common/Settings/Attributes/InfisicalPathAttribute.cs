namespace Adastral.Cockatoo.Common;

[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public class InfisicalPathAttribute : Attribute
{
    public string Path { get; private set; }
    public InfisicalPathAttribute(string path)
        : base()
    {
        Path = path;
    }
}
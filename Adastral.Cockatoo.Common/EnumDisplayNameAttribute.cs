using System.ComponentModel;
using System.Reflection;

namespace Adastral.Cockatoo.Common;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class EnumDisplayNameAttribute : Attribute
{
    public string Value {get;private set;}
    public EnumDisplayNameAttribute(string value)
    {
        Value = value;
    }
}
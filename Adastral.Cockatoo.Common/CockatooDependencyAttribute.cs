using System.ComponentModel;

namespace Adastral.Cockatoo.Common;

[AttributeUsage(AttributeTargets.Class)]
public class CockatooDependencyAttribute : Attribute
{
    [DefaultValue(uint.MaxValue)]
    public uint Priority { get; set; }

    public CockatooDependencyAttribute()
        : base()
    {
        Priority = uint.MaxValue;
    }
}
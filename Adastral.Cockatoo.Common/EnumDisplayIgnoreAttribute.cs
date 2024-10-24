namespace Adastral.Cockatoo.Common;

/// <summary>
/// Ignore the enum field when displaying options to use in frontend.
/// </summary>
[AttributeUsage(AttributeTargets.Enum| AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class EnumDisplayIgnoreAttribute : Attribute
{
}
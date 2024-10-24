namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// When applied to an Enum member, it will signifiy that this cannot be converted into a scoped permission.
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class GlobalPermissionAttribute : Attribute
{
}
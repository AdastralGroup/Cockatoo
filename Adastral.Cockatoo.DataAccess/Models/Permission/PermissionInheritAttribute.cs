namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// When calculating permissions, it will append this permission when the kind it's on is used.
/// </summary>
/// <example>
/// <code>
/// public enum PermissionKind {
///     [PermissionInherit(UserAdminViewAll)]
///     [PermissionInherit(UserAdminDisable)]
///     UserAdmin,
///     [PermissionInherit(ServiceAccountViewAll)]
///     UserAdminViewAll,
///     UserAdminDisable,
///
///     ServiceAccountViewAll
/// }
/// </code>
///
/// So when <c>PermissionKind.UserAdmin</c> is assigned to a group or user, it will calculate that it will also
/// allow (or deny) <c>PermissionKind.UserAdminViewAll</c> and <c>PermissionKind.UserAdminDisable</c>, unless specified
/// otherwise by a more important group.
/// </example>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public class PermissionInheritAttribute : Attribute
{
    public PermissionKind InheritFrom { get; set; }

    public PermissionInheritAttribute(PermissionKind inheritFrom)
    {
        InheritFrom = inheritFrom;
    }
}
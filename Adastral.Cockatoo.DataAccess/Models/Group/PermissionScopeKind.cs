using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.DataAccess.Models;

public enum PermissionScopeKind
{
    [EnumDisplayName("Per-Application")]
    Application,
    [EnumDisplayName("Global (override)")]
    Global
}
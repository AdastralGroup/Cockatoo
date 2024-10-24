using System.ComponentModel;
using System.Reflection;

namespace Adastral.Cockatoo.Common;

public static class EnumExtensions
{
    /// <summary>
    /// Get the display name for the enum kind provided.
    /// </summary>
    /// <returns>
    /// Return <see cref="EnumDisplayNameAttribute.Value"/> when found on the <paramref name="kind"/> provided, or the
    /// <paramref name="kind"/> as a string when not found.
    /// </returns>
    public static string GetDisplayName<T>(this T kind) where T : struct, System.Enum
    {
        var member = typeof(T).GetMember(kind.ToString()).FirstOrDefault();
        if (member == null)
            return kind.ToString();
        var attr = member.GetCustomAttribute<EnumDisplayNameAttribute>();
        return attr?.Value ?? kind.ToString();
    }
    
    /// <summary>
    /// Get the value of the description on an enum value.
    /// </summary>
    /// <returns>
    /// <see cref="DescriptionAttribute.Description"/>, or an empty string when that attribute couldn't be found on
    /// the <paramref name="kind"/> provided.
    /// </returns>
    public static string GetDescription<T>(this T kind) where T : struct, System.Enum
    {
        var member = typeof(T).GetMember(kind.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? "";
    }
    
    /// <summary>
    /// Check if an enum should be ignored when displaying it to the user.
    /// </summary>
    /// <returns>
    /// Will return <see langword="true"/> when <see cref="EnumDisplayIgnoreAttribute"/> is found on the
    /// <paramref name="kind"/> provided.
    /// </returns>
    public static bool IgnoreForDisplay<T>(this T kind)
        where T : struct, Enum
    {
        var member = typeof(T).GetMember(kind.ToString())?.FirstOrDefault();
        if (member == null)
            return false;
        var attr = member.GetCustomAttribute<EnumDisplayIgnoreAttribute>();
        return attr != null;
    }
}
using Microsoft.AspNetCore.Http;

namespace Adastral.Cockatoo.Services.WebApi.Helpers;

public static class SessionExtensions
{
    public static bool TryGetString(this ISession session, string key, out string? value)
    {
        value = session.GetString(key);
        return string.IsNullOrEmpty(value) == false;
    }
}
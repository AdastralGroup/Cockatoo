using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Adastral.Cockatoo.Services.WebApi.Helpers;

public static class AspAuthHelper
{
    public static string? GetUserRemoteName(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            return GetUserRemoteName(context.User);
        }

        return null;
    }
    public static string? GetUserRemoteName(ClaimsPrincipal user)
    {
        return GetClaimValue(user.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
    }
    public static string? GetUserRemoteName(ClaimsIdentity identity)
    {
        return GetClaimValue(identity.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
    }

    public static string? GetUserRemoteId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            return GetUserRemoteId(context.User);
        }

        return null;
    }
    public static string? GetUserRemoteId(ClaimsPrincipal user)
    {
        return GetClaimValue(user.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
    }
    public static string? GetUserRemoteId(ClaimsIdentity identity)
    {
        return GetClaimValue(identity.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
    }
    public static string? GetUserRemoteEmail(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            return GetUserRemoteEmail(context.User);
        }

        return null;
    }

    public static string? GetUserRemoteEmail(ClaimsPrincipal user)
    {
        return GetClaimValue(user.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
    }
    public static string? GetUserRemoteEmail(ClaimsIdentity identity)
    {
        return GetClaimValue(identity.Claims, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
    }
    
    public static string? GetClaimValue<TEnumerable>(TEnumerable claims, string type)
        where TEnumerable : IEnumerable<Claim>
    {
        foreach (var claim in claims)
        {
            if (claim.Type == type)
            {
                return claim.Value;
            }
        }
        return null;
    }
}
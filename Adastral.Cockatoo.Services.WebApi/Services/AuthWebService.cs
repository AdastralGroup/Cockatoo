using System.Security.Claims;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Helpers;
using kate.shared.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class AuthWebService : BaseService
{
    private readonly CoreContext _core;
    private readonly UserRepository _userRepo;
    private readonly UserPreferencesRepository _userPreferencesRepo;
    private readonly ServiceAccountTokenRepository _serviceAccountTokenRepo;
    private readonly CockatooConfig _config;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public AuthWebService(IServiceProvider services)
        : base(services)
    {
        _core = services.GetRequiredService<CoreContext>();
        _config = services.GetRequiredService<CockatooConfig>();
        _userRepo = services.GetRequiredService<UserRepository>();
        _userPreferencesRepo = services.GetRequiredService<UserPreferencesRepository>();
        _serviceAccountTokenRepo = services.GetRequiredService<ServiceAccountTokenRepository>();
    }

    public async Task<bool> IsAuthenticated(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            return await GetCurrentUser(context) != null;
        }
        else if (await GetTokenModel(context.Request) != null)
        {
            return true;
        }
        else if (context.Request.Headers.TryGetValue("Authorization", out var x))
        {
            foreach (var p in x)
            {
                if (p?.StartsWith("Basic ") ?? false)
                {
                    var s = p.Substring(p.IndexOf(" ") + 1);
                    string dec;
                    try
                    {
                        dec = GeneralHelper.Base64Decode(s);
                    }
                    catch
                    {
                        continue;
                    }
                    var username = dec.Substring(0, dec.IndexOf(":") + 1);
                    var password = dec.Substring(dec.IndexOf(":") + 1, dec.Length + dec.IndexOf(":") + 1);
                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                        continue;
                    var check = await HandleSignInDirect(context, username, password);
                    if (check)
                        return true;
                }
            }
        }
        return false;
    }

    public async Task<ServiceAccountTokenModel?> GetTokenModel(HttpRequest request)
    {
        if (request.Headers.TryGetValue(_config.AspNET.TokenHeader, out var token))
        {
            var tokenModel = await _serviceAccountTokenRepo.GetByToken(token!, true);
            if (tokenModel == null)
            {
                return null;
            }
            return tokenModel;
        }
        return null;
    }
    public bool TryGetTokenModel(HttpRequest request, out ServiceAccountTokenModel? model)
    {
        model = GetTokenModel(request).Result;
        return model != null;
    }

    public async Task<UserPreferencesModel?> GetCurrentUserPreferences(HttpContext? context)
    {
        if (context == null)
            return null;
        var user = await GetCurrentUser(context);
        if (user != null)
        {
            var preferences = await _userPreferencesRepo.GetById(user.Id);
            if (preferences == null)
            {
                preferences = new()
                {
                    UserId = user.Id
                };
                await _userPreferencesRepo.InsertOrUpdate(preferences);
            }

            return preferences;
        }

        return null;
    }
    public async Task<UserModel?> GetCurrentUser(HttpContext context)
    {
        if (TryGetCurrentUserViaToken(context, out var tokenUser))
        {
            return tokenUser;
        }
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            return await GetOrCreateUser((ClaimsIdentity)context.User.Identity!);
        }
        return null;
    }

    public async Task<UserModel?> GetOrCreateUser(ClaimsIdentity identity)
    {
        if (identity.IsAuthenticated == false)
        {
            return null;
        }

        var remoteUserId = AspAuthHelper.GetUserRemoteId(identity);
        var remoteUserEmail = AspAuthHelper.GetUserRemoteEmail(identity);
        if (remoteUserId == null)
        {
            throw new HttpRequestException("Could not get Claim http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        }

        var userModel = await _userRepo.GetByOAuthId(remoteUserId);
        userModel ??= await _userRepo.GetByEmail(remoteUserEmail);
        if (userModel == null)
        {
            userModel ??= new();
            userModel.Email = remoteUserEmail;
            userModel.OAuthUserId = remoteUserId;
            userModel.DisplayName = AspAuthHelper.GetUserRemoteName(identity) ?? remoteUserEmail ?? userModel.Id;
            await _userRepo.InsertOrUpdate(userModel);
        }
        return userModel;
    }

    public async Task<UserModel?> GetCurrentUserViaToken(HttpContext context)
    {
        var token = await GetTokenModel(context.Request);
        if (token != null)
        {
            var userModel = await _userRepo.GetById(token.ServiceAccountId);
            if (userModel != null && (userModel?.IsServiceAccount ?? false))
            {
                return userModel;
            }
        }
        var basicAuth = await GetUserFromBasicAuth(context);
        if (basicAuth != null)
        {
            return basicAuth;
        }
        return null;
    }

    public async Task<UserModel?> GetUserFromBasicAuth(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var x))
        {
            foreach (var p in x)
            {
                if (p?.StartsWith("Basic ") ?? false)
                {
                    var s = p.Substring(p.IndexOf(" ") + 1);
                    string dec;
                    try
                    {
                        dec = GeneralHelper.Base64Decode(s);
                    }
                    catch
                    {
                        continue;
                    }
                    var username = dec.Substring(0, dec.IndexOf(":") + 1);
                    var password = dec.Substring(dec.IndexOf(":") + 1, dec.Length + dec.IndexOf(":") + 1);
                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                        continue;
                    var check = await HandleSignInDirect(context, username, password);
                    if (context.User.Identity?.IsAuthenticated ?? false)
                    {
                        return await GetOrCreateUser((ClaimsIdentity)context.User.Identity!);
                    }
                }
            }
        }
        return null;
    }

    public bool TryGetCurrentUserViaToken(HttpContext context, out UserModel? user)
    {
        user = GetCurrentUserViaToken(context).Result;
        return user != null;
    }

    public async Task<bool> HandleSignInDirect(HttpContext httpContext, string username, string password)
    {
        var providers = _core.GetRequiredServicesImplementing<IDirectAuthenticationProvider>();
        foreach (var item in providers)
        {
            if (!item.Enabled())
                continue;
            bool r = false;
            ClaimsPrincipal? principal = null;
            try
            {
                r = item.TryValidateCredentials(username, password, out principal);
            }
            catch (Exception ex)
            {
                _log.Error($"name={item.GetName()}|Failed to validate credentials for user {username}\n{ex}");
                continue;
            }
            if (r)
            {
                try
                {
                    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal!);
                }
                catch (Exception ex)
                {
                    _log.Error($"name={item.GetName()}|Failed to run HttpContext.SignInAsync(\"{CookieAuthenticationDefaults.AuthenticationScheme}\")\n{ex}");
                    continue;
                }
                try
                {

                var claimIdent = principal!.Identities.FirstOrDefault()!;
                if (principal!.Identity?.IsAuthenticated ?? false && claimIdent != null)
                {
                    await GetOrCreateUser(claimIdent);
                }
                }
                catch (Exception ex)
                {
                    _log.Error($"name={item.GetName()}|Failed to run {nameof(GetOrCreateUser)}\n{ex}");
                    continue;
                }
                return true;
            }
        }
        return false;
    }
}
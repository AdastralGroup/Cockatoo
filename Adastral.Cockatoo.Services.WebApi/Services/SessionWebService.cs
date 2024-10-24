using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using NLog;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class SessionWebService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly AuthWebService _authWebService;
    private readonly UserSessionRepository _userSessionRepo;
    private readonly UserSessionRequestRepository _userSessionRequestRepo;
    private readonly ServiceAccountTokenRepository _serviceAccountTokenRepo;
    public SessionWebService(IServiceProvider services)
        : base(services)
    {
        _authWebService = services.GetRequiredService<AuthWebService>();
        _userSessionRepo = services.GetRequiredService<UserSessionRepository>();
        _userSessionRequestRepo = services.GetRequiredService<UserSessionRequestRepository>();
        _serviceAccountTokenRepo = services.GetRequiredService<ServiceAccountTokenRepository>();
    }

    public async Task<UserSessionModel?> GetCurrentSession(HttpContext context)
    {
        if (context.Session.TryGetString("_Id", out var sessionId))
        {
            var model = await _userSessionRepo.GetById(sessionId!);
            if (model != null)
            {
                var current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (model.ExpiresAt == null)
                {
                    if ((context.User.Identity?.IsAuthenticated ?? false) == false)
                    {
                        model.ExpiresAt = new BsonTimestamp(current);
                        model.IsDeleted = true;
                        model = await _userSessionRepo.InsertOrUpdate(model);
                        _log.Info($"{model.Id}| Marked as deleted since user is not authenticated");
                    }
                }

                if (model.ExpiresAt <= current)
                {
                    return null;
                }
                return model;
            }
        }
        return null;
    }

    public enum CreateSessionResult
    {
        Success,
        AlreadyExists
    }

    public async Task<CreateSessionResult> TryCreate(UserModel user, HttpContext context)
    {
        string sessionId = context.Session.GetString("_Id") ?? "";
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            context.Session.SetString("_Id", sessionId);
        }

        var existing = await _userSessionRepo.GetById(sessionId);
        if (existing != null)
            return CreateSessionResult.AlreadyExists;

        string? token = null;
        var tokenModel = await _authWebService.GetTokenModel(context.Request);
        if (tokenModel != null)
        {
            token = tokenModel.Token;
        }

        var model = new UserSessionModel()
        {
            UserId = user.Id,
            UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            Token = token,
        };
        model.Id = sessionId;
        await _userSessionRepo.InsertOrUpdate(model);
        _log.Info($"Created new session ({model.Id})");
        return CreateSessionResult.Success;
    }

    public async Task<UserSessionModel> GetOrCreateSessionForToken(ServiceAccountTokenModel tokenModel, HttpContext context)
    {
        var current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (string.IsNullOrEmpty(tokenModel.AssociatedSessionId) == false)
        {
            var sess = await _userSessionRepo.GetById(tokenModel.AssociatedSessionId!);
            if (sess != null
            &&  sess.IsDeleted == false
            && (sess.ExpiresAt == null || (sess.ExpiresAt != null && sess.ExpiresAt < current)))
            {
                _log.Info($"Returned existing session ({sess.Id})");
                return sess;
            }
        }

        UserSessionModel sessionModel = new()
        {
            UserId = tokenModel.ServiceAccountId,
            IpAddress = context.Request.GetIpAddress(),
            UserAgent = context.Request.Headers.UserAgent.ToString()
        };
        sessionModel = await _userSessionRepo.InsertOrUpdate(sessionModel);
        tokenModel.AssociatedSessionId = sessionModel.Id;
        await _serviceAccountTokenRepo.InsertOrUpdate(tokenModel);
        _log.Info($"Created new session ({sessionModel.Id})");
        return sessionModel;
    }

    public async Task<UserSessionModel?> GetOrCreateSession(HttpContext context)
    {
        if (_authWebService.TryGetTokenModel(context.Request, out var tokenModel))
        {
            return await GetOrCreateSessionForToken(tokenModel!, context);
        }
        else if (context.User.Identity?.IsAuthenticated ?? false)
        {
            var session = await GetCurrentSession(context);
            if (session == null)
            {
                // should never be null when we are authenticated
                var userModel = await _authWebService.GetCurrentUser(context);
                await TryCreate(userModel!, context);
                return await GetCurrentSession(context);
            }
        }
        return null;
    }

    public async Task TrackRequest(HttpContext context)
    {
        try
        {
            var session = await GetOrCreateSession(context);
            if (session == null)
            {
                return;
            }

            var model = new UserSessionRequestModel()
            {
                UserSessionId = session.Id,
                Method = context.Request.Method,
                Url = context.Request.Path,
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                IpAddress = context.GetRemoteIpAddress(),
                CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };
            await _userSessionRequestRepo.InsertOrUpdate(model);
        }
        catch (Exception ex)
        {
            _log.Error(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }
}
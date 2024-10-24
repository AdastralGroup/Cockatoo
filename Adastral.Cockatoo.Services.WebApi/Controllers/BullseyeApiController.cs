using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[TrackRequest]
public class BullseyeApiController : Controller
{
    private readonly BullseyeAppRepository _bullseyeAppRepo;
    private readonly ApplicationDetailRepository _appDetailRepo;
    private readonly BlogPostRepository _blogPostRepo;
    private readonly BlogPostTagRepository _blogPostTagRepo;
    private readonly BlogTagRepository _blogTagRepo;
    private readonly BullseyeCacheService _bullseyeCacheService;
    private readonly BullseyeAppRevisionRepository _bullseyeRevisionRepo;
    private readonly UserRepository _userRepo;
    private readonly PermissionWebService _permissionWebService;
    private readonly AuthWebService _authWebService;
    private readonly ScopedPermissionWebService _scopedPermissionWebService;
    public BullseyeApiController(IServiceProvider services)
        : base()
    {
        _bullseyeAppRepo = services.GetRequiredService<BullseyeAppRepository>();
        _appDetailRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _blogPostRepo = services.GetRequiredService<BlogPostRepository>();
        _blogPostTagRepo = services.GetRequiredService<BlogPostTagRepository>();
        _blogTagRepo = services.GetRequiredService<BlogTagRepository>();
        _bullseyeCacheService = services.GetRequiredService<BullseyeCacheService>();
        _bullseyeRevisionRepo = services.GetRequiredService<BullseyeAppRevisionRepository>();
        _userRepo = services.GetRequiredService<UserRepository>();
        _permissionWebService = services.GetRequiredService<PermissionWebService>();
        _authWebService = services.GetRequiredService<AuthWebService>();
        _scopedPermissionWebService = services.GetRequiredService<ScopedPermissionWebService>();
    }

    /// <summary>
    /// Get all Blog Posts associated with a Bullseye Application Revision
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>When an Application is not live, <see cref="PermissionKind.ApplicationDetailViewAll"/> is required.</item>
    /// <item>When a Revision is not live, <see cref="PermissionKind.BullseyeViewPrivateModels"/> is required.</item>
    /// <item>Blog Posts that are not live will only be included in the result when user has <see cref="PermissionKind.ApplicationBlogPostViewAll"/>
    /// </list>
    /// </remarks>
    [ProducesResponseType(typeof(List<BlogPostV1Response>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpGet("~/api/v1/Bullseye/{appId}/Revision/{revisionId}/BlogPosts")]
    public async Task<ActionResult> GetBlogPostsForRevision(string appId, string revisionId)
    {
        try
        {
            var appDetailModel = await _appDetailRepo.GetById(appId);
            if (appDetailModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRepository)} with Id {nameof(appId)}"), BaseService.SerializerOptions);
            }
            
            bool includePrivate = true;
            var user = await _authWebService.GetCurrentUser(HttpContext);
            if (appDetailModel!.IsPrivate)
            {
                includePrivate = false;
                if (user != null)
                {
                    var i = await _scopedPermissionWebService.CheckApplicationScopedPermission(
                        appDetailModel.Id, user, [PermissionKind.ApplicationDetailViewAll]);
                    if (i != ScopedPermissionWebService.CheckScopedPermissionResult.Continue)
                    {
                        Response.StatusCode = 403;
                        return Json(new NotAuthorizedResponse()
                        {
                            MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                        }, BaseService.SerializerOptions);
                    }

                    includePrivate = true;
                }
                else
                {
                    Response.StatusCode = 403;
                    return Json(new NotAuthorizedResponse()
                    {
                        MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                    }, BaseService.SerializerOptions);
                }
            }
            
            // pretend 404 when not kachemak
            if (appDetailModel.Type != ApplicationDetailType.Kachemak)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRepository)} with Id {nameof(appId)}"), BaseService.SerializerOptions);
            }

            var bullseyeRevisionModel = await _bullseyeRevisionRepo.GetById(revisionId);
            if (bullseyeRevisionModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppRevisionModel),
                    nameof(BullseyeAppRevisionModel.Id),
                    revisionId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} with Id {nameof(revisionId)}"), BaseService.SerializerOptions);
            }
            if (bullseyeRevisionModel.IsLive == false)
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.BullseyeViewPrivateModels) == false)
                {
                    Response.StatusCode = 404;
                    return Json(new NotFoundWebResponse(
                        typeof(BullseyeAppRevisionModel),
                        nameof(BullseyeAppRevisionModel.Id),
                        revisionId,
                        $"Could not find model in {nameof(BullseyeAppRevisionRepository)} with Id {nameof(revisionId)}"), BaseService.SerializerOptions);
                }
            }

            bool viewAllBlogPosts = await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.ApplicationBlogPostViewAll);

            var posts = await _blogPostRepo.GetManyForRevision(appId, !viewAllBlogPosts);
            var result = new List<BlogPostV1Response>();
            var userDict = new Dictionary<string, UserModel>();
            var tagDict = new Dictionary<string, BlogTagModel>();
            foreach (var item in posts)
            {
                var x = new BlogPostV1Response();
                x.FromModel(item);
                foreach (var uid in item.AuthorIds)
                {
                    if (userDict.ContainsKey(uid) == false)
                    {
                        var usrModel = await _userRepo.GetById(uid);
                        if (usrModel != null)
                            userDict[usrModel.Id] = usrModel;
                    }
                    if (userDict.TryGetValue(uid, out var usr))
                    {
                        var usrx = new UserV1StrippedResponse();
                        usrx.FromModel(usr);
                        x.Authors.Add(usrx);
                    }
                }

                var postTags = await _blogPostTagRepo.GetManyForPost(item.Id);
                foreach (var tag in postTags)
                {
                    if (tagDict.ContainsKey(tag.TagId) == false)
                    {
                        var tagModel = await _blogTagRepo.GetById(tag.TagId);
                        if (tagModel != null)
                            tagDict[tag.TagId] = tagModel;
                    }

                    if (tagDict.TryGetValue(tag.TagId, out var tg))
                    {
                        var tgx = new BlogPostV1TagResponse();
                        tgx.FromModel(tg);
                        x.Tags.Add(tgx);
                    }
                }

                result.Add(x);
            }
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Get the latest version of <see cref="BullseyeV1"/> for the App Id provided.
    /// </summary>
    /// <param name="appId"><see cref="BullseyeAppModel.ApplicationDetailModelId"/></param>
    /// <param name="liveState">Will be ignored and assumed as <see langword="true"/> when requesting user doesn't have <see cref="PermissionKind.BullseyeViewPrivateModels"/></param>
    [ProducesResponseType(typeof(BullseyeV1), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpGet("~/api/v1/Bullseye/{appId}")]
    public async Task<ActionResult> GetV1(string appId, bool? liveState = null)
    {
        try
        {
            var appDetailModel = await _appDetailRepo.GetById(appId);
            if (appDetailModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} {nameof(appId)}"), BaseService.SerializerOptions);
            }
            
            bool includePrivate = true;
            var user = await _authWebService.GetCurrentUser(HttpContext);
            if (appDetailModel!.IsPrivate)
            {
                includePrivate = false;
                if (user != null)
                {
                    var i = await _scopedPermissionWebService.CheckApplicationScopedPermission(
                        appDetailModel.Id, user, [PermissionKind.ApplicationDetailViewAll]);
                    if (i != ScopedPermissionWebService.CheckScopedPermissionResult.Continue)
                    {
                        Response.StatusCode = 403;
                        return Json(new NotAuthorizedResponse()
                        {
                            MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                        }, BaseService.SerializerOptions);
                    }

                    includePrivate = true;
                }
                else
                {
                    Response.StatusCode = 403;
                    return Json(new NotAuthorizedResponse()
                    {
                        MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                    }, BaseService.SerializerOptions);
                }
            }

            // pretend 404 when not kachemak
            if (appDetailModel.Type != ApplicationDetailType.Kachemak)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} {nameof(appId)}"), BaseService.SerializerOptions);
            }

            // always get live-only model when user has permission BullseyeViewPrivateModels
            var cacheModel = await _bullseyeCacheService.GetLatestV1(appId, includePrivate ? liveState : true);
            if (cacheModel == null)
            {
                throw new NoNullAllowedException($"{nameof(_bullseyeCacheService)}.{nameof(_bullseyeCacheService.GetLatestV1)} returned null");
            }
            return Json(cacheModel, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Get the latest version of <see cref="BullseyeV2"/> for the App Id provided.
    /// </summary>
    /// <param name="appId"><see cref="BullseyeAppModel.ApplicationDetailModelId"/></param>
    /// <param name="liveState">Will be ignored and assumed as <see langword="true"/> when requesting user doesn't have <see cref="PermissionKind.BullseyeViewPrivateModels"/></param>
    [ProducesResponseType(typeof(BullseyeV2), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpGet("~/api/v2/Bullseye/{appId}")]
    public async Task<ActionResult> GetV2(string appId, bool? liveState = null)
    {
        try
        {
            var appDetailModel = await _appDetailRepo.GetById(appId);
            if (appDetailModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} {nameof(appId)}"), BaseService.SerializerOptions);
            }
            bool includePrivate = true;
            var user = await _authWebService.GetCurrentUser(HttpContext);
            if (appDetailModel!.IsPrivate)
            {
                includePrivate = false;
                if (user != null)
                {
                    var i = await _scopedPermissionWebService.CheckApplicationScopedPermission(
                        appDetailModel.Id, user, [PermissionKind.ApplicationDetailViewAll]);
                    if (i != ScopedPermissionWebService.CheckScopedPermissionResult.Continue)
                    {
                        Response.StatusCode = 403;
                        return Json(new NotAuthorizedResponse()
                        {
                            MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                        }, BaseService.SerializerOptions);
                    }

                    includePrivate = true;
                }
                else
                {
                    Response.StatusCode = 403;
                    return Json(new NotAuthorizedResponse()
                    {
                        MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                    }, BaseService.SerializerOptions);
                }
            }

            // pretend 404 when not kachemak
            if (appDetailModel.Type != ApplicationDetailType.Kachemak)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} {nameof(appId)}"), BaseService.SerializerOptions);
            }

            // always get live-only model when user has permission BullseyeViewPrivateModels
            var cacheModel = await _bullseyeCacheService.GetLatestV2(appId, includePrivate ? liveState : true);
            if (cacheModel == null)
            {
                throw new NoNullAllowedException($"{nameof(_bullseyeCacheService)}.{nameof(_bullseyeCacheService.GetLatestV2)} returned null");
            }
            return Json(cacheModel, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }
}
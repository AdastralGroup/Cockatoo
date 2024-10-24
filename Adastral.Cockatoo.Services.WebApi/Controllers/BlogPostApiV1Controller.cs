using System.Text.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[TrackRequest]
public class BlogPostApiV1Controller : Controller
{
    private readonly BlogPostRepository _blogPostRepo;
    private readonly BlogPostTagRepository _blogPostTagRepo;
    private readonly BlogTagRepository _blogTagRepo;
    private readonly BlogPostService _blogPostService;
    private readonly UserRepository _userRepo;
    private readonly PermissionWebService _permissionWebService;
    public BlogPostApiV1Controller(IServiceProvider services)
        : base()
    {
        _blogPostRepo = services.GetRequiredService<BlogPostRepository>();
        _blogPostTagRepo = services.GetRequiredService<BlogPostTagRepository>();
        _blogTagRepo = services.GetRequiredService<BlogTagRepository>();
        _blogPostService = services.GetRequiredService<BlogPostService>();
        _userRepo = services.GetRequiredService<UserRepository>();
        _permissionWebService = services.GetRequiredService<PermissionWebService>();
    }

    /// <summary>
    /// Get a blog post.
    /// </summary>
    /// <remarks>
    /// When <see cref="BlogPostModel.IsLive"/> is <see langword="false"/>, then <see cref="PermissionKind.ApplicationBlogPostViewAll"/> is required to get Status 200.
    /// </remarks>
    [ProducesResponseType(typeof(BlogPostModel), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpGet("~/api/v1/Blog/Post/{id}")]
    public async Task<ActionResult> GetBlogPost(string id)
    {
        try
        {
            var model = await _blogPostRepo.GetById(id);
            if (model == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }
            if (model.IsLive == false)
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.ApplicationBlogPostViewAll) == false)
                {
                    Response.StatusCode = 404;
                    return Json(new NotFoundWebResponse(
                        typeof(BlogPostModel),
                        nameof(BlogPostModel.Id),
                        id,
                        $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
                }
            }

            return Json(model, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Get all tags for a blog post
    /// </summary>
    /// <remarks>
    /// When <see cref="BlogPostModel.IsLive"/> is <see langword="false"/>, then <see cref="PermissionKind.ApplicationBlogPostViewAll"/> is required to get Status 200.
    /// </remarks>
    [ProducesResponseType(typeof(List<BlogTagModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpGet("~/api/v1/Blog/Post/{id}/Tags")]
    public async Task<ActionResult> GetBlogPostTags(string id)
    {
        try
        {
            var model = await _blogPostRepo.GetById(id);
            if (model == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }
            if (model.IsLive == false)
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.ApplicationBlogPostViewAll) == false)
                {
                    Response.StatusCode = 404;
                    return Json(new NotFoundWebResponse(
                        typeof(BlogPostModel),
                        nameof(BlogPostModel.Id),
                        id,
                        $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
                }
            }

            var postTags = await _blogPostTagRepo.GetManyForPost(model.Id);
            var tags = await _blogTagRepo.GetManyById(postTags.Select(v => v.TagId).ToArray());
            return Json(tags, CoreContext.Instance);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Create a new Blog Post
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostCreate"/>
    /// </remarks>
    [ProducesResponseType(typeof(BlogPostModel), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpPost("~/api/v1/Blog/Post/Create")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BlogPostCreate)]
    public async Task<ActionResult> CreateBlogPost()
    {
        try
        {
            var model = new BlogPostModel();
            await _blogPostRepo.InsertOrUpdate(model);
            return Json(model, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Bulk update fields for a Blog Post
    /// </summary>
    [HttpPatch("~/api/v1/Blog/Post/{blogPostId}")]
    [AuthRequired]
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostV1Response>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    public async Task<ActionResult> UpdateBlogPost(
        string blogPostId,
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] BlogPostV1UpdateRequest data)
    {
        try
        {
            var blogPostModel = await _blogPostRepo.GetById(blogPostId);
            if (blogPostModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    blogPostId,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {blogPostId}"), BaseService.SerializerOptions);
            }
            if (blogPostModel.IsLive == false)
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.ApplicationBlogPostViewAll) == false)
                {
                    Response.StatusCode = 404;
                    return Json(new NotFoundWebResponse(
                        typeof(BlogPostModel),
                        nameof(BlogPostModel.Id),
                        blogPostId,
                        $"Could not find model in {nameof(BlogPostRepository)} with Id {blogPostId}"), BaseService.SerializerOptions);
                }
            }

            if (data.IsLive != null)
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.BlogPostEditor) == false)
                {
                    Response.StatusCode = 401;
                    return Json(new ExceptionWebResponse(new Exception($"Missing Permission {PermissionKind.BlogPostEditor} (required for {nameof(data.IsLive)})")), BaseService.SerializerOptions);
                }
            }
            if (!string.IsNullOrEmpty(data.Title)
              || string.IsNullOrEmpty(data.Content)
              || !string.IsNullOrEmpty(data.ApplicationId)
              || !string.IsNullOrEmpty(data.BullseyeRevisionId))
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.BlogPostEditor) == false)
                {
                    Response.StatusCode = 401;
                    return Json(new ExceptionWebResponse(new Exception($"Missing Permission {PermissionKind.BlogPostEditor}")), BaseService.SerializerOptions);
                }
            }

            if (data.Authors != null)
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.BlogPostUpdateAuthors) == false)
                {
                    Response.StatusCode = 401;
                    return Json(new ExceptionWebResponse(new Exception($"Missing Permission {PermissionKind.BlogPostUpdateAuthors}")), BaseService.SerializerOptions);
                }
            }
            if (data.Tags != null)
            {
                if (await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.BlogPostUpdateTags) == false)
                {
                    Response.StatusCode = 401;
                    return Json(new ExceptionWebResponse(new Exception($"Missing Permission {PermissionKind.BlogPostUpdateTags}")), BaseService.SerializerOptions);
                }
            }

            // TODO write data to the database. values that are null should be ignored.

            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Delete a blog post.
    /// </summary>
    /// <param name="id"><see cref="BlogPostModel.Id"/></param>
    /// <param name="deleteResources">Delete attachments from Storage. Requires <see cref="PermissionKind.FileDelete"/></param>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostDelete"/>
    /// </remarks>
    [ProducesResponseType(typeof(BlogPostV1DeleteResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpDelete("~/api/v1/Blog/Post/{id}")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BlogPostDelete)]
    public async Task<ActionResult> DeleteBlogPost(string id, [FromQuery] bool deleteResources)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }

            bool allowResourceDelete = await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.FileDelete);
            var result = await _blogPostService.Delete(new()
            {
                Id = postModel.Id,
                DeleteStorageResources = allowResourceDelete && deleteResources
            });
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
    /// Update <see cref="BlogPostModel.IsLive"/>
    /// </summary>
    /// <remarks>
    /// Requires permission <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpPatch("~/api/v1/Blog/Post/{id}/IsLive")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BlogPostEditor)]
    public async Task<ActionResult> ChangeIsLiveState(string id, bool isLive)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }

            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.IsLive = isLive;
            await _blogPostRepo.InsertOrUpdate(postModel);
            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Add an Author to a Blog Post.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostUpdateAuthors"/>, or <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpPatch("~/api/v1/Blog/Post/{id}/Author/{authorUserId}")]
    [AuthRequired]
    [PermissionRequired([PermissionKind.BlogPostUpdateAuthors, PermissionKind.BlogPostEditor])]
    public async Task<ActionResult> AddAuthor(string id, string authorUserId)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }
            var userModel = await _userRepo.GetById(authorUserId);
            if (userModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(UserModel),
                    nameof(UserModel.Id),
                    authorUserId,
                    $"Could not find model in {nameof(UserRepository)} with Id {authorUserId}"), BaseService.SerializerOptions);
            }

            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.AuthorIds.Add(authorUserId);
            postModel.AuthorIds = postModel.AuthorIds.Distinct().ToList();
            await _blogPostRepo.InsertOrUpdate(postModel);
            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Remove an Author from a Blog Post.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostUpdateAuthors"/>, or <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpDelete("~/api/v1/Blog/Post/{id}/Author/{authorUserId}")]
    [AuthRequired]
    [PermissionRequired([PermissionKind.BlogPostUpdateAuthors, PermissionKind.BlogPostEditor])]
    public async Task<ActionResult> RemoveAuthor(string id, string authorUserId)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }

            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.AuthorIds = postModel.AuthorIds.Where(v => v != authorUserId).ToList();
            await _blogPostRepo.InsertOrUpdate(postModel);
            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Set authors on a Blog Post.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostUpdateAuthors"/>, or <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [HttpPost("~/api/v1/Blog/Post/{id}/Author")]
    [AuthRequired]
    [PermissionRequired([PermissionKind.BlogPostUpdateAuthors, PermissionKind.BlogPostEditor])]
    public async Task<ActionResult> SetAuthors(string id, List<string> authorUserIds)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }
            foreach (var index in Enumerable.Range(0, authorUserIds.Count))
            {
                var userModel = await _userRepo.GetById(authorUserIds[index]);
                if (userModel == null)
                {
                    Response.StatusCode = 404;
                    return Json(new NotFoundWebResponse(
                        typeof(UserModel),
                        nameof(UserModel.Id),
                        authorUserIds[index],
                        $"Could not find model in {nameof(UserRepository)} with Id {authorUserIds[index]} (index: {index})"), BaseService.SerializerOptions);
                }
            }

            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.AuthorIds = authorUserIds;
            await _blogPostRepo.InsertOrUpdate(postModel);
            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Add a Tag to a Blog Post
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostUpdateTags"/> or <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<List<BlogPostTagModel>>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired([PermissionKind.BlogPostUpdateTags, PermissionKind.BlogPostEditor])]
    [HttpPatch("~/api/v1/Blog/Post/{id}/Tag/{tagId}")]
    public async Task<ActionResult> AddPostTag(string id, string tagId)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }

            var tagModel = await _blogTagRepo.GetById(tagId);
            if (tagModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogTagModel),
                    nameof(BlogTagModel.Id),
                    tagId,
                    $"Could not find model in {nameof(BlogTagRepository)} with Id {tagId}"), BaseService.SerializerOptions);
            }

            var after = await _blogPostTagRepo.GetManyForPost(id);
            var before = JsonSerializer.Deserialize<List<BlogPostTagModel>>(JsonSerializer.Serialize(after, BaseService.SerializerOptions), BaseService.SerializerOptions);
            if (await _blogPostTagRepo.AssociationExists(id, tagId) == false)
            {
                var model = new BlogPostTagModel()
                {
                    BlogPostId = id,
                    TagId = tagId
                };
                await _blogPostTagRepo.InsertOrUpdate(model);
                after.Add(model);
            }
            return Json(new ComparisonResponse<List<BlogPostTagModel>>(before, after), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Remove a Tag from a Blog Post
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostUpdateTags"/> or <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<List<BlogPostTagModel>>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired([PermissionKind.BlogPostUpdateTags, PermissionKind.BlogPostEditor])]
    [HttpDelete("~/api/v1/Blog/Post/{id}/Tag/{tagId}")]
    public async Task<ActionResult> RemovePostTag(string id, string tagId)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }

            var before = await _blogPostTagRepo.GetManyForPost(id);
            foreach (var x in before)
            {
                if (x.TagId == tagId && x.BlogPostId == id)
                {
                    await _blogPostTagRepo.Delete(x.Id);
                }
            }
            var after = await _blogPostTagRepo.GetManyForPost(id);
            return Json(new ComparisonResponse<List<BlogPostTagModel>>(before, after), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Update Blog Post Title.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BlogPostEditor)]
    [HttpPost("~/api/v1/Blog/Post/{id}/Title")]
    public async Task<ActionResult> UpdateTitle(string id, string title)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }


            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.Title = title;
            await _blogPostRepo.InsertOrUpdate(postModel);

            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Update Blog Post Content.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BlogPostEditor)]
    [HttpPost("~/api/v1/Blog/Post/{id}/Content")]
    public async Task<ActionResult> UpdateContent(string id, string content)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }


            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.Content = content;
            await _blogPostRepo.InsertOrUpdate(postModel);

            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Update what Application the Blog Post is associated with.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BlogPostEditor)]
    [HttpPost("~/api/v1/Blog/Post/{id}/ApplicationId")]
    public async Task<ActionResult> UpdateApplicationId(string id, string applicationId)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }


            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.ApplicationId = string.IsNullOrEmpty(applicationId) ? null : applicationId;
            await _blogPostRepo.InsertOrUpdate(postModel);

            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Update what Bullseye Revision the Blog Post is associated with.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BlogPostEditor"/>
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BlogPostModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BlogPostEditor)]
    [HttpPost("~/api/v1/Blog/Post/{id}/BullseyeRevision")]
    public async Task<ActionResult> UpdateBullseyeRevisionId(string id, string bullseyeRevisionId)
    {
        try
        {
            var postModel = await _blogPostRepo.GetById(id);
            if (postModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BlogPostModel),
                    nameof(BlogPostModel.Id),
                    id,
                    $"Could not find model in {nameof(BlogPostRepository)} with Id {id}"), BaseService.SerializerOptions);
            }


            var before = JsonSerializer.Deserialize<BlogPostModel>(JsonSerializer.Serialize(postModel, BaseService.SerializerOptions), BaseService.SerializerOptions);
            postModel.BullseyeRevisionId = string.IsNullOrEmpty(bullseyeRevisionId) ? null : bullseyeRevisionId;
            await _blogPostRepo.InsertOrUpdate(postModel);

            return Json(new ComparisonResponse<BlogPostModel>(before, postModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }
}
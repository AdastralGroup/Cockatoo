using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi.Controllers
{
    [Route("~/api/v1/Manage/ClientApiKeyProvider")]
    [ApiController]
    [AuthRequired]
    [TrackRequest]
    public class ManageClientApiKeyProviderApiV1Controller : Controller
    {
        private readonly StorageFileRepository _storageFileRepo;
        private readonly ClientApiKeyProviderRepository _clientApiKeyProviderRepo;
        public ManageClientApiKeyProviderApiV1Controller(IServiceProvider services)
            : base()
        {
            _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
            _clientApiKeyProviderRepo = services.GetRequiredService<ClientApiKeyProviderRepository>();
        }

        [HttpGet("List")]
        [AuthRequired]
        [PermissionRequired(PermissionKind.ClientApiKeyProviderManager)]
        [ProducesResponseType(typeof(ManageClientApiKeyProviderV1ListResponse), 200, "application/json")]
        public async Task<ActionResult> List()
        {
            var data = await _clientApiKeyProviderRepo.GetAll();
            var files = new Dictionary<string, StorageFileModel>();
            var result = new List<ManageClientApiKeyProviderV1DetailsResponse>();
            foreach (var item in data)
            {
                var i = new ManageClientApiKeyProviderV1DetailsResponse()
                {
                    Id = item.Id,
                    DisplayName = item.DisplayName,
                    CreatedAt = item.CreatedAt.Value,
                    BrandIconFileId = item.BrandIconFileId,
                    IsActive = item.IsActive,
                    PublicKeyXml = item.PublicKeyXml
                };
                if (!string.IsNullOrEmpty(item.BrandIconFileId))
                {
                    if (files.TryGetValue(item.BrandIconFileId, out var p))
                    {
                        i.BrandIconFile = p;
                    }
                    else
                    {
                        var file = await _storageFileRepo.GetById(item.BrandIconFileId);
                        if (file != null)
                        {
                            i.BrandIconFile = file;
                            files[item.BrandIconFileId] = file;
                        }
                    }
                }
                result.Add(i);
            }

            Response.StatusCode = 200;
            return Json(
                new ManageClientApiKeyProviderV1ListResponse()
                {
                    Items = result
                }, BaseService.SerializerOptions);
        }
    }
}

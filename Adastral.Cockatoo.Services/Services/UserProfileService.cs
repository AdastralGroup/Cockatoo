using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class UserProfileService : BaseService
{
    private readonly StorageService _storageService;
    private readonly UserPreferencesRepository _userPrefRepo;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly UserRepository _userRepo;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public UserProfileService(IServiceProvider services)
        : base(services)
    {
        _storageService = services.GetRequiredService<StorageService>();
        _userPrefRepo = services.GetRequiredService<UserPreferencesRepository>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _userRepo = services.GetRequiredService<UserRepository>();
    }
    public async Task UploadAvatarForUser(UserModel user, string filename, Stream fileStream, long? fileSize = null)
    {
        var preferencesModel = await _userPrefRepo.GetById(user.Id) ?? new();
        preferencesModel.UserId = user.Id;
        if ((fileSize ?? fileStream.Length) > 8_000_000)
        {
            throw new ArgumentException($"File is too large. Must be <8MB (is {FormatHelper.FileSize(fileSize ?? fileStream.Length)})");
        }
        using (var ms = new MemoryStream())
        {
            await fileStream.CopyToAsync(ms);
            if (ms.Length < 8_000_000)
            {
                var s = await _storageService.UploadFile(ms, filename);

                // delete existing file (if there is one)
                if (!string.IsNullOrEmpty(preferencesModel!.AvatarStorageFileId))
                {
                    var existingFile = await _storageFileRepo.GetById(preferencesModel.AvatarStorageFileId);
                    if (existingFile != null)
                    {
                        await _storageService.Delete(existingFile);
                    }
                }
                preferencesModel.AvatarStorageFileId = s.Id;
            }
            else
            {
                throw new ArgumentException($"File is too large. Must be <8MB (is {FormatHelper.FileSize(ms.Length)})");
            }
        }
        await _userPrefRepo.InsertOrUpdate(preferencesModel);
    }

    public async Task DeleteAvatarForUser(UserModel user)
    {
        var preferencesModel = await _userPrefRepo.GetById(user.Id) ?? new();
        preferencesModel.UserId = user.Id;
        if (!string.IsNullOrEmpty(preferencesModel.AvatarStorageFileId))
        {
            var fileModel = await _storageFileRepo.GetById(preferencesModel.AvatarStorageFileId);
            if (fileModel != null)
            {
                await _storageService.Delete(fileModel);
            }
        }
        preferencesModel.AvatarStorageFileId = null;
        await _userPrefRepo.InsertOrUpdate(preferencesModel);
    }
}
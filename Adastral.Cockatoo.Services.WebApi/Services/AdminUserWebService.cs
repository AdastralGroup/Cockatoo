using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class AdminUserWebService : BaseService
{
    private readonly UserRepository _userRepository;
    private readonly UserPreferencesRepository _userPreferencesRepository;
    private readonly ServiceAccountRepository _serviceAccountRepository;

    public AdminUserWebService(IServiceProvider services)
        : base(services)
    {
        _userRepository = services.GetRequiredService<UserRepository>();
        _userPreferencesRepository = services.GetRequiredService<UserPreferencesRepository>();
        _serviceAccountRepository = services.GetRequiredService<ServiceAccountRepository>();
    }

    public async Task<AdminUserV1DetailResponse?> GetDetails(string userId)
    {
        var user = await _userRepository.GetById(userId);
        if (user == null)
            return null;

        return await GetDetails(user);
    }

    public async Task<AdminUserV1DetailResponse?> GetDetails(UserModel user)
    {
        var response = new AdminUserV1DetailResponse();
        response.FromModel(user);
        var preferences = await _userPreferencesRepository.GetById(user.Id);
        response.FromModel(preferences);

        if (user.IsServiceAccount)
        {
            var serviceAccount = await _serviceAccountRepository.GetById(user.Id);
            response.FromModel(serviceAccount);
        }

        return response;
    }

    public async Task<List<AdminUserV1DetailResponse>> GetAll()
    {
        var result = new List<AdminUserV1DetailResponse>();
        foreach (var user in await _userRepository.GetAll())
        {
            var data = await GetDetails(user);
            if (data != null)
            {
                result.Add(data);
            }
        }

        return result;
    }
}
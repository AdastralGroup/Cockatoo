using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using kate.shared.Helpers;

namespace Adastral.Cockatoo.Client;

public partial class CockatooRestClient
{
    private readonly CockatooRestClientOptions _options;
    
    public CockatooRestClient(CockatooRestClientOptions options)
    {
        _options = options.Clone();
        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, cetChain, policyErrors) => true;
        _httpClient = new HttpClient(handler);
        _httpClient.Timeout = TimeSpan.FromDays(2);
        _httpClient.DefaultRequestHeaders.Add("x-cockatoo-token", _options.Token);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", GetType().ToString());

        _endpoint = new()
        {
            Base = _options.BaseUri.ToString(),
            Token = _options.Token
        };

        ManageBullseye = new(this);
        ManageApplication = new(this);
        Bullseye = new(this);
        Southbank = new(this);
        Storage = new(this);
        Admin = new(this);
    }
    internal readonly HttpClient _httpClient;
    internal readonly EndpointMessages _endpoint;
    public readonly ManageBullseyeV1RestClient ManageBullseye;
    public readonly ManageApplicationV1RestClient ManageApplication;
    public readonly BullseyeRestClient Bullseye;
    public readonly SouthbankRestClient Southbank;
    public readonly StorageFileV1RestClient Storage;
    public readonly AdminV1RestClient Admin;
    public async Task<List<ApplicationDetailModel>> GetAvailableApplications()
    {
        var response = await _httpClient.SendAsync(_endpoint.ApplicationDetailV1GetAvailable());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<List<ApplicationDetailModel>>(response, responseText, true)!;
    }

    public async Task<ApplicationDetailModel> GetApplication(string applicationId)
    {
        var response = await _httpClient.SendAsync(_endpoint.ApplicationDetailV1Get(applicationId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<ApplicationDetailModel>(response, responseText, true)!;
    }
    
    public async Task<UserModel> GetOwnUser()
    {
        var response = await _httpClient.SendAsync(_endpoint.UserV1GetSelf());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<UserModel>(response, responseText, true)!;
    }

    public async Task<UserModel> GetUser(string userId)
    {
        var response = await _httpClient.SendAsync(_endpoint.UserV1Get(userId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<UserModel>(response, responseText, true)!;
    }

    public async Task<ServiceAccountTokenModel> CreateToken(UserV1CReateTokenRequest data)
    {
        var response = await _httpClient.SendAsync(_endpoint.UserV1CreateToken(data));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<ServiceAccountTokenModel>(response, responseText, true)!;
    }

    public async Task PermissionAdminDeleteRole(string roleId)
    {
        var response = await _httpClient.SendAsync(_endpoint.PermissionAdminApiV1DeleteRole(roleId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        if (response.IsSuccessStatusCode == false)
        {
            throw new APIClientRequestFailureException(response, responseText);
        }
    }
    
    #region Blog Post
    public async Task<BlogPostModel> CreateBlogPost()
    {
        var response = await _httpClient.SendAsync(_endpoint.BlogPostV1Create());
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<BlogPostModel>(response, responseText, true)!;
    }

    public async Task<BlogPostV1Response> GetBlogPost(string blogPostId)
    {
        var response = await _httpClient.SendAsync(_endpoint.BlogPostV1Get(blogPostId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<BlogPostV1Response>(response, responseText, true)!;
    }

    public async Task<BlogPostV1DeleteResponse> DeleteBlogPost(string blogPostId, bool deleteResources)
    {
        var response = await _httpClient.SendAsync(_endpoint.BlogPostV1Delete(blogPostId, deleteResources));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<BlogPostV1DeleteResponse>(response, responseText, true)!;
    }

    public async Task<ComparisonResponse<BlogPostV1Response>> UpdateBlogPost(string blogPostId, BlogPostV1UpdateRequest data)
    {
        var response = await _httpClient.SendAsync(_endpoint.BlogPostV1Update(blogPostId, data));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<ComparisonResponse<BlogPostV1Response>>(response, responseText, true)!;
    }

    public async Task<List<BlogTagModel>> GetBlogPostTags(string blogPostId)
    {
        var response = await _httpClient.SendAsync(_endpoint.BlogPostV1GetTags(blogPostId));
        var responseText = response.Content.ReadAsStringAsync().Result;
        return CastOnSuccess<List<BlogTagModel>>(response, responseText, true)!;
    }
    #endregion
    
    internal TData? CastOnSuccess<TData>(HttpResponseMessage response, string responseText, bool throwWhenNull)
        where TData : class, new()
    {
        if (response.IsSuccessStatusCode)
        {
            var data = JsonSerializer.Deserialize<TData>(responseText, BaseService.SerializerOptions);
            if (throwWhenNull && data == null)
                throw new NoNullAllowedException($"Failed to parse {typeof(TData).Name} (deserialized to null)");
            return data;
        }

        ParseError(response, responseText);
        throw new APIClientRequestFailureException(response, responseText);
    }

    internal TStruct CastOnSuccess<TStruct>(HttpResponseMessage response, Stream responseStream)
        where TStruct : struct, IBinarySerialize
    {
        if (responseStream.CanSeek)
        {
            responseStream.Seek(0, SeekOrigin.Begin);
        }

        if (!responseStream.CanRead)
        {
            throw new ArgumentException($"Stream does not support reading", nameof(responseStream));
        }
        if (response.IsSuccessStatusCode)
        {
            string? responseContentType = response.Content.Headers.ContentType?.MediaType;
            string customEncodingKind = (response.Headers.GetValues("x-text-encoding").FirstOrDefault() ?? "").Trim().ToUpper();
            if (responseContentType == "application/octet-stream")
            {
                var result = new TStruct();
                using (var reader = new BinaryReader(responseStream))
                {
                    result.Deserialize(reader);
                }

                return result;
            }
            else if (responseContentType == "text/plain" && customEncodingKind == "BASE64")
            {
                var responseText = "";
                using (var s = new StreamReader(responseStream))
                {
                    responseText = s.ReadToEnd();
                }

                var result = new TStruct();
                using (var ms = new MemoryStream(Convert.FromBase64String(responseText)))
                {
                    using (var reader = new BinaryReader(ms))
                    {
                        result.Deserialize(reader);
                    }
                }

                return result;
            }
            else
            {
                throw new NotImplementedException($"Unknown response content type {responseContentType}");
            }
        }
        else
        {
            string responseText = "";
            using (var s = new StreamReader(responseStream))
            {
                responseText = s.ReadToEnd();
            }
            throw new APIClientRequestFailureException(response, responseText);
        }
    }
    
    

    public static void ParseError(HttpResponseMessage response, string content)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.NotFound:
                var notFoundResponse = JsonSerializer.Deserialize<NotFoundResponse>(content, BaseService.SerializerOptions);
                if (notFoundResponse == null)
                {
                    throw new NoNullAllowedException($"Failed to parse {nameof(NotFoundResponse)} (deserialized to null)", new APIClientRequestFailureException(response, content));
                }

                throw new NotFoundException(notFoundResponse);
            case HttpStatusCode.Unauthorized:
                var notAuthorizedResponse = JsonSerializer.Deserialize<NotAuthorizedResponse>(content, BaseService.SerializerOptions);
                if (notAuthorizedResponse == null)
                {
                    throw new NoNullAllowedException($"Failed to parse {nameof(NotAuthorizedResponse)} (deserialized to null)", new APIClientRequestFailureException(response, content));
                }

                throw new NotAuthorizedException(notAuthorizedResponse);
        }
    }
}
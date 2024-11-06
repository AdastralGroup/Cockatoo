using System.Text.Json;
using System.Text.RegularExpressions;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class ManageApplicationWebService(IServiceProvider services) : BaseService(services)
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly ApplicationDetailRepository _appDetailRepo = services.GetRequiredService<ApplicationDetailRepository>();
    private readonly ApplicationColorRepository _appColorRepo = services.GetRequiredService<ApplicationColorRepository>();
    private readonly ApplicationImageRepository _appImageRepo = services.GetRequiredService<ApplicationImageRepository>();
    private readonly StorageFileRepository _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
    public async Task<ManageApplicationV1CreateResponse> CreateApplication(ManageApplicationV1CreateRequest requestData)
    {
        if (string.IsNullOrEmpty(requestData.Name))
        {
            throw new ArgumentException($"Value must not be null or empty on property {nameof(requestData.Name)}", nameof(requestData));
        }

        var application = new ApplicationDetailModel()
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = requestData.Name
        };
        if (!string.IsNullOrEmpty(requestData.Version))
        {
            application.LatestVersion = requestData.Version;
        }
        if (requestData.Type != null)
        {
            application.Type = (ApplicationDetailType)requestData.Type;
        }
        if (requestData.Private != null)
        {
            application.IsPrivate = requestData.Private == true;
        }
        if (requestData.Hidden != null)
        {
            application.IsHidden = requestData.Hidden == true;
        }
        if (requestData.Managed != null)
        {
            application.Managed = requestData.Managed == true;
        }

        if (requestData.AppVarData?.Mod != null)
        {
            requestData.AppVarData?.Mod.InsertInto(application.AppVarData.Mod);
        }
        if (requestData.AppVarData?.ExternalMetadata != null)
        {
            requestData.AppVarData?.ExternalMetadata.InsertInto(application.AppVarData.Remote);
        }

        var colorInsert = new List<ApplicationColorModel>();
        var imageInsert = new List<ApplicationImageModel>();
        var associatedFiles = new Dictionary<string, StorageFileModel>();
        string? ParseColor(string color, out string? errorText)
        {
            errorText = null;
            var colorRegex = new Regex(@"^(#|)[0-9a-f]{6}$");
            if (colorRegex.IsMatch(color))
            {
                if (color.StartsWith("#"))
                    return color;
                return $"#{color}";
            }
            var colorRegexShort = new Regex(@"^(#|)[0-9a-f]{3}$");
            if (colorRegexShort.IsMatch(color))
            {
                var p = color.Substring(color.IndexOf("#") + 1);
                string s = "";
                for (int i = 0; i < p.Length; i++)
                {
                    s += $"{p[i]}{p[i]}";
                }
                return $"#{s}";
            }
            var colorRegexIntArray = new Regex(@"^[0-9]{1,3}\s{0,},[0-9]{1,3}\s{0,},[0-9]{1,3}\s{0,}$");
            if (colorRegexIntArray.IsMatch(color))
            {
                var split = color.Split(",").Select(v => v.Trim()).ToArray();
                var s = "";
                var i = 0;
                foreach (var x in split)
                {
                    if (!int.TryParse(x, out var p))
                    {
                        errorText = $"Cannot convert integer array to hex color. Invalid number";
                        return null;
                    }
                    if (p > 255)
                    {
                        errorText = $"Cannot convert integer to hex color. Item {i} is greater than 255";
                        return null;
                    }
                    s += p.ToString("x").PadLeft(2, '0');
                    i++;
                }
            }
            errorText = $"Unable to parse value \"{color}\" since it is an invalid hex color.";
            return null;
        }

        var errors = new List<(string, string)>();

        foreach (var (t, v) in requestData.AppVarData?.Colors ?? [])
        {
            string ns = $"{nameof(requestData)}.{nameof(requestData.AppVarData)}.{nameof(requestData.AppVarData.Colors)}[{(int)t}]";
            try
            {
                var c = ParseColor(v, out var errorText);
                if (c != null)
                {
                    colorInsert.Add(new ApplicationColorModel()
                    {
                        ApplicationId = application.Id,
                        Kind = t,
                        Value = c
                    });
                }
                else
                {
                    errors.Add((ns,
                    errorText ?? $"Invalid value \"{v}\""));
                }
            }
            catch (Exception ex)
            {
                errors.Add((ns, $"Exception thrown \"{ex.Message}\""));
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetExtra(nameof(requestData), JsonSerializer.Serialize(requestData, BaseService.SerializerOptions));
                    scope.SetExtra("workingItem", ns);
                });
                _log.Error($"Failed to process item {t} for request\n{ex}");
            }
        }

        foreach (var i in GeneralHelper.GetEnumList<ApplicationColorKind>()
            .Where(v => !colorInsert.Any(x => x.Kind == v)))
        {
            colorInsert.Add(new ApplicationColorModel()
            {
                ApplicationId = application.Id,
                Kind = i,
                Value = null
            });
        }

        async Task InsertImageUrls()
        {
            var missingKinds = GeneralHelper.GetEnumList<ApplicationImageKind>()
                .Where(v => !imageInsert.Any(x => x.Kind == v)).ToList();
            foreach (var (kind, url) in requestData.AppVarData?.ImageUrls ?? [])
            {
                string ns = $"{nameof(requestData)}.{nameof(requestData.AppVarData)}.{nameof(requestData.AppVarData.ImageUrls)}[{(int)kind}]";

                if (!missingKinds.Contains(kind))
                    continue;
                try
                {
                    var opts = new UriCreationOptions();
                    if (Uri.TryCreate(url, in opts, out var x))
                    {
                        var obj = new ApplicationImageModel()
                        {
                            ApplicationId = application.Id,
                            Kind = kind,
                            Url = url,
                            IsManagedFile = false
                        };
                        try
                        {
                            await obj.UpdateHash();
                        }
                        catch (Exception ex)
                        {
                            errors.Add((ns, $"Failed to get SHA256: {ex}"));
                            SentrySdk.CaptureException(ex, scope =>
                            {
                                scope.SetExtra(nameof(requestData), JsonSerializer.Serialize(requestData, BaseService.SerializerOptions));
                                scope.SetExtra("workingItem", ns);
                                scope.SetTag("innerTask", nameof(InsertImageUrls));
                            });
                            _log.Error($"Failed to process item {kind} for request\n{ex}");
                            continue;
                        }
                        imageInsert.Add(obj);
                    }
                    else
                    {
                        errors.Add((ns, $"Invalid URL"));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add((ns, $"Failed to insert value: {ex}"));
                    SentrySdk.CaptureException(ex, scope =>
                    {
                        scope.SetExtra(nameof(requestData), JsonSerializer.Serialize(requestData, BaseService.SerializerOptions));
                        scope.SetExtra("workingItem", ns);
                        scope.SetTag("innerTask", nameof(InsertImageUrls));
                    });
                    _log.Error($"Failed to process item {kind} for request\n{ex}");
                }
            }
        }

        async Task InsertImageIds()
        {
            var missingKinds = GeneralHelper.GetEnumList<ApplicationImageKind>()
                .Where(v => !imageInsert.Any(x => x.Kind == v)).ToList();
            foreach (var (kind, id) in requestData.AppVarData?.ImageIds ?? [])
            {
                string ns = $"{nameof(requestData)}.{nameof(requestData.AppVarData)}.{nameof(requestData.AppVarData.ImageIds)}[{(int)kind}]";
                if (!missingKinds.Contains(kind))
                try
                {
                    var file = await _storageFileRepo.GetById(id);
                    if (file == null)
                    {
                        errors.Add((ns, $"File not found ({id})"));
                        continue;
                    }

                    if (!file.Location.EndsWith(".png"))
                    {
                        errors.Add((ns, $"Invalid file format, must be png"));
                        continue;
                    }

                    associatedFiles[file.Id] = file;

                    imageInsert.Add(new ApplicationImageModel()
                    {
                        ApplicationId = application.Id,
                        Kind = kind,
                        Sha256Hash = file.Sha256Hash,
                        IsManagedFile = true,
                        ManagedFileId = file.Id
                    });
                }
                catch (Exception ex)
                {
                    errors.Add((ns, $"Failed to insert value: {ex}"));
                    SentrySdk.CaptureException(ex, scope =>
                    {
                        scope.SetExtra(nameof(requestData), JsonSerializer.Serialize(requestData, BaseService.SerializerOptions));
                        scope.SetExtra("workingItem", ns);
                        scope.SetTag("innerTask", nameof(InsertImageIds));
                    });
                    _log.Error($"InsertImageIds|Failed to process item {kind} for request\n{ex}");
                }
            }
        }

        if (requestData.AppVarData != null)
        {
            if (requestData.AppVarData.PrioritizeImageId)
            {
                await InsertImageIds();
                await InsertImageUrls();
            }
            else
            {
                await InsertImageUrls();
                await InsertImageIds();
            }
        }

        async Task Rollback()
        {
            try
            {
                await _appDetailRepo.DeleteById(application.Id);
            }
            catch {}
            foreach (var c in colorInsert)
            {
                try
                {
                    await _appColorRepo.DeleteById(c.Id);
                }
                catch {}
            }
            foreach (var i in imageInsert)
            {
                try
                {
                    await _appImageRepo.DeleteById(i.Id);
                }
                catch {}
            }
        }

        try
        {
            await _appDetailRepo.InsertOrUpdate(application);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to insert {typeof(ApplicationDetailModel)}\n{ex}");
            await Rollback();
            throw;
        }
        foreach (var c in colorInsert)
        {
            try
            {
                await _appColorRepo.InsertOrUpdate(c);
            }
            catch (Exception ex)
            {
                var d = JsonSerializer.Serialize(c, BaseService.SerializerOptions);
                _log.Error($"Failed to insert {typeof(ApplicationColorModel)} {d}\n{ex}");
                await Rollback();
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetExtra("targetObject", d);
                });
                throw;
            }
        }
        foreach (var i in imageInsert)
        {
            try
            {
                await _appImageRepo.InsertOrUpdate(i);
            }
            catch (Exception ex)
            {
                var d = JsonSerializer.Serialize(i, BaseService.SerializerOptions);
                _log.Error($"Failed to insert {typeof(ApplicationImageModel)} {d}\n{ex}");
                await Rollback();
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetExtra("targetObject", d);
                });
                throw;
            }
        }

        var result = new ManageApplicationV1CreateResponse()
        {
            Application = application,
            Colors = colorInsert,
            Images = imageInsert,
            Files = associatedFiles
        };
        if (errors.Count > 0)
        {
            result.Errors = errors.Select(v => new ManageApplicationV1CreateResponse.InnerErrorResponseItem(v.Item1, v.Item2)).ToList();
        }
        return result;
    }
}
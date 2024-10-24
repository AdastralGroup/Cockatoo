using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using FluentScheduler;
using kate.shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class SouthbankService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly SouthbankCacheRepository _sbCacheRepo;
    private readonly ApplicationDetailRepository _appDetailRepo;
    private readonly ApplicationImageRepository _appImageRepo;
    private readonly ApplicationColorRepository _appColorRepo;
    private readonly StorageService _storageService;
    private readonly TaskMutexService _taskMutexService;
    private readonly CockatooConfig _config;
    public SouthbankService(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<CockatooConfig>();
        _sbCacheRepo = services.GetRequiredService<SouthbankCacheRepository>();
        _appDetailRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _appImageRepo = services.GetRequiredService<ApplicationImageRepository>();
        _appColorRepo = services.GetRequiredService<ApplicationColorRepository>();
        _storageService = services.GetRequiredService<StorageService>();
        _taskMutexService = services.GetRequiredService<TaskMutexService>();
    }

    /// <summary>
    /// Re-generate an instance of <see cref="SouthbankCacheModel"/>, and insert it via <see cref="SouthbankCacheRepository.InsertOrUpdate"/>
    /// </summary>
    public async Task<SouthbankCacheModel> GenerateSouthbank()
    {
        var apps = await _appDetailRepo.GetAll();
        var v1 = new SouthbankV1();
        if (!string.IsNullOrEmpty(_config.Southbank.v1DownloadUrl))
        {
            v1.DownloadUrl = _config.Southbank.v1DownloadUrl;
        }
        var v2 = new SouthbankV2();
        if (!string.IsNullOrEmpty(_config.Southbank.v2DownloadUrl))
        {
            v2.DownloadUrl = _config.Southbank.v2DownloadUrl;
        }
        var v3 = new SouthbankV3();
        if (!string.IsNullOrEmpty(_config.Southbank.v3DownloadUrl))
        {
            v3.DownloadUrl = _config.Southbank.v3DownloadUrl;
        }
        foreach (var app in apps)
        {
            if (app.IsPrivate || app.Type != ApplicationDetailType.Kachemak || app.IsHidden)
            {
                continue;
            }
            var img = await _appImageRepo.GetAllForApplication(app);
            var color = await _appColorRepo.GetAllForApplication(app);
            var v1_i = new SouthbankV1GameItem();
            var v2_i = new SouthbankV2GameItem();
            var v3_i = new SouthbankV3GameItem();
            v1_i.Name = app.DisplayName ?? app.Id;
            v2_i.Name = app.DisplayName ?? app.Id;
            v3_i.Name = app.DisplayName ?? app.Id;
            v1_i.VersionMethod = 0;
            v2_i.VersionMethod = 0;
            v3_i.VersionMethod = 0;
            v3_i.BaseAppId = app.AppVarData.Mod.BaseAppId;
            v3_i.RequireProton = app.AppVarData.Mod.RequireProton;
            v3_i.RequiredAppIds = app.AppVarData.Mod.RequiredAppIds;
            if (v3_i.BaseAppId == 0)
            {
                v3_i.BaseAppId = null;
            }
            v1_i.BelmontDetails = new();
            v2_i.BelmontDetails = new();
            
            if (img != null)
            {
                foreach (var x in img)
                {
                    var xn = await _storageService.GetUrl(x);
                    var xh = await _storageService.GetHash(x);
                    switch (x.Kind)
                    {
                        case ApplicationImageKind.Icon:
                            if (string.IsNullOrEmpty(v1_i.BelmontDetails.IconUrl))
                            {
                                v1_i.BelmontDetails.IconUrl = xn;
                            }
                            v2_i.BelmontDetails.IconUrl = [
                                xn,
                                xh
                            ];
                            break;
                        case ApplicationImageKind.Star:
                            if (string.IsNullOrEmpty(v1_i.BelmontDetails.StarUrl))
                            {
                                v1_i.BelmontDetails.StarUrl = xn;
                            }
                            v2_i.BelmontDetails.StarUrl = [
                                xn,
                                xh
                            ];
                            break;
                        case ApplicationImageKind.Wordmark:
                            if (string.IsNullOrEmpty(v1_i.BelmontDetails.WordmarkUrl))
                            {
                                v1_i.BelmontDetails.WordmarkUrl = xn;
                            }
                            v2_i.BelmontDetails.WordmarkUrl = [
                                xn,
                                xh
                            ];
                            break;
                        case ApplicationImageKind.Background:
                            if (string.IsNullOrEmpty(v1_i.BelmontDetails.BackgroundUrl))
                            {
                                v1_i.BelmontDetails.BackgroundUrl = xn;
                            }
                            v2_i.BelmontDetails.BackgroundUrl = [
                                xn,
                                xh
                            ];
                            break;
                    }
                }
            }
        
            if (color != null)
            {
                foreach (var c in color)
                {
                    switch (c.Kind)
                    {
                        case ApplicationColorKind.Dark:
                            v1_i.BelmontDetails.ColorDark = c.Value;
                            v2_i.BelmontDetails.ColorDark = c.Value;
                            break;
                        case ApplicationColorKind.Light:
                            v1_i.BelmontDetails.ColorLight = c.Value;
                            v2_i.BelmontDetails.ColorLight = c.Value;
                        break;
                        case ApplicationColorKind.Main:
                            v1_i.BelmontDetails.ColorMain = c.Value;
                            v2_i.BelmontDetails.ColorMain = c.Value;
                        break;
                        case ApplicationColorKind.Accent:
                            v1_i.BelmontDetails.ColorAccent = c.Value;
                            v2_i.BelmontDetails.ColorAccent = c.Value;
                        break;
                        case ApplicationColorKind.Secondary:
                            v1_i.BelmontDetails.ColorSecondary = c.Value;
                            v2_i.BelmontDetails.ColorSecondary = c.Value;
                        break;
                        case ApplicationColorKind.LightForeground:
                            v1_i.BelmontDetails.ColorLightForeground = c.Value;
                            v2_i.BelmontDetails.ColorLightForeground = c.Value;
                        break;
                        case ApplicationColorKind.Click:
                            v1_i.BelmontDetails.ColorClick = c.Value;
                            v2_i.BelmontDetails.ColorClick = c.Value;
                        break;
                        case ApplicationColorKind.ClickT:
                            v1_i.BelmontDetails.ColorClickT = c.Value;
                            v2_i.BelmontDetails.ColorClickT = c.Value;
                        break;
                    }
                }
            }

            v3_i.BelmontDetails = v2_i.BelmontDetails;

            var key = string.IsNullOrEmpty(app.AppVarData.Mod.SourceModName)
                ? app.Id
                : app.AppVarData.Mod.SourceModName;

            v1.Games[key] = v1_i;
            v2.Games[key] = v2_i;
            v3.Games[key] = v3_i;
        }

        var model = new SouthbankCacheModel();
        model.SetV1(v1);
        model.SetV2(v2);
        model.SetV3(v3);
        model.SetTimestamp();
        await _sbCacheRepo.InsertOrUpdate(model);
        _log.Debug($"Inserted record {model.Id} at {model.Timestamp}");
        return model;
    }

    public override Task InitializeAsync()
    {
        InitializeJobs();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialize <see cref="JobManager"/>
    /// </summary>
    private void InitializeJobs()
    {
        JobManager.AddJob(() =>
        {
            new Thread((ThreadStart) delegate
            {
                try
                {
                    GenerateSouthbankScheduleHandler().Wait();
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to run scheduled task {nameof(GenerateSouthbankScheduleHandler)}\n{ex}");
                    SentrySdk.CaptureException(ex, (scope) =>
                    {
                        scope.SetTag($"scheduledTaskName", nameof(GenerateSouthbankScheduleHandler));
                    });
                }
            }).Start();
        }, (schedule) =>
        {
            schedule.ToRunOnceAt(DateTime.Now.AddSeconds(5))
                .AndEvery(30).Minutes();
        });
    }

    /// <summary>
    /// Calls <see cref="GenerateSouthbank"/> while safely capturing the exception, and timing metrics for Sentry.
    /// </summary>
    /// <remarks>
    /// Invoked every 30min, and 5s after <see cref="InitializeJobs"/> is called.
    /// </remarks>
    private async Task GenerateSouthbankScheduleHandler()
    {
        var slug = $"{GetType().Name}-{nameof(GenerateSouthbank)}";
        _log.Debug($"{slug}|Running...");
        var checkInId = SentrySdk.CaptureCheckIn(slug, CheckInStatus.InProgress);
        try
        {
            await GenerateSouthbank();
            SentrySdk.CaptureCheckIn(slug, CheckInStatus.Ok, checkInId);
            _log.Debug($"{slug}|Job completed! ({checkInId})");
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to run {nameof(GenerateSouthbank)}\n{ex}");
            SentrySdk.CaptureException(ex);
            SentrySdk.CaptureCheckIn(slug, CheckInStatus.Error, checkInId);
        }
    }

    /// <summary>
    /// <para>Get the latest cached model.</para>
    /// 
    /// <para>Will call <see cref="GenerateSouthbank()"/> when <see cref="SouthbankCacheRepository.GetLatest"/> returns <see langword="null"/>.</para>
    /// </summary>
    public async Task<SouthbankCacheModel> GetLatest()
    {
        var model = await _sbCacheRepo.GetLatest();
        if (model == null)
        {
            _log.Debug($"Latest {nameof(SouthbankCacheModel)} doesn't exist. Generating a fresh one!");
            model = await GenerateSouthbank();
        }
        return model!;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using NLog;
using Sentry;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class SteamAPIService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public SteamAPIService(IServiceProvider services)
        : base(services)
    { }

    public override async Task InitializeAsync()
    {
        try
        {
            await GetAppsList();
        }
        catch (Exception ex)
        {
            _log.Error(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }

    public Dictionary<uint, SteamAppDetailItem> GetAppsListCache = [];

    public async Task<Dictionary<uint, SteamAppDetailItem>> GetAppsList()
    {
        var url = "https://api.steampowered.com/ISteamApps/GetAppList/v0002/";
        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var responseText = response.Content.ReadAsStringAsync().Result;
        if (response.IsSuccessStatusCode)
        {
            var deser = JsonSerializer.Deserialize<GetAppsListV2Response>(responseText, BaseService.SerializerOptions) ?? new();
            lock (GetAppsListCache)
            {
                GetAppsListCache = deser.AppList.Apps.DistinctBy(v => v.AppId).ToDictionary(v => v.AppId, v => v);
            }
        }
        else
        {
            _log.Error($"Failed to get apps list from Steam. Status Code: {response.StatusCode}\n{responseText}");
        }
        return GetAppsListCache;
    }

    public class GetAppsListV2Response
    {
        [JsonPropertyName("applist")]
        public SteamAppsListV2Response AppList { get; set; } = new();
    }
    public class SteamAppsListV2Response
    {
        [JsonPropertyName("apps")]
        public List<SteamAppDetailItem> Apps { get; set; } = [];
    }
    public class SteamAppDetailItem
    {
        [JsonPropertyName("appid")]
        public uint AppId { get; set; }
        [JsonPropertyName("name")]
        [JsonIgnore(Condition =JsonIgnoreCondition.WhenWritingDefault)]
        public string Name { get; set; } = "";
    }
}
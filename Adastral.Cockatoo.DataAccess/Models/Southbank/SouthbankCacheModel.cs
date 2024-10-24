using System.Text.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using kate.shared.Helpers;

namespace Adastral.Cockatoo.DataAccess.Models;

public class SouthbankCacheModel : BaseGuidModel
{
    public static string CollectionName => "southbank_cache";

    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Since Unix Epoch (Seconds, UTC)</para>
    /// </summary>
    public string Timestamp { get; set; } = "0";
    public long GetTimestamp()
    {
        return long.Parse(Timestamp);
    }
    public void SetTimestamp()
    {
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    /// <summary>
    /// Content of <see cref="SouthbankV1"/> as a JSON encoded as Base64.
    /// </summary>
    public string V1 { get; set; } = "";
    public SouthbankV1? GetV1()
    {
        var content = GeneralHelper.Base64Decode(V1);
        var data = JsonSerializer.Deserialize<SouthbankV1>(content, BaseService.SerializerOptions);
        return data;
    }
    public void SetV1(SouthbankV1 value)
    {
        var data = JsonSerializer.Serialize(value, BaseService.SerializerOptions);
        var content = GeneralHelper.Base64Encode(data);
        V1 = content;
    }

    /// <summary>
    /// Content of <see cref="SouthbankV2"/> as a JSON encoded as Base64.
    /// </summary>
    public string V2 { get; set; } = "";
    public SouthbankV2? GetV2()
    {
        var content = GeneralHelper.Base64Decode(V2);
        var data = JsonSerializer.Deserialize<SouthbankV2>(content, BaseService.SerializerOptions);
        return data;
    }
    public void SetV2(SouthbankV2 value)
    {
        var data = JsonSerializer.Serialize(value, BaseService.SerializerOptions);
        var content = GeneralHelper.Base64Encode(data);
        V2 = content;
    }

    /// <summary>
    /// Content of <see cref="SouthbankV3"/> as a JSON encoded as Base64.
    /// </summary>
    public string V3 { get; set; } = "";
    public SouthbankV3? GetV3()
    {
        var content = GeneralHelper.Base64Decode(V3);
        var data = JsonSerializer.Deserialize<SouthbankV3>(content, BaseService.SerializerOptions);
        return data;
    }
    public void SetV3(SouthbankV3 value)
    {
        var data = JsonSerializer.Serialize(value, BaseService.SerializerOptions);
        var content = GeneralHelper.Base64Encode(data);
        V3 = content;
    }

    public SouthbankCacheModel() : base()
    {
        SetTimestamp();
        SetV1(new());
        SetV2(new());
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models
{
    /// <summary>
    /// v3 of Southbank Schema.
    /// </summary>
    public class SouthbankV3
    {
        [JsonPropertyName("sb_ver")]
        public int Version { get; set; } = 3;
        [JsonPropertyName("dl_url")]
        public string DownloadUrl { get; set; } = "";
        [JsonPropertyName("games")]
        public Dictionary<string, SouthbankV3GameItem> Games { get; set; } = new();
    }
    /// <summary>
    /// Game item in <see cref="SouthbankV3.Games"/>
    /// </summary>
    /// <remarks>Partially extends <see cref="SouthbankV2GameItem"/></remarks>
    public class SouthbankV3GameItem
    {
        /// <summary>
        /// Display name for the game. If you wish to get the game "id", the use the key in <see cref="SouthbankV2.Games"/>
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        /// <summary>
        /// Type of the game. <c>0</c> for kachemak
        /// </summary>
        [JsonPropertyName("l1_type")]
        public int VersionMethod { get; set; } = 0;
        /// <summary>
        /// What AppID on Steam is this launched with? Only required for mods.
        /// </summary>
        [JsonPropertyName("base_app_id")]
        public uint? BaseAppId { get; set; }
        /// <summary>
        /// App IDs that are required for this game to run. Will prevent launch of an application if any of these aren't installed.
        /// </summary>
        [JsonPropertyName("required_app_ids")]
        public uint[]? RequiredAppIds { get; set; }
        /// <summary>
        /// Does this game require proton?
        /// </summary>
        [JsonPropertyName("require_proton")]
        public bool RequireProton { get; set; }
        /// <summary>
        /// Belmont styling details. Same as v2
        /// </summary>
        [JsonPropertyName("belmont")]
        public SouthbankV2BelmontDetails BelmontDetails { get; set; } = new();
    }
}

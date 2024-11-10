using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models
{
    public class ClientApiKeyModel : BaseGuidModel
    {
        public const string CollectionName = "clientApiKey";

        public ClientApiKeyModel()
            : base()
        {
            UserId = "00000000-0000-0000-0000-000000000000";
            ClientKeyProivderId = "00000000-0000-0000-0000-000000000000";
            Enabled = true;
            CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            Token = GenerateToken();
        }

        private static string GenerateToken()
        {
            var rand = new Random();
            var characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.=";
            return new string(Enumerable
                .Range(0, 32)
                .Select(num => characters[rand.Next() % characters.Length])
                .ToArray());
        }

        /// <summary>
        /// Foreign Key to <see cref="UserModel"/>
        /// </summary>
        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        public string UserId { get; set; }

        /// <summary>
        /// Foreign Key to <see cref="ClientApiKeyProviderModel"/>
        /// </summary>
        [DefaultValue("00000000-0000-0000-0000-000000000000")]
        public string ClientKeyProivderId { get; set; }

        /// <summary>
        /// When <see langword="false"/>, <see cref="DisableReason"/> must be set.
        /// </summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Token that was generated.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// When this document was created. Unix Timestamp (Seconds, UTC)
        /// </summary>
        public BsonTimestamp CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when this document was disabled. Unix Timestamp (Seconds, UTC)
        /// </summary>
        [BsonIgnoreIfNull]
        public BsonTimestamp? DisabledAt { get; set; }

        /// <summary>
        /// Why this Token was disabled. 
        /// </summary>
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.String)]
        public ClientAuthorizationTokenDisableReason? DisableReason { get; set; }

        /// <summary>
        /// Will be populated when <see cref="DisableReason"/> is set to <see cref="ClientAuthorizationTokenDisableReason.Other"/>
        /// </summary>
        [BsonIgnoreIfNull]
        [BsonIgnoreIfDefault]
        public string? DisableReasonOther { get; set; }

        /// <summary>
        /// User that disabled this token. When <see langword="null"/>, then assume that it is the user that this 
        /// was created for (<see cref="UserId"/>)
        /// </summary>
        /// <remarks>
        /// Foreign Key to <see cref="UserModel"/>
        /// </remarks>
        [BsonIgnoreIfNull]
        [BsonIgnoreIfDefault]
        public string? DisabledByUserId { get; set; }
    }
    public enum ClientAuthorizationTokenDisableReason
    {
        /// <summary>
        /// User requested to be logged out, so the token was destroyed.
        /// </summary>
        Logout,
        
        /// <summary>
        /// User did not authenticate with this token within the last month.
        /// </summary>
        Inactive,
        
        /// <summary>
        /// Account was manually disabled by a system administrator.
        /// </summary>
        AccountDisabled,

        /// <summary>
        /// Other reason, reason should be specified in <see cref="ClientApiKeyModel.DisableReasonOther"/>
        /// </summary>
        Other
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.DataAccess.Models
{
    public class ServiceAccountTokenModel : BaseGuidModel
    {
        public const string CollectionName = "serviceAccount_token";
        /// <summary>
        /// Generated Token
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// <see cref="ServiceAccountModel.UserId"/>
        /// </summary>
        public string ServiceAccountId { get; set; }
        /// <summary>
        /// When not <see langword="null"/>, this is the Id of the <see cref="UserSessionModel"/> that was used to create this token.
        /// </summary>
        [BsonIgnoreIfNull]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CreatedWithSessionId { get; set; }
        /// <summary>
        /// Session Id that is for this Token. Foreign Key to <see cref="UserSessionModel.Id"/>
        /// </summary>
        [BsonIgnoreIfNull]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AssociatedSessionId { get; set; }
        /// <summary>
        /// <para>When this Token was created (Unix Timestamp, Seconds, UTC)</para>
        /// </summary>
        [Required]
        [BsonRequired]
        [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
        public BsonTimestamp CreatedAtTimestamp { get; set; }

        /// <summary>
        /// <para>Timestamp when this token expires (Unix Timestamp, Seconds, UTC)</para>
        /// </summary>
        /// <remarks>
        /// Value will be <see langword="null"/> when the expiry check shouldn't be done (it never expires).
        /// </remarks>
        [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
        public BsonTimestamp? ExpiresAtTimestamp { get; set; }
        public static string GenerateToken()
        {
            var rand = new Random();
            var characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789><.#!~";
            return new string(Enumerable
                .Range(0, 32)
                .Select(num => characters[rand.Next() % characters.Length])
                .ToArray());
        }
        public ServiceAccountTokenModel()
            : base()
        {
            Token = GenerateToken();
            ServiceAccountId = "";
            CreatedAtTimestamp = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            ExpiresAtTimestamp = new BsonTimestamp(CreatedAtTimestamp.Value + 2678400);
        }
    }
}

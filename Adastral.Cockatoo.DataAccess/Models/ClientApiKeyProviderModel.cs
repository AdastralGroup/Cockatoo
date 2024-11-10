using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models
{
    public class ClientApiKeyProviderModel : BaseGuidModel
    {
        public const string CollectionName = "clientApiKeyProvider";

        public ClientApiKeyProviderModel()
            : base()
        {
            DisplayName = Id;
            CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            IsActive = false;
            PublicKeyXml = "";
            PrivateKeyXml = "";

            if (string.IsNullOrEmpty(PublicKeyXml) || string.IsNullOrEmpty(PrivateKeyXml))
            {
                GenerateKeyPair();
            }
        }

        private void GenerateKeyPair()
        {
            var data = RSA.Create(2048);
            var privateKey = data.ToXmlString(true);
            var publicKey = data.ToXmlString(false);
            if (string.IsNullOrEmpty(privateKey) || string.IsNullOrEmpty(publicKey))
            {
                if (string.IsNullOrEmpty(privateKey) && !string.IsNullOrEmpty(publicKey))
                {
                    throw new InvalidOperationException($"Unable to {nameof(GenerateKeyPair)} since the result of {typeof(RSA)}.{nameof(data.ToXmlString)}(true) returned a string that's null or empty");
                }
                else if (!string.IsNullOrEmpty(privateKey) && string.IsNullOrEmpty(publicKey))
                {
                    throw new InvalidOperationException($"Unable to {nameof(GenerateKeyPair)} since the result of {typeof(RSA)}.{nameof(data.ToXmlString)}(false) returned a string that's null or empty");

                }
                else if (string.IsNullOrEmpty(privateKey) && string.IsNullOrEmpty(publicKey))
                {
                    throw new InvalidOperationException($"Unable to {nameof(GenerateKeyPair)} since the result of {typeof(RSA)}.{nameof(data.ToXmlString)}(false) and {typeof(RSA)}.{nameof(data.ToXmlString)}(true) returned a string that's null or empty");
                }
            }
            PublicKeyXml = publicKey;
            PrivateKeyXml = privateKey;
        }

        /// <summary>
        /// Display Name for the Client Key Provider
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Timestamp when this document was created. Unix Timestamp (UTC, Seconds)
        /// </summary>
        public BsonTimestamp CreatedAt { get; set; }

        /// <summary>
        /// Foreign Key to <see cref="StorageFileModel"/>
        /// </summary>
        public string? BrandIconFileId { get; set; }

        /// <summary>
        /// Should any requests that were generated with this Client Key Provider be respected?
        /// Defaults to false, which means the auth key provided should be ignored.
        /// </summary>
        [DefaultValue(false)]
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// Result of <see cref="RSA.ToXmlString(bool)"/> where the bool parameter is false.
        /// </summary>
        /// <remarks>
        /// Used for encrypting user credentials when being passed through to the REST API, which will then generate a client authorization token.
        /// </remarks>
        public string PublicKeyXml { get; set; }

        /// <summary>
        /// Result of <see cref="RSA.ToXmlString(bool)"/> where the bool parameter is true.
        /// </summary>
        public string PrivateKeyXml { get; set; }
    }
}

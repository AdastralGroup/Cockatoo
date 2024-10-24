using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models
{
    public class ServiceAccountModel
    {
        public const string CollectionName = "serviceAccount";
        /// <summary>
        /// <see cref="UserModel.Id"/> that this is a service account for.
        /// </summary>
        [BsonId]
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.String)]
        public string UserId { get; set; } = "";
        /// <summary>
        /// <see cref="UserModel.Id"/> that owns this Service Account
        /// </summary>
        public string OwnerUserId { get; set; } = "";
    }
}

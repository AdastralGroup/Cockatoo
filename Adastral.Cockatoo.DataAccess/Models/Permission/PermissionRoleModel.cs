using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models
{
    public class PermissionRoleModel : BaseGuidModel
    {
        public const string CollectionName = "auth_permission_role";

        [BsonRepresentation(BsonType.String)]
        public PermissionKind Kind { get; set; } = PermissionKind.Login;
        /// <summary>
        /// Should the permission kind be allowed? When <see langword="false"/> it will not allow it.
        /// </summary>
        public bool Allow { get; set; }
        /// <summary>
        /// Foreign Key <see cref="PermissionGroupModel.Id"/>
        /// </summary>
        public string PermissionGroupId { get; set; } = "";
    }
}

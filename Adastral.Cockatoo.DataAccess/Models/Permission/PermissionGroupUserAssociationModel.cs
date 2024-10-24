using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models
{
    /// <summary>
    /// Model representing the link between a User and a Permission Group.
    /// </summary>
    public class PermissionGroupUserAssociationModel : BaseGuidModel
    {
        public const string CollectionName = "auth_permission_groupuserassociation";
        /// <summary>
        /// Foreign key to <see cref="UserModel.Id"/>
        /// </summary>
        public string UserId { get; set; } = "";
        /// <summary>
        /// Foreign key to <see cref="PermissionGroupModel.Id"/>
        /// </summary>
        public string PermissionGroupId { get; set; } = "";
    }
}

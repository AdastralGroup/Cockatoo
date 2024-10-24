using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adastral.Cockatoo.DataAccess.Models
{
    public class PermissionGroupModel : BaseGuidModel
    {
        public PermissionGroupModel()
            : base()
        {
            SetCreatedAtTimestamp();
        }
        public const string CollectionName = "auth_permission_group";
        /// <summary>
        /// Permission Group name
        /// </summary>
        public string Name { get; set; } = "New Permission Group";
        /// <summary>
        /// <para><see cref="long"/> formatted as a string.</para>
        /// 
        /// <para>Timestamp when this Permission Group was created (Seconds since UTC Epoch)</para>
        /// </summary>
        public string CreatedAtTimestamp { get; set; } = "0";
        /// <summary>
        /// Parse <see cref="CreatedAtTimestamp"/> to <see cref="long"/>
        /// </summary>
        public long GetCreatedAtTimestamp()
        {
            return long.Parse(CreatedAtTimestamp);
        }
        /// <summary>
        /// Set the value for <see cref="CreatedAtTimestamp"/>
        /// </summary>
        public void SetCreatedAtTimestamp()
        {
            CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }
        /// <summary>
        /// Ordering Priority. <see cref="uint.MaxValue"/> is the most important, and <see cref="uint.MinValue"/> is the least important.
        /// </summary>
        public uint Priority { get; set; }
    }
}

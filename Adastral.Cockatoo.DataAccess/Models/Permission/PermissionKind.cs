using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Adastral.Cockatoo.Common;

namespace Adastral.Cockatoo.DataAccess.Models
{
    public enum PermissionKind
    {
        /// <summary>
        /// Has permission to do everything and anything.
        /// </summary>
        [GlobalPermission]
        [Description("Has permission to do everything and anything.")]
        Superuser,

        /// <summary>
        /// Has read/write access to the Permission Admin routes.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Permission - Admin")]
        [Description("Has read/write access to the Permission Admin routes.")]
        PermissionAdmin,

        /// <summary>
        /// Full access to all Service Accounts
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Service Account - Admin")]
        [Description("Full access to all Service Accounts")]
        [PermissionInherit(ServiceAccountCreate)]
        ServiceAccountAdmin,

        /// <summary>
        /// Allow user to create service accounts
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Service Account - Create")]
        [Description("User can create service accounts")]
        ServiceAccountCreate,

        #region User
        /// <summary>
        /// Allow user to administrate/manage any user.
        /// </summary>
        /// <summary>
        /// Inherits
        /// <list type="bullet">
        /// <item><see cref="UserAdminViewAll"/></item>
        /// <item><see cref="UserAdminUpdateDetails"/></item>
        /// <item><see cref="UserAdminGetPermissions"/></item>
        /// <item><see cref="UserAdminAddToGroup"/></item>
        /// <item><see cref="UserAdminRecalculatePermissions"/></item>
        /// </list></summary>
        [GlobalPermission]
        [EnumDisplayName("User Management - Admin")]
        [Description("User can do anything related to managing/administrating users.")]
        [PermissionInherit(UserAdminViewAll)]
        [PermissionInherit(UserAdminUpdateDetails)]
        [PermissionInherit(UserAdminGetPermissions)]
        [PermissionInherit(UserAdminAddToGroup)]
        [PermissionInherit(UserAdminRecalculatePermissions)]
        UserAdmin,
        /// <summary>
        /// Allow user to directly get <see cref="UserModel"/> for all users.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("User Management - View")]
        [Description("User can view the details for any other user (display name, id, email, steam id, oauth id)")]
        UserAdminViewAll,
        /// <summary>
        /// Allow user to update the Display Name and Email for any uer.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("User Management - Update")]
        [Description("User can update the name/email of any user.")]
        UserAdminUpdateDetails,
        /// <summary>
        /// Allow user to see calculated/cached permissions for any user.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("User Management - View Permissions")]
        [Description("User can get all the permissions that were calculated for any user.")]
        UserAdminGetPermissions,
        /// <summary>
        /// Allow user to add any user to a group.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("User Management - Add to Group")]
        [Description("User can add user to any group.")]
        UserAdminAddToGroup,
        /// <summary>
        /// Allow user to use <see cref="Adastral.Cockatoo.DataAccess.Services.PermissionCacheService.CalculateForUser"/>.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("User Management - Recalculate Permissions")]
        [Description("User can recalculate permissions for all users.")]
        UserAdminRecalculatePermissions,
        #endregion

        #region Group
        /// <summary>
        /// Allow user to administrate all Groups.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Group Management - Admin")]
        [Description("User can do anything relating to Groups")]
        [PermissionInherit(GroupAdminViewAll)]
        [PermissionInherit(GroupAdminManagePermissions)]
        [PermissionInherit(GroupAdminCreate)]
        [PermissionInherit(GroupAdminDelete)]
        GroupAdmin,
        /// <summary>
        /// Allow user to view all Groups
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Group Management - View")]
        [Description("User can view all groups, and User Ids that are in those groups.")]
        GroupAdminViewAll,
        [GlobalPermission]
        [EnumDisplayName("Group Management - Manage Permissions")]
        [PermissionInherit(GroupAdminGrantPermission)]
        [PermissionInherit(GroupAdminDenyPermission)]
        [PermissionInherit(GroupAdminRevokePermission)]
        GroupAdminManagePermissions,
        /// <summary>
        /// Allow user to grant a permission on a group.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Group Management - Permission Grant")]
        [Description("User can grant permissions on any group.")]
        GroupAdminGrantPermission,
        /// <summary>
        /// Allow user to deny a permission on a group.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Group Management - Permission Deny")]
        [Description("User can deny permissions on any group.")]
        GroupAdminDenyPermission,
        /// <summary>
        /// Allow user to revoke a permission from a group.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Group Management - Permission Revoke")]
        [Description("User can revoke permissions on any group.")]
        GroupAdminRevokePermission,
        /// <summary>
        /// Allow user to create a group.
        /// </summary>
        [GlobalPermission]
        [EnumDisplayName("Group Management - Create")]
        [Description("User can create a group.")]
        [PermissionInherit(GroupAdminViewAll)]
        [PermissionInherit(GroupAdminManageRoles)]
        GroupAdminCreate,
        [GlobalPermission]
        [EnumDisplayName("Group Management - Manage Roles")]
        [Description("User can create, update, and delete roles.")]
        GroupAdminManageRoles,
        [GlobalPermission]
        [EnumDisplayName("Group Management - Delete")]
        [Description("User can delete a group.")]
        [PermissionInherit(GroupAdminViewAll)]
        GroupAdminDelete,
        #endregion

        #region Application
        [Description("Has full read/write access to Application Details")]
        [PermissionInherit(ApplicationDetailViewAll)]
        [PermissionInherit(ApplicationDetailCreate)]
        [PermissionInherit(ApplicationDetailEditDetails)]
        [PermissionInherit(ApplicationDetailEditAppearance)]
        [PermissionInherit(ApplicationDetailAUDNAdmin)]
        [PermissionInherit(ApplicationBlogPostViewAll)]
        [PermissionInherit(RefreshSouthbank)]
        [EnumDisplayName("Manage Applications - Admin")]
        ApplicationDetailAdmin,
        [EnumDisplayName("Manage Applications - Create")]
        ApplicationDetailCreate,
        [EnumDisplayName("Manage Applications - Delete")]
        ApplicationDetailDelete,
        [Description("Can view all Applications, no matter what their private state is.")]
        [EnumDisplayName("Application - View Override")]
        ApplicationDetailViewAll,
        [Description("Can edit the Details of an Application")]
        [EnumDisplayName("Application - Edit Details")]
        ApplicationDetailEditDetails,
        [Description("Can edit the Image, Colors, and any sort of Appearance customization for an Application")]
        [EnumDisplayName("Application - Edit Appearance")]
        ApplicationDetailEditAppearance,
        #endregion

        #region Application (AutoUpdater.NET)
        [PermissionInherit(ApplicationDetailViewAll)]
        [PermissionInherit(ApplicationDetailAUDNView)]
        [PermissionInherit(ApplicationDetailAUDNDeleteRevision)]
        [PermissionInherit(ApplicationDetailAUDNToggleRevisionEnableState)]
        [PermissionInherit(ApplicationDetailAUDNSubmitRevision)]
        [Description("User has full permissions to do anything relating to the AutoUpdater.NET features.")]
        [EnumDisplayName("AutoUpdater.NET - Admin")]
        ApplicationDetailAUDNAdmin,
        /// <summary>
        /// View all details relating to AutoUpdater.NET
        /// </summary>
        [Description("View details relating to AutoUpdater.NET Applications")]
        [EnumDisplayName("AutoUpdater.NET - View")]
        ApplicationDetailAUDNView,
        /// <summary>
        /// Allow the IsEnabled flag to be toggled on a Revision.
        /// </summary>
        [Description("Allow user to toggle the \"Enable\" state of a revision")]
        [EnumDisplayName("AutoUpdater.NET - Toggle Revision \"Enable\" state")]
        ApplicationDetailAUDNToggleRevisionEnableState,
        /// <summary>
        /// Allow deletion of revisions.
        /// </summary>
        [Description("Allow deletion of revisions.")]
        [EnumDisplayName("AutoUpdater.NET - Delete Revision")]
        ApplicationDetailAUDNDeleteRevision,
        [EnumDisplayName("AutoUpdater.NET - Create Revision")]
        [Description("Submit new revision for an Application that is for AutoUpdater.NET")]
        ApplicationDetailAUDNSubmitRevision,
        #endregion

        /// <summary>
        /// User can view all blog posts for an application, no matter what it's IsLive state is.
        /// </summary>
        [EnumDisplayName("Application Blog - View All")]
        [Description("User can view all blog posts for an application, no matter what it's IsLive state is.")]
        ApplicationBlogPostViewAll,
        [GlobalPermission]
        [Description("Can forcefully re-generate the Southbank Cache")]
        [EnumDisplayName("Refresh Southbank")]
        RefreshSouthbank,

        #region Blog
        /// <summary>
        /// Allow user to administrate any aspect of the Blog system
        /// </summary>
        [Description("User can administrate any aspect of the Blog system")]
        [PermissionInherit(BlogPostCreate)]
        [PermissionInherit(BlogPostDelete)]
        [PermissionInherit(BlogPostEditor)]
        [EnumDisplayName("Blog - Admin")]
        BlogPostAdmin,
        /// <summary>
        /// Allow user to modify any property on any blog post they can access.
        /// </summary>
        [Description("User can edit any property or thing on a Blog Post")]
        [PermissionInherit(BlogPostUpdateAuthors)]
        [PermissionInherit(BlogPostUpdateTags)]
        [EnumDisplayName("Blog Post - Editor")]
        BlogPostEditor,
        /// <summary>
        /// Allow user to create Blog Posts.
        /// </summary>
        [Description("User can create a blog post.")]
        [EnumDisplayName("Blog Post - Create")]
        BlogPostCreate,
        /// <summary>
        /// Allow user to delete Blog Post models from the Database.
        /// </summary>
        [Description("User can delete a blog post.")]
        [EnumDisplayName("Blog Post - Delete")]
        BlogPostDelete,
        /// <summary>
        /// Allow user to add or remove authors from a blog post.
        /// </summary>
        [Description("User can add or remove authors from a Blog Post")]
        [EnumDisplayName("Blog Post - Update Authors")]
        BlogPostUpdateAuthors,
        /// <summary>
        /// Allow user to add or remove tags from a blog post.
        /// </summary>
        [Description("User can add or remove tags from a Blog Post")]
        [EnumDisplayName("Blog Post - Update Tags")]
        BlogPostUpdateTags,
        #endregion

        #region File
        /// <summary>
        /// Allow user to administrate any aspect of the Storage File system
        /// </summary>
        [GlobalPermission]
        [Description("User can administrate any aspect of the Storage File system")]
        [PermissionInherit(FileUpload)]
        [PermissionInherit(FileDelete)]
        [EnumDisplayName("File Management - Admin")]
        FileAdmin,
        /// <summary>
        /// Allow user to upload files via the API. Is not required for uploading files used in the "Images" tab in the
        /// route <c>/ApplicationDetail/Edit/&lt;id&gt;</c>
        /// </summary>
        [GlobalPermission]
        [Description("Uploading Files via the API")]
        [EnumDisplayName("File Management - Upload")]
        FileUpload,
        /// <summary>
        /// Allow user to delete files from Database and from the Storage Provider.
        /// </summary>
        [GlobalPermission]
        [Description("Allow user to delete files from Database and from the Storage Provider.")]
        [EnumDisplayName("File Management - Delete")]
        FileDelete,
        #endregion

        #region Bullseye
        [PermissionInherit(BullseyeGenerateCache)]
        [PermissionInherit(BullseyeRegisterPatch)]
        [PermissionInherit(BullseyeRegisterRevision)]
        [PermissionInherit(BullseyeDeleteApp)]
        [PermissionInherit(BullseyeDeleteRevision)]
        [PermissionInherit(BullseyeDeletePatch)]
        [PermissionInherit(BullseyeUpdatePreviousRevision)]
        [PermissionInherit(BullseyeUpdateRevisionLiveState)]
        [PermissionInherit(BullseyeAppMarkLatestRevision)]
        [PermissionInherit(BullseyeManageRevision)]
        [PermissionInherit(BullseyeViewPrivateModels)]
        [EnumDisplayName("Bullseye Management - Admin")]
        BullseyeAdmin,
        /// <summary>
        /// Regenerate the Bullseye Cache for all apps.
        /// </summary>
        [Description("Re-generate the Bullseye Cache for an Application")]
        [EnumDisplayName("Bullseye Management - Generate Cache")]
        BullseyeGenerateCache,
        /// <summary>
        /// Create a patch for revisions.
        /// </summary>
        [Description("Create patches for Apps")]
        [EnumDisplayName("Bullseye Management - Register Patch")]
        BullseyeRegisterPatch,
        /// <summary>
        /// Create a revision for an app.
        /// </summary>
        [Description("Create revisions for an App")]
        [EnumDisplayName("Bullseye Management - Register Revision")]
        BullseyeRegisterRevision,
        [PermissionInherit(BullseyeUpdatePreviousRevision)]
        [PermissionInherit(BullseyeUpdateRevisionLiveState)]
        [PermissionInherit(BullseyeAppMarkLatestRevision)]
        BullseyeManageRevision,
        /// <summary>
        /// Allow user to delete Bullseye App, all of its patches and revisions. Will also try and delete the files
        /// used by the revisions and patches.
        /// </summary>
        [Description("Allow user to delete Bullseye App, all of its patches and revisions. Will also try and delete " +
                     "the files used by the revisions and patches.")]
        [PermissionInherit(BullseyeDeleteRevision)]
        [PermissionInherit(BullseyeDeletePatch)]
        [EnumDisplayName("Bullseye Management - Delete App")]
        BullseyeDeleteApp,
        /// <summary>
        /// Allow user to delete any revision, including any patches for that revision. Will also try and delete the
        /// files used by the revision and it's patches.
        /// </summary>
        [Description("Allow user to delete any revision, including any patches for that revision. Will also try and " +
                     "delete the files used by the revision and it's patches.")]
        [EnumDisplayName("Bullseye Management - Delete Revision")]
        BullseyeDeleteRevision,
        /// <summary>
        /// Allow user to delete any patch, including the files used by that patch.
        /// </summary>
        [Description("Allow user to delete any patch, including the files used by that patch.")]
        [EnumDisplayName("Bullseye Management - Delete Patch")]
        BullseyeDeletePatch,
        /// <summary>
        /// Allow user to set what the previous revision was for an existing revision.
        /// </summary>
        [Description("Allow user to set what the previous revision was for an existing revision.")]
        [EnumDisplayName("Bullseye Management - Update \"Previous Revision\"")]
        BullseyeUpdatePreviousRevision,
        /// <summary>
        /// Allow user to update the IsLive and PublishAt properties for a Revision.
        /// </summary>
        [Description("Allow user to update the IsLive and PublishAt properties for a Revision.")]
        [EnumDisplayName("Bullseye Management - Update \"IsLive\" State")]
        BullseyeUpdateRevisionLiveState,
        /// <summary>
        /// Allow user to mark a specific revision as the latest one.
        /// </summary>
        [Description("Allow user to mark a specific revision as the latest one.")]
        [EnumDisplayName("Bullseye Management - Mark Revision as Latest")]
        BullseyeAppMarkLatestRevision,
        /// <summary>
        /// User can view Bullseye v1/v2 models for non-public applications
        /// </summary>
        [Description("User can view Bullseye v1/v2 models for non-public applications")]
        [EnumDisplayName("Bullseye Application - View Private Apps")]
        BullseyeViewPrivateModels,
        #endregion

        /// <summary>
        /// Allow the user to login. If OAuth succeeds, it will pretend that they are not authenticated.
        /// </summary>
        [GlobalPermission]
        [Description("Allow the user to login. If OAuth succeeds, it will pretend that they are not authenticated.")]
        [EnumDisplayName("Login")]
        Login,
    }
    public static class PermissionKindExtensions
    {
        public static bool IsGlobal(this PermissionKind kind)
        {
            var member = typeof(PermissionKind).GetMember(kind.ToString())?.FirstOrDefault();
            if (member == null)
                return false;
            var attr = Attribute.GetCustomAttribute(member, typeof(GlobalPermissionAttribute));
            return attr != null;
        }
    }
}

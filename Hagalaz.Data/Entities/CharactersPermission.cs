using System;

namespace Hagalaz.Data.Entities
{
    [Obsolete("Use AspnetRoles instead")]
    public partial class CharactersPermission
    {
        public enum PermissionType
        {
            Default,
            SystemAdministrator,
            GameAdministrator,
            GameModerator,
            Donator
        }
        
        public uint MasterId { get; set; }
        public PermissionType? Permission { get; set; } = null!;

        public virtual Character Master { get; set; } = null!;
    }
}

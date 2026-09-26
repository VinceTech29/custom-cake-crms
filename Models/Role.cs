using System.Collections.Generic;

namespace CC.Models
{
    public class Role
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public List<SystemUser> Users { get; set; } = new List<SystemUser>();
    }
}

using CC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.domain.Entities
{
    public class Role
    {
        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public ICollection<SystemUser> Users { get; set; } = new List<SystemUser>();
    }
}

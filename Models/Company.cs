using System;
using System.Collections.Generic;

namespace CC.Models
{
    public class Company
    {
        public int CompanyId { get; set; }

        public string CompanyCode { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public List<SystemUser> Users { get; set; } = new List<SystemUser>();
    }
}

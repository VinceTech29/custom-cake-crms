using CC.Domain.Entities;
using System;

namespace CC.Domain.Entities
{
    public class Branch
    {
        public int BranchId { get; set; }

        public int CompanyId { get; set; }

        public string BranchName { get; set; } = string.Empty;

        public string BranchCode { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? ContactPhone { get; set; }

        public string? ContactEmail { get; set; }

        public string? ManagerName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public Company? Company { get; set; }
    }
}

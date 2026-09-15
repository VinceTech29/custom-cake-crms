using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class CustomerFollowUp
    {
        public int FollowUpId { get; set; }

        public int CustomerId { get; set; }

        public int StaffUserId { get; set; }

        public int StatusId { get; set; }

        public DateTime FollowUpDate { get; set; }

        public string Notes { get; set; } = string.Empty;

        public DateTime? NextFollowUpDate { get; set; }

        // Navigation
        public Customer? Customer { get; set; }

        public SystemUser? StaffUser { get; set; }

        public FollowUpStatus? Status { get; set; }
    }
}

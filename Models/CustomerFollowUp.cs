using System;

namespace CC.Models
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

        public Customer? Customer { get; set; }

        public SystemUser? StaffUser { get; set; }

        public FollowUpStatus? Status { get; set; }
    }
}

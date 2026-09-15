using System;

namespace CC.Domain.Entities
{
    public class CustomerInquiry
    {
        public int InquiryId { get; set; }

        public string InquiryCode { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public string CakeType { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string AssignedTo { get; set; } = "Unassigned";

        public decimal EstimatedBudget { get; set; }

        public string Status { get; set; } = "New";

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Customer? Customer { get; set; }
    }
}

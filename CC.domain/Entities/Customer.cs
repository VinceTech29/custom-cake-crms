using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public int CompanyId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public int? AddressId { get; set; }

        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;

        public int CreatedByUserId { get; set; }

        public string? AddressText { get; set; }

        public string? CakePreferences { get; set; }

        public string? Notes { get; set; }
        public bool IsUnsubscribed { get; set; } = false;

        // Navigation
        public Company? Company { get; set; }

        public Address? Address { get; set; }

        public SystemUser? CreatedByUser { get; set; }

        public ICollection<CustomerFollowUp> FollowUps { get; set; }
            = new List<CustomerFollowUp>();

        public ICollection<SalesOrder> Orders { get; set; }
            = new List<SalesOrder>();

        public ICollection<CustomerInquiry> Inquiries { get; set; }
            = new List<CustomerInquiry>();
    }
}

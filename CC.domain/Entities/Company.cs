using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class Company
    {
        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public int? AddressId { get; set; }

        public string ContactEmail { get; set; } = string.Empty;

        public string ContactPhone { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public Address? Address { get; set; }

        public ICollection<SystemUser> Users { get; set; } = new List<SystemUser>();

        public ICollection<Customer> Customers { get; set; } = new List<Customer>();

        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public string CompanyCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}

using System;
using System.Collections.Generic;

namespace CC.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public int CompanyId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string AddressText { get; set; } = string.Empty;

        public string CakePreferences { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime RegisteredDate { get; set; }

        public int CreatedByUserId { get; set; }

        public Company? Company { get; set; }

        public Address? Address { get; set; }

        public SystemUser? CreatedByUser { get; set; }

        public List<CustomerFollowUp> FollowUps { get; set; } = new List<CustomerFollowUp>();

        public List<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
    }
}

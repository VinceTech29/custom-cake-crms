using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class SystemUser
    {
        public int UserId { get; set; }

        public int CompanyId { get; set; }

        public int RoleId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        // Navigation
        public Company? Company { get; set; }

        public Role? Role { get; set; }

        public ICollection<Customer> Customers { get; set; }
            = new List<Customer>();

        public ICollection<CustomerFollowUp> FollowUps { get; set; }
            = new List<CustomerFollowUp>();

        public ICollection<SalesOrder> SalesOrders { get; set; }
            = new List<SalesOrder>();

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();

        public ICollection<TermsAcceptance> TermsAcceptances { get; set; }
            = new List<TermsAcceptance>();

        public ICollection<SystemAuditLog> AuditLogs { get; set; }
            = new List<SystemAuditLog>();

        public ICollection<ReportLog> Reports { get; set; }
            = new List<ReportLog>();
    }
}

using System;
using System.Collections.Generic;

namespace CC.Models
{
    public class SystemUser
    {
        public int UserId { get; set; }

        public int CompanyId { get; set; }

        public int RoleId { get; set; }

        public string Username { get; set; } = string.Empty;

        // PasswordHash intentionally omitted from WinForms models for safety
        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }

        public DateTime? LastLoginDate { get; set; }

        public Company? Company { get; set; }

        public Role? Role { get; set; }

        public List<Customer> Customers { get; set; } = new List<Customer>();

        public List<CustomerFollowUp> FollowUps { get; set; } = new List<CustomerFollowUp>();

        public List<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

        public List<Payment> Payments { get; set; } = new List<Payment>();

        public List<TermsAcceptance> TermsAcceptances { get; set; } = new List<TermsAcceptance>();

        public List<SystemAuditLog> AuditLogs { get; set; } = new List<SystemAuditLog>();

        public List<ReportLog> Reports { get; set; } = new List<ReportLog>();
    }
}

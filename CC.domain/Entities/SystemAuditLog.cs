using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class SystemAuditLog
    {
        public int LogId { get; set; }

        public int UserId { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public string ActionDescription { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? IPAddress { get; set; }

        // Navigation
        public SystemUser? User { get; set; }
    }
}

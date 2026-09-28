using System;

namespace CC.Models
{
    public class SystemAuditLog
    {
        public int LogId { get; set; }

        public int UserId { get; set; }

        public string ActionType { get; set; } = string.Empty;

        public string ActionDescription { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string? IPAddress { get; set; }

        public SystemUser? User { get; set; }
    }
}

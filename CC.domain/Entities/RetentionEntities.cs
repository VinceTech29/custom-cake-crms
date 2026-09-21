using System;

namespace CC.Domain.Entities
{
    public class RetentionEmailTemplate
    {
        public int TemplateId { get; set; }

        public string SegmentName { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string PreviewText { get; set; } = string.Empty;

        public string BodyText { get; set; } = string.Empty;

        public string OfferDescription { get; set; } = string.Empty;

        public decimal DiscountPercent { get; set; }

        public int ValidityDays { get; set; } = 14;

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = "System";
    }

    public class RetentionEmailLog
    {
        public int LogId { get; set; }

        public int CompanyId { get; set; }

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string SegmentName { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string BodySent { get; set; } = string.Empty;

        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public string SentBy { get; set; } = "System";

        public string Status { get; set; } = "Delivered"; // Delivered, Opened, Clicked, Bounced

        public DateTime? OpenedDate { get; set; }

        public DateTime? ClickedDate { get; set; }

        public int? ConvertedOrderId { get; set; }

        public decimal? ConvertedOrderAmount { get; set; }
    }

    public class RetentionSetting
    {
        public int SettingId { get; set; }

        public int CompanyId { get; set; }

        public int ActiveDaysThreshold { get; set; } = 90;

        public int AtRiskDaysThreshold { get; set; } = 180;

        public int LoyalMinOrders { get; set; } = 3;

        public int CooldownDays { get; set; } = 14;

        public bool AutoSendingEnabled { get; set; } = true;

        public DateTime? LastRecalculatedAt { get; set; }

        public string LastRecalculatedBy { get; set; } = "System";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string UpdatedBy { get; set; } = "System";

        // Email / SMTP Settings
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public string? SmtpFromEmail { get; set; }
        public string? SmtpFromName { get; set; } = "Sweet Story Cake Shop";
        public bool SmtpEnableSsl { get; set; } = true;
        public bool SmtpMockMode { get; set; } = true;
    }
}

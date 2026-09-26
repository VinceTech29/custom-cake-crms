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

    public class RetentionRequest
    {
        public int RequestId { get; set; }

        public int CompanyId { get; set; }

        // Customer
        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        // Retention Proposal Details
        public string TargetSegment { get; set; } = "At Risk";

        public string ActionType { get; set; } = "Special Discount";

        public decimal DiscountPercent { get; set; }

        public string RetentionDetails { get; set; } = string.Empty;

        // Reason for Retention (Requirement 3)
        public string ReasonCategory { get; set; } = string.Empty;

        public string? ReasonCustomDetails { get; set; }

        public string FullReason => string.IsNullOrWhiteSpace(ReasonCustomDetails)
            ? ReasonCategory
            : (ReasonCategory == "Other" ? ReasonCustomDetails : $"{ReasonCategory} - {ReasonCustomDetails}");

        // Status: "Pending", "Approved", "Rejected"
        public string Status { get; set; } = "Pending";

        // Request Details (Recorded by System)
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

        public int RequestedByUserId { get; set; }

        public string RequestedByUserName { get; set; } = string.Empty;

        // Review Details (Recorded by System)
        public DateTime? ReviewedDate { get; set; }

        public int? ReviewedByUserId { get; set; }

        public string? ReviewedByUserName { get; set; }

        public string? ReviewAction { get; set; }

        // Rejection Details
        public DateTime? RejectionDate { get; set; }

        public int? RejectedByUserId { get; set; }

        public string? RejectedByUserName { get; set; }

        public string? RejectionReason { get; set; }

        public string? AdminRemarks { get; set; }
    }
}

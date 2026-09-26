using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class ReportLog
    {
        public int ReportId { get; set; }

        public int GeneratedByUserId { get; set; }

        public string ReportType { get; set; } = string.Empty;

        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public SystemUser? GeneratedByUser { get; set; }

        public ICollection<ReportParameter> Parameters { get; set; }
            = new List<ReportParameter>();
    }
}

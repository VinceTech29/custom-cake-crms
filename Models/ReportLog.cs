using System;
using System.Collections.Generic;

namespace CC.Models
{
    public class ReportLog
    {
        public int ReportId { get; set; }

        public int GeneratedByUserId { get; set; }

        public string ReportType { get; set; } = string.Empty;

        public DateTime GeneratedDate { get; set; }

        public SystemUser? GeneratedByUser { get; set; }

        public List<ReportParameter> Parameters { get; set; } = new List<ReportParameter>();
    }
}

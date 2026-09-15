using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class ReportParameter
    {
        public int ReportParameterId { get; set; }

        public int ReportId { get; set; }

        public string ParamName { get; set; } = string.Empty;

        public string ParamValue { get; set; } = string.Empty;

        // Navigation
        public ReportLog? Report { get; set; }
    }
}

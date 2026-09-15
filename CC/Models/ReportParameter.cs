namespace CC.Models
{
    public class ReportParameter
    {
        public int ReportParameterId { get; set; }

        public int ReportId { get; set; }

        public string ParamName { get; set; } = string.Empty;

        public string ParamValue { get; set; } = string.Empty;

        public ReportLog? Report { get; set; }
    }
}

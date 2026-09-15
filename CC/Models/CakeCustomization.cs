namespace CC.Models
{
    public class CakeCustomization
    {
        public int CustomizationId { get; set; }

        public int OrderDetailId { get; set; }

        public string Flavor { get; set; } = string.Empty;

        public string Shape { get; set; } = string.Empty;

        public string ColorTheme { get; set; } = string.Empty;

        public string MessageOnCake { get; set; } = string.Empty;

        public string? ReferenceImageUrl { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
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

        // Navigation
        public SalesOrderDetail? OrderDetail { get; set; }
    }
}

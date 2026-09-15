using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class SalesOrderDetail
    {
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }

        public string ItemDescription { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        // Navigation
        public SalesOrder? Order { get; set; }

        public CakeCustomization? CakeCustomization { get; set; }
    }
}

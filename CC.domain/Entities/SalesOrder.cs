using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class SalesOrder
    {
        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        public int CreatedByUserId { get; set; }

        public int? DeliveryAddressId { get; set; }

        public int StatusId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveryDate { get; set; }

        public string CakeSize { get; set; } = string.Empty;

        public string Flavor { get; set; } = string.Empty;

        public string DesignTheme { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        // Navigation
        public Customer? Customer { get; set; }

        public SystemUser? CreatedByUser { get; set; }

        public Address? DeliveryAddress { get; set; }

        public OrderStatus? Status { get; set; }

        public ICollection<SalesOrderDetail> OrderDetails { get; set; }
            = new List<SalesOrderDetail>();

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}

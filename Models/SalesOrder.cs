using System;
using System.Collections.Generic;

namespace CC.Models
{
    public class SalesOrder
    {
        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        public int CreatedByUserId { get; set; }

        public int? DeliveryAddressId { get; set; }

        public int StatusId { get; set; }

        public string CakeSize { get; set; } = string.Empty;

        public string Flavor { get; set; } = string.Empty;

        public string DesignTheme { get; set; } = string.Empty;

        public string CustomMessage { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        public Customer? Customer { get; set; }

        public SystemUser? CreatedByUser { get; set; }

        public Address? DeliveryAddress { get; set; }

        public OrderStatus? Status { get; set; }

        public List<SalesOrderDetail> OrderDetails { get; set; } = new List<SalesOrderDetail>();

        public List<Payment> Payments { get; set; } = new List<Payment>();
    }
}

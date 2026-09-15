using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        public int MethodId { get; set; }

        public int StatusId { get; set; }

        public int ProcessedByUserId { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string TransactionReference { get; set; } = string.Empty;

        // Navigation
        public SalesOrder? Order { get; set; }

        public PaymentMethod? Method { get; set; }

        public PaymentStatus? Status { get; set; }

        public SystemUser? ProcessedByUser { get; set; }
    }
}

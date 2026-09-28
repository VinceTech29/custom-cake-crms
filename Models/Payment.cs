using System;

namespace CC.Models
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

        public SalesOrder? Order { get; set; }

        public PaymentMethod? Method { get; set; }

        public PaymentStatus? Status { get; set; }

        public SystemUser? ProcessedByUser { get; set; }
    }
}

using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class PaymentStatus
    {
        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        // Navigation
        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}

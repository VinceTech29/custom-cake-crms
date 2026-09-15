using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class PaymentMethod
    {
        public int MethodId { get; set; }

        public string MethodName { get; set; } = string.Empty;

        // Navigation
        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}

using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class SubscriptionPlan
    {
        public int PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int DurationDays { get; set; }

        public int MaxUsers { get; set; }

        public bool AllowBranching { get; set; }

        public int MaxBranches { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Subscription> Subscriptions { get; set; }
            = new List<Subscription>();
    }
}

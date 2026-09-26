using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class Subscription
    {
        public int SubscriptionId { get; set; }

        public int CompanyId { get; set; }

        public int PlanId { get; set; }

        public int StatusId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        // Navigation
        public Company? Company { get; set; }

        public SubscriptionPlan? Plan { get; set; }

        public SubscriptionStatus? Status { get; set; }
    }
}

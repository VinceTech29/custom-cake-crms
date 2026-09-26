using System;

namespace CC.Models
{
    public class Subscription
    {
        public int SubscriptionId { get; set; }

        public int CompanyId { get; set; }

        public int PlanId { get; set; }

        public int StatusId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public Company? Company { get; set; }

        public SubscriptionPlan? Plan { get; set; }

        public SubscriptionStatus? Status { get; set; }
    }
}

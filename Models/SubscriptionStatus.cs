using System.Collections.Generic;

namespace CC.Models
{
    public class SubscriptionStatus
    {
        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}

using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class SubscriptionStatus
    {
        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        // Navigation
        public ICollection<Subscription> Subscriptions { get; set; }
            = new List<Subscription>();
    }
}

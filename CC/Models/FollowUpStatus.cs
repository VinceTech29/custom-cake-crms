using System.Collections.Generic;

namespace CC.Models
{
    public class FollowUpStatus
    {
        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public List<CustomerFollowUp> FollowUps { get; set; } = new List<CustomerFollowUp>();
    }
}

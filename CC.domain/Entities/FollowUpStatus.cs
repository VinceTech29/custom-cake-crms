using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class FollowUpStatus
    {
        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public ICollection<CustomerFollowUp> FollowUps { get; set; }
            = new List<CustomerFollowUp>();
    }
}

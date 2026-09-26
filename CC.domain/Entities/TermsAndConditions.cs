using CC.domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class TermsAndConditions
    {
        public int TermsId { get; set; }

        public string Version { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime EffectiveDate { get; set; }

        public int CreatedByUserId { get; set; }

        // Navigation
        public SystemUser? CreatedByUser { get; set; }

        public ICollection<TermsAcceptance> Acceptances { get; set; }
            = new List<TermsAcceptance>();
    }
}

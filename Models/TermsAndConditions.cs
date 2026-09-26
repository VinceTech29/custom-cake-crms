using System;
using System.Collections.Generic;

namespace CC.Models
{
    public class TermsAndConditions
    {
        public int TermsId { get; set; }

        public string Version { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime EffectiveDate { get; set; }

        public int CreatedByUserId { get; set; }

        public SystemUser? CreatedByUser { get; set; }

        public List<TermsAcceptance> Acceptances { get; set; } = new List<TermsAcceptance>();
    }
}

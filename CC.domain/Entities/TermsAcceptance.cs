using System;
using System.Collections.Generic;
using System.Text;

namespace CC.Domain.Entities
{
    public class TermsAcceptance
    {
        public int AcceptanceId { get; set; }

        public int TermsId { get; set; }

        public int UserId { get; set; }

        public DateTime AcceptedDate { get; set; }

        // Navigation
        public TermsAndConditions? Terms { get; set; }

        public SystemUser? User { get; set; }
    }
}

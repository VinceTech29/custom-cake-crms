using System;

namespace CC.Models
{
    public class TermsAcceptance
    {
        public int AcceptanceId { get; set; }

        public int TermsId { get; set; }

        public int UserId { get; set; }

        public DateTime AcceptedDate { get; set; }

        public TermsAndConditions? Terms { get; set; }

        public SystemUser? User { get; set; }
    }
}

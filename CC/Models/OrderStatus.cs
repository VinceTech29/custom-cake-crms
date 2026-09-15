using System.Collections.Generic;

namespace CC.Models
{
    public class OrderStatus
    {
        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public List<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
    }
}

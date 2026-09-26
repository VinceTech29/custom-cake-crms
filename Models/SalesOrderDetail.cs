namespace CC.Models
{
    public class SalesOrderDetail
    {
        public int OrderDetailId { get; set; }

        public int OrderId { get; set; }

        public string ItemDescription { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public CakeCustomization? CakeCustomization { get; set; }
    }
}

namespace ArcheryAlley.Models
{
    public class PaymentViewModel
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public int Duration { get; set; }
        public string TargetDetails { get; set; }
        public string RangeDetails { get; set; }
        public decimal TotalPrice { get; set; }
        
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public int SlotId { get; set; }
        public string SelectedLanes { get; set; }
        public string SelectedLaneRanges { get; set; }
        public int NumberOfPax { get; set; }
        public string RateCode { get; set; }
        public decimal OriginalPrice { get; set; }  // pre-promo price, for display
        public string TargetSize { get; set; }
        public int TargetAmount { get; set; }
    }
}

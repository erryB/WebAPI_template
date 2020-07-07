namespace WebAPI.Model.Responses
{
    public class RequestDetailResponse
    {
        public long Qty { get; set; }
        public string ProductDisplayName { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductPriceCurrency { get; set; }
    }
}

namespace frontend.Shared 
{
    public class MarketPrice
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public decimal Change24h { get; set; }
    }
}

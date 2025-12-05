namespace frontend.Shared
{
    public class MarketOverviewResponse
    {
        public List<MarketInstrument> Crypto { get; set; } = new();
        public List<MarketInstrument> Stocks { get; set; } = new();
        public List<MarketInstrument> Indices { get; set; } = new();
    }

    public class MarketInstrument
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "crypto", "stock", "index"
        public decimal PriceUsd { get; set; }
        public decimal? Change24h { get; set; }
    }
}

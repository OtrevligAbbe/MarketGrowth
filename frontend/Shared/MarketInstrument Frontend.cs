namespace frontend.Shared;

public class MarketInstrument
{
    public string Category { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal PriceUsd { get; set; }
    public decimal Change24h { get; set; }
}

public class MarketOverviewResponse
{
    public List<MarketInstrument> Crypto { get; set; } = new();
    public List<MarketInstrument> Stocks { get; set; } = new();
    public List<MarketInstrument> Indices { get; set; } = new();
}

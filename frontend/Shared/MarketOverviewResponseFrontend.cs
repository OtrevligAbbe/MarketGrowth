namespace frontend.Shared;

public class MarketOverviewResponse
{
    public List<MarketInstrument> Crypto { get; set; } = new();
    public List<MarketInstrument> Stocks { get; set; } = new();
    public List<MarketInstrument> Indices { get; set; } = new();
}

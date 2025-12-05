namespace MarketGrowth.Api.Models;

public class MarketInstrument
{
    public string Symbol { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;  // "crypto" eller "stock"
    public decimal PriceUsd { get; set; }
    public decimal? Change24h { get; set; }
}

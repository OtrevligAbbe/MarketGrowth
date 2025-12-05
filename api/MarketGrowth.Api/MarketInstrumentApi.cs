namespace MarketGrowth.Api;

public class MarketInstrument
{
    // BTC, ETH, AAPL, SPY osv
    public string Symbol { get; set; } = string.Empty;

    // Bitcoin, Apple, S&P 500 osv
    public string Name { get; set; } = string.Empty;

    // "Crypto", "Stock", "Index"
    public string Category { get; set; } = string.Empty;

    // Pris i USD
    public decimal PriceUsd { get; set; }

    // 24h förändring i %
    public decimal Change24h { get; set; }
}
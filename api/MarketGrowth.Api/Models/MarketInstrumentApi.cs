using System.Collections.Generic;

namespace MarketGrowth.Api.Models
{
    public class MarketInstrument
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal PriceUsd { get; set; }
        public decimal Change24h { get; set; }
        public List<decimal> Sparkline7d { get; set; } = new();
    }
}

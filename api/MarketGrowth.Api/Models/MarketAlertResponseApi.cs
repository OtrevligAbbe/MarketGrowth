using System;

namespace MarketGrowth.Api.Models
{
    public class MarketAlertResponseApi
    {
        public string Symbol { get; set; } = default!;
        public string AssetType { get; set; } = default!;
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal ChangePercent { get; set; }
        public string Direction { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
    }
}

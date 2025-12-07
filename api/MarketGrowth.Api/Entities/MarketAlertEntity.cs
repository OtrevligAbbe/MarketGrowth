using System;

namespace MarketGrowth.Api.Entities
{
    public class MarketAlertEntity
    {
        public string id { get; set; } = Guid.NewGuid().ToString();

        public string Symbol { get; set; } = default!;
        public string AssetType { get; set; } = default!; 

        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal ChangePercent { get; set; } 

        public DateTime CreatedUtc { get; set; }
        public string Direction { get; set; } = default!; 
    }
}

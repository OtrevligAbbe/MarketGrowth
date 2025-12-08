using System;

namespace MarketGrowth.Api.Entities
{
    public class MarketSnapshotEntity
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string Symbol { get; set; } = default!;   
        public string AssetType { get; set; } = default!;
        public decimal Price { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}

using System;

namespace frontend.Shared  
{
    public class AlertFrontend
    {
        public string Symbol { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal ChangePercent { get; set; }
        public string Direction { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; }
    }
}

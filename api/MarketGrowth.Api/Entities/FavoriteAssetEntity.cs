using System;
using Newtonsoft.Json;

namespace MarketGrowth.Api.Entities
{
    public class FavoriteAssetEntity
    {
       
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("UserId")]
        public string UserId { get; set; }

        [JsonProperty("AssetId")]
        public string AssetId { get; set; }

        [JsonProperty("AssetType")]
        public string AssetType { get; set; }

        [JsonProperty("Symbol")]
        public string Symbol { get; set; }

        [JsonProperty("LastPrice")]
        public decimal LastPrice { get; set; }

        [JsonProperty("CreatedUtc")]
        public DateTime CreatedUtc { get; set; }
    }
}

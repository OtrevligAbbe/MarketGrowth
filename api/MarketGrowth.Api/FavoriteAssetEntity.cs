using System;
using System.Text.Json.Serialization;

namespace MarketGrowth.Api
{
    // Så här ser ett favorit-objekt ut inne i Cosmos DB
    public class FavoriteAssetEntity
    {
        // Cosmos "id"-fält (måste heta id)
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("assetId")]
        public string AssetId { get; set; } = string.Empty;

        [JsonPropertyName("assetType")]
        public string AssetType { get; set; } = string.Empty;

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("lastPrice")]
        public decimal LastPrice { get; set; }

        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}

namespace MarketGrowth.Frontend.Shared;

public class FavoriteAssetRequest
{
    public string UserId { get; set; } = string.Empty;

    public string AssetId { get; set; } = string.Empty;

    public string AssetType { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public decimal LastPrice { get; set; }
}

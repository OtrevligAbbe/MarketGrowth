namespace MarketGrowth.Frontend.Shared;

public class FavoriteAssetRequest
{
    // Id på användaren (t.ex. från B2C / auth)
    public string UserId { get; set; } = string.Empty;

    // Någon unik nyckel för tillgången (t.ex. symbol eller id)
    public string AssetId { get; set; } = string.Empty;

    // Typ av tillgång, t.ex. "stock" eller "crypto"
    public string AssetType { get; set; } = string.Empty;

    // Namn/symbol som visas för användaren
    public string Symbol { get; set; } = string.Empty;

    // Senaste priset när användaren sparade favorit (valfritt)
    public decimal LastPrice { get; set; }
}

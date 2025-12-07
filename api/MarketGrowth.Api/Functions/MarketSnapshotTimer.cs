using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using MarketGrowth.Api.Entities;
using MarketGrowth.Api.Repositories;

namespace MarketGrowth.Api.Functions
{
    public class MarketSnapshotTimer
    {
        private readonly ILogger _logger;
        private readonly IMarketSnapshotRepository _snapshotRepo;
        private readonly IMarketAlertRepository _alertRepo;
        private readonly IHttpClientFactory _httpClientFactory;

        public MarketSnapshotTimer(
            ILoggerFactory loggerFactory,
            IMarketSnapshotRepository snapshotRepo,
            IMarketAlertRepository alertRepo,
            IHttpClientFactory httpClientFactory)
        {
            _logger = loggerFactory.CreateLogger<MarketSnapshotTimer>();
            _snapshotRepo = snapshotRepo;
            _alertRepo = alertRepo;
            _httpClientFactory = httpClientFactory;
        }

        // kör var 5:e minut
        [Function("MarketSnapshotTimer")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
        {
            _logger.LogInformation($"MarketSnapshotTimer kördes: {DateTime.UtcNow}");

            var assets = new[]
            {
                new { Id = "bitcoin",  Symbol = "BTC" },
                new { Id = "ethereum", Symbol = "ETH" },
                new { Id = "solana",   Symbol = "SOL" },
                new { Id = "ripple",   Symbol = "XRP" },
                new { Id = "litecoin", Symbol = "LTC" }
            };

            var ids = string.Join(",", assets.Select(a => a.Id));
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.coingecko.com/api/v3/simple/price?ids={ids}&vs_currencies=usd";

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid anrop till CoinGecko");
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kunde inte hämta krypto-priser. Status: {StatusCode}", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var asset in assets)
            {
                if (!root.TryGetProperty(asset.Id, out var assetElement)) continue;
                if (!assetElement.TryGetProperty("usd", out var usdElement)) continue;

                var price = usdElement.GetDecimal();

                var newSnapshot = new MarketSnapshotEntity
                {
                    Symbol = asset.Symbol,
                    AssetType = "Crypto",
                    Price = price,
                    TimestampUtc = DateTime.UtcNow
                };

                // föregående snapshot
                var previous = await _snapshotRepo.GetLatestAsync(asset.Symbol);

                // spara nytt snapshot
                await _snapshotRepo.SaveAsync(newSnapshot);
                _logger.LogInformation("Sparade snapshot {Symbol}: {Price} USD", asset.Symbol, price);

                // ingen alert om vi inte har tidigare värde
                if (previous == null || previous.Price <= 0) continue;

                var changePercent = (newSnapshot.Price - previous.Price) / previous.Price * 100m;

                // tröskel
                if (Math.Abs(changePercent) < 0.01m) continue;

                var alert = new MarketAlertEntity
                {
                    Symbol = asset.Symbol,
                    AssetType = "Crypto",
                    OldPrice = previous.Price,
                    NewPrice = newSnapshot.Price,
                    ChangePercent = Math.Round(changePercent, 2),
                    CreatedUtc = DateTime.UtcNow,
                    Direction = changePercent >= 0 ? "Up" : "Down"
                };

                await _alertRepo.SaveAsync(alert);

                _logger.LogInformation(
                    "ALERT {Symbol}: {Direction} {ChangePercent}% ({Old} -> {New})",
                    alert.Symbol,
                    alert.Direction,
                    alert.ChangePercent,
                    alert.OldPrice,
                    alert.NewPrice);
            }
        }
    }
}

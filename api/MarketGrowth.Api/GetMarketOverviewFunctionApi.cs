using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MarketGrowth.Api
{
    public class GetMarketOverviewFunction
    {
        private readonly HttpClient _http;
        private readonly ILogger<GetMarketOverviewFunction> _logger;
        private readonly string _alphaKey;

        public GetMarketOverviewFunction(
            IHttpClientFactory httpClientFactory,
            ILogger<GetMarketOverviewFunction> logger)
        {
            _http = httpClientFactory.CreateClient();
            _logger = logger;

            _alphaKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY") ?? "";
        }

        // HUVUD-ENDPOINT: GET /api/market/overview
        [Function("GetMarketOverview")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "market/overview")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var result = new MarketOverviewResponse
                {
                    Crypto = await FetchCryptoAsync(),
                    Stocks = await FetchStocksAsync(),
                    Indices = await FetchIndicesAsync()
                };

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMarketOverview");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Internal server error.");
            }

            return response;
        }

        // KRYPTOVALUTOR (CoinGecko)
        private async Task<List<MarketInstrument>> FetchCryptoAsync()
        {
            var url =
                "https://api.coingecko.com/api/v3/simple/price" +
                "?ids=bitcoin,ethereum,solana,ripple,litecoin" +
                "&vs_currencies=usd&include_24hr_change=true";

            var json = await _http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var list = new List<MarketInstrument>();

            AddCoin(root, "bitcoin", "BTC", "Bitcoin", list);
            AddCoin(root, "ethereum", "ETH", "Ethereum", list);
            AddCoin(root, "solana", "SOL", "Solana", list);
            AddCoin(root, "ripple", "XRP", "Ripple", list);
            AddCoin(root, "litecoin", "LTC", "Litecoin", list);

            // Hämta 7-dagars sparkline per coin
            foreach (var asset in list)
            {
                var id = asset.Symbol.ToUpperInvariant() switch
                {
                    "BTC" => "bitcoin",
                    "ETH" => "ethereum",
                    "SOL" => "solana",
                    "XRP" => "ripple",
                    "LTC" => "litecoin",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(id))
                {
                    asset.Sparkline7d = await GetCryptoSparklineAsync(id);
                }
            }

            return list;
        }

        private static void AddCoin(
            JsonElement root,
            string cgId,
            string symbol,
            string name,
            List<MarketInstrument> target)
        {
            if (!root.TryGetProperty(cgId, out var el)) return;

            var price = el.GetProperty("usd").GetDecimal();
            var change = el.GetProperty("usd_24h_change").GetDecimal();

            target.Add(new MarketInstrument
            {
                Category = "Crypto",
                Symbol = symbol,
                Name = name,
                PriceUsd = price,
                Change24h = Math.Round(change, 2)
            });
        }

        // CoinGecko /market_chart svar
        private class CoinGeckoMarketChartResponse
        {
            [JsonPropertyName("prices")]
            public List<List<decimal>> Prices { get; set; } = new();
        }

        private async Task<List<decimal>> GetCryptoSparklineAsync(string coinId)
        {
            try
            {
                var url =
                    $"https://api.coingecko.com/api/v3/coins/{coinId}/market_chart?vs_currency=usd&days=7";

                var json = await _http.GetStringAsync(url);
                var data = JsonSerializer.Deserialize<CoinGeckoMarketChartResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Prices == null || data.Prices.Count == 0)
                    return new List<decimal>();

                // Plocka bara ut priset (index 1)
                var prices = data.Prices
                    .Where(p => p.Count >= 2)
                    .Select(p => p[1])
                    .ToList();

                return NormalizeToUnit(prices);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch sparkline for {CoinId}", coinId);
                return new List<decimal>();
            }
        }

        // AKTIER & INDEX (Alpha Vantage)
        private async Task<List<MarketInstrument>> FetchStocksAsync()
        {
            var symbols = new[]
            {
                ("AAPL", "Apple"),
                ("MSFT", "Microsoft"),
                ("NVDA", "NVIDIA"),
                ("TSLA", "Tesla"),
                ("ADBE", "Adobe")
            };

            var list = new List<MarketInstrument>();

            foreach (var (symbol, name) in symbols)
            {
                var m = await FetchAlphaGlobalQuoteAsync(symbol, name, "Stock");
                if (m != null) list.Add(m);
            }

            return list;
        }

        private async Task<List<MarketInstrument>> FetchIndicesAsync()
        {
            var symbols = new[]
            {
                ("SPY", "S&P 500"),
                ("QQQ", "Nasdaq 100"),
                ("DIA", "Dow Jones"),
                ("EWJ", "Japan (Nikkei)"),
                ("EEM", "Emerging Markets")
            };

            var list = new List<MarketInstrument>();

            foreach (var (symbol, name) in symbols)
            {
                var m = await FetchAlphaGlobalQuoteAsync(symbol, name, "Index");
                if (m != null) list.Add(m);
            }

            return list;
        }

        private async Task<MarketInstrument?> FetchAlphaGlobalQuoteAsync(
            string symbol,
            string name,
            string category)
        {
            if (string.IsNullOrWhiteSpace(_alphaKey))
            {
                _logger.LogWarning("ALPHAVANTAGE_API_KEY is not configured.");
                return null;
            }

            var url =
                $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_alphaKey}";

            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("Global Quote", out var quote))
            {
                _logger.LogWarning("No Global Quote for {Symbol}", symbol);
                return null;
            }

            var priceStr = quote.GetProperty("05. price").GetString();
            var changePercentStr = quote.GetProperty("10. change percent").GetString(); // t.ex. "1.23%"

            if (!decimal.TryParse(
                    priceStr,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var price))
            {
                _logger.LogWarning("Could not parse price for {Symbol}", symbol);
                return null;
            }

            changePercentStr = changePercentStr?.Trim().TrimEnd('%') ?? "0";

            if (!decimal.TryParse(
                    changePercentStr,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var changePercent))
            {
                changePercent = 0;
            }

            var instrument = new MarketInstrument
            {
                Category = category,
                Symbol = symbol,
                Name = name,
                PriceUsd = price,
                Change24h = changePercent
            };

            // Hämta 7-dagars sparkline för både aktier & index
            instrument.Sparkline7d = await GetAlphaSparklineAsync(symbol);

            return instrument;
        }

        private async Task<List<decimal>> GetAlphaSparklineAsync(string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_alphaKey))
                    return new List<decimal>();

                var url =
                    $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_alphaKey}";

                var json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("Time Series (Daily)", out var series))
                    return new List<decimal>();

                var prices = series
                    .EnumerateObject()
                    .OrderByDescending(p => p.Name) // "2025-12-06" osv
                    .Take(7)
                    .Select(p =>
                    {
                        var closeStr = p.Value.GetProperty("4. close").GetString();
                        return decimal.TryParse(
                            closeStr,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out var close)
                            ? close
                            : (decimal?)null;
                    })
                    .Where(p => p.HasValue)
                    .Select(p => p!.Value)
                    .Reverse() // äldst -> nyast för snygg graf
                    .ToList();

                return NormalizeToUnit(prices);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Alpha sparkline for {Symbol}", symbol);
                return new List<decimal>();
            }
        }

        // Hjälpmetod för normalisering

        private static List<decimal> NormalizeToUnit(List<decimal> prices)
        {
            if (prices == null || prices.Count == 0)
                return new List<decimal>();

            var min = prices.Min();
            var max = prices.Max();
            var range = max - min;
            if (range == 0) range = 1;

            return prices
                .Select(p => (p - min) / range)
                .ToList();
        }
    }
}

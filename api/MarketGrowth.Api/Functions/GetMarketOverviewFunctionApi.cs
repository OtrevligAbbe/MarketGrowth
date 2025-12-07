using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MarketGrowth.Api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MarketGrowth.Api.Functions
{
    public class GetMarketOverviewFunction
    {
        private readonly HttpClient _http;
        private readonly ILogger<GetMarketOverviewFunction> _logger;
        private readonly string _alphaKey;

        // Cache för aktier och index
        private static List<MarketInstrument> _cachedStocks = new();
        private static List<MarketInstrument> _cachedIndices = new();
        private static DateTime _stocksLastUpdated = DateTime.MinValue;
        private static DateTime _indicesLastUpdated = DateTime.MinValue;


        private static MarketOverviewResponse? _lastSnapshot;

        private static readonly object _cacheLock = new();

        private static readonly Random _random = new();

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
                    Crypto = new List<MarketInstrument>(),
                    Stocks = new List<MarketInstrument>(),
                    Indices = new List<MarketInstrument>()
                };

                // 1. Crypto (CoinGecko med riktig sparkline)
                try
                {
                    var crypto = await FetchCryptoAsync();
                    if (crypto.Count > 0)
                    {
                        result.Crypto = crypto;
                    }
                    else if (_lastSnapshot?.Crypto is { Count: > 0 })
                    {
                        // om vi får tom lista men har gamla data → använd gamla
                        result.Crypto = _lastSnapshot.Crypto;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch crypto data, using snapshot cache if available.");
                    if (_lastSnapshot?.Crypto is { Count: > 0 })
                    {
                        result.Crypto = _lastSnapshot.Crypto;
                    }
                }

                // 2. Stocks (Alpha Vantage + intern cache + fejk-sparkline)
                try
                {
                    var stocks = await FetchStocksAsync();
                    if (stocks.Count > 0)
                    {
                        result.Stocks = stocks;
                    }
                    else if (_lastSnapshot?.Stocks is { Count: > 0 })
                    {
                        result.Stocks = _lastSnapshot.Stocks;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch stocks, using snapshot cache if available.");
                    if (_lastSnapshot?.Stocks is { Count: > 0 })
                    {
                        result.Stocks = _lastSnapshot.Stocks;
                    }
                }

                // 3. Indices (Alpha Vantage + intern cache + fejk-sparkline)
                try
                {
                    var indices = await FetchIndicesAsync();
                    if (indices.Count > 0)
                    {
                        result.Indices = indices;
                    }
                    else if (_lastSnapshot?.Indices is { Count: > 0 })
                    {
                        result.Indices = _lastSnapshot.Indices;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch indices, using snapshot cache if available.");
                    if (_lastSnapshot?.Indices is { Count: > 0 })
                    {
                        result.Indices = _lastSnapshot.Indices;
                    }
                }

                var hasAnyData =
                    (result.Crypto?.Count ?? 0) > 0 ||
                    (result.Stocks?.Count ?? 0) > 0 ||
                    (result.Indices?.Count ?? 0) > 0;

                if (hasAnyData)
                {
                    // Uppdatera snapshot med allt vi lyckades få fram
                    lock (_cacheLock)
                    {
                        _lastSnapshot = result;
                    }

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteAsJsonAsync(result);
                    return response;
                }

                // Ingen ny data, men tidigare snapshot finns
                if (_lastSnapshot is not null)
                {
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteAsJsonAsync(_lastSnapshot);
                    return response;
                }

                // Absolut ingen data någonsin
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("No market data available.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in GetMarketOverview");

                // Vid total krasch försök ändå skicka snapshot om vi har en
                if (_lastSnapshot is not null)
                {
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteAsJsonAsync(_lastSnapshot);
                    return response;
                }

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Internal server error.");
                return response;
            }
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

            // Hämta 7-dagars sparkline per coin från CoinGecko
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
                    var spark = await GetCryptoSparklineAsync(id);

                    
                    if (spark != null && spark.Count > 0)
                    {
                        asset.Sparkline7d = spark;
                    }
                    else
                    {
                        asset.Sparkline7d = GenerateRandomSparkline();
                        _logger.LogWarning("Using fallback sparkline for {Symbol}", asset.Symbol);
                    }
                }
                else
                {
                    
                    asset.Sparkline7d = GenerateRandomSparkline();
                    _logger.LogWarning("No CoinGecko id found for {Symbol}, using fallback sparkline.", asset.Symbol);
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

                var prices = data.Prices
                    .Where(p => p.Count >= 2)
                    .Select(p => p[1])
                    .ToList();

                return NormalizeToUnit(prices);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch crypto sparkline for {CoinId}", coinId);
                return new List<decimal>();
            }
        }

        //AKTIER & INDEX (Alpha Vantage)

        private async Task<List<MarketInstrument>> FetchStocksAsync()
        {
            lock (_cacheLock)
            {
                if (DateTime.UtcNow - _stocksLastUpdated < TimeSpan.FromMinutes(1) &&
                    _cachedStocks.Count > 0)
                {
                    return _cachedStocks;
                }
            }

            var symbols = new[]
            {
                ("AAPL", "Apple"),
                ("MSFT", "Microsoft"),
                ("NVDA", "NVIDIA"),
                ("TSLA", "Tesla"),
                ("ADBE", "Adobe")
            };

            var list = new List<MarketInstrument>();

            try
            {
                foreach (var (symbol, name) in symbols)
                {
                    var m = await FetchAlphaGlobalQuoteAsync(symbol, name, "Stock");
                    if (m != null) list.Add(m);
                }

                lock (_cacheLock)
                {
                    _cachedStocks = list;
                    _stocksLastUpdated = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch stocks, falling back to stocks cache.");
                lock (_cacheLock)
                {
                    return _cachedStocks;
                }
            }

            return list;
        }

        private async Task<List<MarketInstrument>> FetchIndicesAsync()
        {
            lock (_cacheLock)
            {
                if (DateTime.UtcNow - _indicesLastUpdated < TimeSpan.FromMinutes(1) &&
                    _cachedIndices.Count > 0)
                {
                    return _cachedIndices;
                }
            }

            var symbols = new[]
            {
                ("SPY", "S&P 500"),
                ("QQQ", "Nasdaq 100"),
                ("DIA", "Dow Jones"),
                ("EWJ", "Japan (Nikkei)"),
                ("EEM", "Emerging Markets")
            };

            var list = new List<MarketInstrument>();

            try
            {
                foreach (var (symbol, name) in symbols)
                {
                    var m = await FetchAlphaGlobalQuoteAsync(symbol, name, "Index");
                    if (m != null) list.Add(m);
                }

                lock (_cacheLock)
                {
                    _cachedIndices = list;
                    _indicesLastUpdated = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch indices, falling back to indices cache.");
                lock (_cacheLock)
                {
                    return _cachedIndices;
                }
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

           
            if (doc.RootElement.TryGetProperty("Note", out var noteEl))
            {
                _logger.LogWarning("Alpha Vantage note for {Symbol}: {Note}", symbol, noteEl.GetString());
                return null;
            }

            if (!doc.RootElement.TryGetProperty("Global Quote", out var quote))
            {
                _logger.LogWarning("No Global Quote for {Symbol}", symbol);
                return null;
            }

            var priceStr = quote.GetProperty("05. price").GetString();
            var changePercentStr = quote.GetProperty("10. change percent").GetString();

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
                Change24h = changePercent,
                Sparkline7d = GenerateRandomSparkline()
            };

            return instrument;
        }

        //Hjälpmetoder

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

        private static List<decimal> GenerateRandomSparkline()
        {
            var result = new List<decimal>();
            decimal current = 0.5m;

            for (int i = 0; i < 20; i++)
            {
                var step = (decimal)(_random.NextDouble() - 0.5) * 0.2m; // små hopp upp/ner
                current += step;
                if (current < 0m) current = 0m;
                if (current > 1m) current = 1m;
                result.Add(current);
            }

            return result;
        }
    }
}

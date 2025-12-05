using MarketGrowth.Api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace MarketGrowth.Api;

public class GetMarketOverviewFunction
{
    private readonly HttpClient _http;
    private readonly ILogger<GetMarketOverviewFunction> _logger;
    private readonly JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _alphaKey;

    public GetMarketOverviewFunction(IHttpClientFactory httpClientFactory,
                                     ILogger<GetMarketOverviewFunction> logger)
    {
        _http = httpClientFactory.CreateClient();
        _logger = logger;
        _alphaKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY") ?? "";
    }

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

    // ---------- 5 krypto från CoinGecko (återanvänd din befintliga kod) ----------
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

        return list;
    }

    private static void AddCoin(JsonElement root, string cgId, string symbol, string name,
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
            Change24h = change
        });
    }

    // ---------- 5 aktier via Alpha Vantage GLOBAL_QUOTE ----------
    private async Task<List<MarketInstrument>> FetchStocksAsync()
    {
        var symbols = new[]
        {
            ("AAPL","Apple"),
            ("MSFT","Microsoft"),
            ("NVDA","NVIDIA"),
            ("TSLA","Tesla"),
            ("ADBE","Adobe")
        };

        var list = new List<MarketInstrument>();

        foreach (var (symbol, name) in symbols)
        {
            var m = await FetchAlphaGlobalQuoteAsync(symbol, name, "Stock");
            if (m != null) list.Add(m);
        }

        return list;
    }

    // ---------- 5 index (eller index-ETF:er) ----------
    private async Task<List<MarketInstrument>> FetchIndicesAsync()
    {
        // Exempel: ETF:er som speglar index (enkelt och funkar bra i Alpha Vantage)
        var symbols = new[]
        {
            ("SPY","S&P 500"),
            ("QQQ","Nasdaq 100"),
            ("DIA","Dow Jones"),
            ("EWJ","Japan (Nikkei)"),
            ("EEM","Emerging Markets")
        };

        var list = new List<MarketInstrument>();

        foreach (var (symbol, name) in symbols)
        {
            var m = await FetchAlphaGlobalQuoteAsync(symbol, name, "Index");
            if (m != null) list.Add(m);
        }

        return list;
    }

    // ---------- Hjälpmetod för Alpha Vantage GLOBAL_QUOTE ----------
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
        var changePercentStr = quote.GetProperty("10. change percent").GetString(); // "1.23%"

        if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any,
                              System.Globalization.CultureInfo.InvariantCulture,
                              out var price))
            return null;

        changePercentStr = changePercentStr?.Trim().TrimEnd('%') ?? "0";

        if (!decimal.TryParse(changePercentStr, System.Globalization.NumberStyles.Any,
                              System.Globalization.CultureInfo.InvariantCulture,
                              out var changePercent))
            changePercent = 0;

        return new MarketInstrument
        {
            Category = category,
            Symbol = symbol,
            Name = name,
            PriceUsd = price,
            Change24h = changePercent
        };
    }
}

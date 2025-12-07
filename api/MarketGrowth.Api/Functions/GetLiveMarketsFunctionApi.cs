using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MarketGrowth.Api.Functions
{
    public class GetLiveMarketsFunction
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GetLiveMarketsFunction> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public GetLiveMarketsFunction(IHttpClientFactory httpClientFactory,
                                      ILogger<GetLiveMarketsFunction> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        // URL blir: /api/market/crypto
        [Function("GetLiveMarkets")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "market/crypto")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // 1. Bygg CoinGecko-URL
                var url =
                    "https://api.coingecko.com/api/v3/simple/price" +
                    "?ids=bitcoin,ethereum,solana,ripple,litecoin" +
                    "&vs_currencies=usd" +
                    "&include_24hr_change=true";

                // 2. Anropa CoinGecko
                var cgResponse = await _httpClient.GetAsync(url);

                if (!cgResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("CoinGecko returned {StatusCode}", cgResponse.StatusCode);
                    response.StatusCode = HttpStatusCode.BadGateway;
                    await response.WriteStringAsync("Failed to fetch data from CoinGecko.");
                    return response;
                }

                var json = await cgResponse.Content.ReadAsStringAsync();

                // 3. Deserialisera svaret
                var rawData = JsonSerializer.Deserialize<Dictionary<string, CoinGeckoPrice>>(json, JsonOptions);
                if (rawData is null)
                {
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    await response.WriteStringAsync("Failed to parse CoinGecko response.");
                    return response;
                }

                // 4. Mappa om till snyggare shape (BTC, ETH, …)
                var result = new Dictionary<string, CoinMarketDto>();

                void Add(string id, string symbol)
                {
                    if (rawData.TryGetValue(id, out var info))
                    {
                        result[symbol] = new CoinMarketDto
                        {
                            Symbol = symbol,
                            PriceUsd = info.Usd,
                            Change24h = info.Usd24hChange
                        };
                    }
                }

                Add("bitcoin", "BTC");
                Add("ethereum", "ETH");
                Add("solana", "SOL");
                Add("ripple", "XRP");
                Add("litecoin", "LTC");

                // 5. Skicka tillbaka som JSON
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching data from CoinGecko");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Internal server error.");
                return response;
            }
        }

        // Hur CoinGecko ser ut
        private class CoinGeckoPrice
        {
            [JsonPropertyName("usd")]
            public decimal Usd { get; set; }

            [JsonPropertyName("usd_24h_change")]
            public decimal Usd24hChange { get; set; }
        }

        // Vad vi skickar till frontend
        public class CoinMarketDto
        {
            public string Symbol { get; set; } = string.Empty;
            public decimal PriceUsd { get; set; }
            public decimal Change24h { get; set; }
        }
    }
}

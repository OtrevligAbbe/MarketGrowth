using System.Net;
using System.Net.Http.Json;
using MarketGrowth.Api.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MarketGrowth.Api;

public class GetLiveMarketsFunction
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetLiveMarketsFunction> _logger;

    public GetLiveMarketsFunction(IHttpClientFactory httpClientFactory,
                                  ILogger<GetLiveMarketsFunction> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    // Hjälpmodell för CoinGecko-svaret
    private class CoinGeckoPrice
    {
        public decimal usd { get; set; }
        public decimal? usd_24h_change { get; set; }
    }

    [Function("GetLiveMarkets")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "markets/live")]
        HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);

        try
        {
            // 1. Hämta 5 krypton från CoinGecko
            // (du kan byta vilka du vill)
            var cryptoIds = "bitcoin,ethereum,solana,ripple,cardano";

            var url =
                $"https://api.coingecko.com/api/v3/simple/price?" +
                $"ids={cryptoIds}&vs_currencies=usd&include_24hr_change=true";

            var cgData =
                await _httpClient.GetFromJsonAsync<Dictionary<string, CoinGeckoPrice>>(url);

            var list = new List<MarketInstrument>();

            if (cgData != null)
            {
                void AddCrypto(string id, string symbol, string name)
                {
                    if (!cgData.TryGetValue(id, out var p)) return;

                    list.Add(new MarketInstrument
                    {
                        Symbol = symbol,
                        Name = name,
                        Type = "crypto",
                        PriceUsd = p.usd,
                        Change24h = p.usd_24h_change
                    });
                }

                AddCrypto("bitcoin", "BTC", "Bitcoin");
                AddCrypto("ethereum", "ETH", "Ethereum");
                AddCrypto("solana", "SOL", "Solana");
                AddCrypto("ripple", "XRP", "XRP");
                AddCrypto("cardano", "ADA", "Cardano");
            }

            // 2. TODO: Hämta 5 aktier från ett gratis aktie-API
            // Här lägger du sen in motsvarande kod för aktier.
            // Lägg till dem i samma "list" men med Type = "stock".

            await response.WriteAsJsonAsync(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching live market data");

            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Error while fetching market data");
        }

        return response;
    }
}

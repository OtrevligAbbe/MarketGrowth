using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace MarketGrowth.Api
{
    public class GetMarketDataFunction
    {
        private readonly ILogger<GetMarketDataFunction> _logger;
        private readonly HttpClient _httpClient;

        // Konstruktor: Tar emot de injicerade tjänsterna (Logger och HttpClient)
        public GetMarketDataFunction(ILoggerFactory loggerFactory, HttpClient httpClient)
        {
            _logger = loggerFactory.CreateLogger<GetMarketDataFunction>();
            _httpClient = httpClient;
        }

        [Function("GetMarketData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "market/crypto")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                // 1. Anropa CoinGecko API (Din PoC)
                var coinGeckoUrl = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=usd";

                var response = await _httpClient.GetAsync(coinGeckoUrl);

                // 2. Kontrollera svar och hantera fel
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    // Skapa HTTP-svaret
                    var httpResponse = req.CreateResponse(HttpStatusCode.OK);
                    httpResponse.Headers.Add("Content-Type", "application/json");
                    await httpResponse.WriteStringAsync(jsonContent);

                    return httpResponse;
                }

                // Hantera API-fel
                _logger.LogError($"CoinGecko API returned status: {response.StatusCode}");
                var errorResponse = req.CreateResponse(response.StatusCode);
                await errorResponse.WriteStringAsync($"Error fetching data from CoinGecko.");
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ett oväntat fel uppstod under API-anropet.");
                var httpResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await httpResponse.WriteStringAsync("Internal Server Error.");
                return httpResponse;
            }
        }
    }
}
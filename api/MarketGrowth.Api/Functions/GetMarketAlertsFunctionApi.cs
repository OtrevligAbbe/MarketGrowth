using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MarketGrowth.Api.Models;
using MarketGrowth.Api.Repositories;

namespace MarketGrowth.Api.Functions
{
    public class GetMarketAlertsFunctionApi
    {
        private readonly ILogger _logger;
        private readonly IMarketAlertRepository _alertRepo;

        public GetMarketAlertsFunctionApi(
            ILoggerFactory loggerFactory,
            IMarketAlertRepository alertRepo)
        {
            _logger = loggerFactory.CreateLogger<GetMarketAlertsFunctionApi>();
            _alertRepo = alertRepo;
        }

        [Function("GetMarketAlerts")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alerts")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // hämta senaste 50 alerts
                var alerts = await _alertRepo.GetLatestAsync(50);

                var result = alerts
                    .Select(a => new MarketAlertResponseApi
                    {
                        Symbol = a.Symbol,
                        AssetType = a.AssetType,
                        OldPrice = a.OldPrice,
                        NewPrice = a.NewPrice,
                        ChangePercent = a.ChangePercent,
                        Direction = a.Direction,
                        CreatedUtc = a.CreatedUtc
                    })
                    .ToList();

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetMarketAlerts");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Error in GetMarketAlerts.");
            }

            return response;
        }
    }
}

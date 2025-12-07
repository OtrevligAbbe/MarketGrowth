using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using MarketGrowth.Api.Models;
using MarketGrowth.Api.Repositories;

namespace MarketGrowth.Api.Functions
{
    public class GetMarketAlertsFunctionApi
    {
        private readonly IMarketAlertRepository _alertRepo;

        public GetMarketAlertsFunctionApi(IMarketAlertRepository alertRepo)
        {
            _alertRepo = alertRepo;
        }

        [Function("GetMarketAlerts")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "alerts")]
            HttpRequestData req)
        {
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using MarketGrowth.Api.Entities;

namespace MarketGrowth.Api.Repositories
{
    public interface IMarketAlertRepository
    {
        Task SaveAsync(MarketAlertEntity alert);
        Task<IReadOnlyList<MarketAlertEntity>> GetLatestAsync(int maxCount);
    }

    public class MarketAlertRepository : IMarketAlertRepository
    {
        private readonly Container _container;

        public MarketAlertRepository(Container container)
        {
            _container = container;
        }

        public Task SaveAsync(MarketAlertEntity alert)
        {
            return _container.CreateItemAsync(alert, new PartitionKey(alert.Symbol));
        }

        public async Task<IReadOnlyList<MarketAlertEntity>> GetLatestAsync(int maxCount)
        {
            var results = new List<MarketAlertEntity>();

            
            var query = new QueryDefinition(
                "SELECT * FROM c ORDER BY c.CreatedUtc DESC");

            using var iterator = _container.GetItemQueryIterator<MarketAlertEntity>(query);

            while (iterator.HasMoreResults && results.Count < maxCount)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results
                .OrderByDescending(a => a.CreatedUtc)
                .Take(maxCount)
                .ToList();
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using MarketGrowth.Api.Entities;

namespace MarketGrowth.Api.Repositories
{
    public interface IMarketSnapshotRepository
    {
        Task SaveAsync(MarketSnapshotEntity snapshot);
        Task<MarketSnapshotEntity?> GetLatestAsync(string symbol);
    }

    public class MarketSnapshotRepository : IMarketSnapshotRepository
    {
        private readonly Container _container;

        public MarketSnapshotRepository(Container container)
        {
            _container = container;
        }

        public Task SaveAsync(MarketSnapshotEntity snapshot)
        {
            return _container.CreateItemAsync(snapshot, new PartitionKey(snapshot.Symbol));
        }

        public async Task<MarketSnapshotEntity?> GetLatestAsync(string symbol)
        {
            var query = new QueryDefinition(
                "SELECT TOP 1 * FROM c WHERE c.Symbol = @symbol ORDER BY c.TimestampUtc DESC")
                .WithParameter("@symbol", symbol);

            using var iterator = _container.GetItemQueryIterator<MarketSnapshotEntity>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(symbol)
                });

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.Resource.FirstOrDefault();
            }

            return null;
        }
    }
}

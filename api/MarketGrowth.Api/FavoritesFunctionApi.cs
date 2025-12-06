using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarketGrowth.Api
{
    public class FavoritesFunction
    {
        private readonly ILogger<FavoritesFunction> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public FavoritesFunction(ILogger<FavoritesFunction> logger)
        {
            _logger = logger;

            var connectionString = Environment.GetEnvironmentVariable("CosmosConnection")
                ?? throw new InvalidOperationException("CosmosConnection setting is missing.");

            var databaseName = Environment.GetEnvironmentVariable("CosmosDbDatabase") ?? "marketgrowth";
            var containerName = Environment.GetEnvironmentVariable("CosmosDbContainer") ?? "favorites";

            _cosmosClient = new CosmosClient(connectionString);
            _container = _cosmosClient.GetContainer(databaseName, containerName);
        }

        // POST /api/favorites  -> skapa eller uppdatera favorit
        [Function("AddFavorite")]
        public async Task<HttpResponseData> AddFavorite(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "favorites")]
            HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var request = await JsonSerializer.DeserializeAsync<FavoriteAssetRequest>(req.Body, JsonOptions);

                if (request == null ||
                    string.IsNullOrWhiteSpace(request.UserId) ||
                    string.IsNullOrWhiteSpace(request.AssetId))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("UserId and AssetId are required.");
                    return response;
                }

                var entity = new FavoriteAssetEntity
                {
                    // gör id unikt per user + asset
                    Id = $"{request.UserId}_{request.AssetId}",
                    UserId = request.UserId,
                    AssetId = request.AssetId,
                    AssetType = request.AssetType,
                    Symbol = request.Symbol,
                    LastPrice = request.LastPrice,
                    CreatedUtc = DateTime.UtcNow
                };

                // Skapar eller uppdaterar raden
                await _container.UpsertItemAsync(entity, new PartitionKey(entity.UserId));

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding favorite");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Failed to add favorite.");
                return response;
            }
        }

        // GET /api/favorites/{userId}  -> hämta alla favoriter för en användare
        [Function("GetFavorites")]
        public async Task<HttpResponseData> GetFavorites(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "favorites/{userId}")]
            HttpRequestData req,
            string userId)
        {
            var response = req.CreateResponse();

            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                    .WithParameter("@userId", userId);

                var results = new List<FavoriteAssetEntity>();

                var iterator = _container.GetItemQueryIterator<FavoriteAssetEntity>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(userId)
                    });

                while (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync();
                    results.AddRange(page);
                }

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(results);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorites");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Failed to get favorites.");
                return response;
            }
        }

        // DELETE /api/favorites/{userId}/{assetId}  -> ta bort en favorit
        [Function("RemoveFavorite")]
        public async Task<HttpResponseData> RemoveFavorite(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "favorites/{userId}/{assetId}")]
            HttpRequestData req,
            string userId,
            string assetId)
        {
            var response = req.CreateResponse();

            try
            {
                var id = $"{userId}_{assetId}";

                await _container.DeleteItemAsync<FavoriteAssetEntity>(
                    id,
                    new PartitionKey(userId));

                response.StatusCode = HttpStatusCode.NoContent;
                return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync("Favorite not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Failed to remove favorite.");
                return response;
            }
        }
    }
}

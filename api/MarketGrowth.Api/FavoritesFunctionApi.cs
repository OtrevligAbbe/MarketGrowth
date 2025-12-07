using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace MarketGrowth.Api
{
    public class FavoritesFunctionApi
    {
        private readonly ILogger<FavoritesFunctionApi> _logger;

        // Håller Cosmos-klienten statiskt så vi inte skapar ny varje gång
        private static CosmosClient? _cosmosClient;
        private static Container? _container;

        public FavoritesFunctionApi(ILogger<FavoritesFunctionApi> logger)
        {
            _logger = logger;
        }

        private static Container? GetContainer(ILogger logger)
        {
            try
            {
                if (_container != null)
                    return _container;

                var conn = Environment.GetEnvironmentVariable("CosmosConnection");
                if (string.IsNullOrWhiteSpace(conn))
                {
                    logger.LogWarning("CosmosConnection setting is missing.");
                    return null;
                }

                var dbName = Environment.GetEnvironmentVariable("CosmosDbDatabase") ?? "marketgrowth";
                var contName = Environment.GetEnvironmentVariable("CosmosDbContainer") ?? "favorites";

                _cosmosClient = new CosmosClient(conn);
                _container = _cosmosClient.GetContainer(dbName, contName);

                return _container;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create CosmosClient / Container.");
                return null;
            }
        }


        // POST /api/favorites
        [Function("AddFavorite")]
        public async Task<HttpResponseData> AddFavorite(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "favorites")]
    HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);

            try
            {
                // 1) Läs body som text och logga den
                using var reader = new StreamReader(req.Body);
                var body = await reader.ReadToEndAsync();
                _logger.LogInformation("RAW BODY in AddFavorite: {Body}", body);

                // 2) Deserialisera body → FavoriteAssetRequest (case-insensitive)
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<FavoriteAssetRequest>(body, options);

                if (request == null ||
                    string.IsNullOrWhiteSpace(request.UserId) ||
                    string.IsNullOrWhiteSpace(request.AssetId))
                {
                    _logger.LogWarning("Validation failed in AddFavorite. UserId or AssetId missing.");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("UserId and AssetId are required.");
                    return response;
                }

                // 3) Bygg entitet
                var entity = new FavoriteAssetEntity
                {
                    Id = $"{request.UserId}_{request.AssetId}",
                    UserId = request.UserId,
                    AssetId = request.AssetId,
                    AssetType = request.AssetType,
                    Symbol = request.Symbol,
                    LastPrice = request.LastPrice,
                    CreatedUtc = DateTime.UtcNow
                };

                // 4) Läs Cosmos-inställningar och logga
                var connString = Environment.GetEnvironmentVariable("CosmosConnection");
                var databaseName = Environment.GetEnvironmentVariable("CosmosDbDatabase") ?? "marketgrowth";
                var containerName = Environment.GetEnvironmentVariable("CosmosDbContainer") ?? "favorites";

                _logger.LogInformation(
                    "Cosmos settings in AddFavorite: connNullOrEmpty={ConnNull}, db={Db}, container={Container}",
                    string.IsNullOrWhiteSpace(connString), databaseName, containerName);

                if (string.IsNullOrWhiteSpace(connString))
                {
                    _logger.LogError("CosmosConnection setting is missing. Skipping save.");
                }
                else
                {
                    try
                    {
                        _logger.LogInformation("Creating CosmosClient...");
                        using var cosmosClient = new CosmosClient(connString);
                        _logger.LogInformation("CosmosClient created successfully.");

                        var container = cosmosClient.GetContainer(databaseName, containerName);
                        _logger.LogInformation("Got container reference: {Db}/{Container}", databaseName, containerName);

                        _logger.LogInformation(
                            "Saving favorite to Cosmos: userId={UserId}, assetId={AssetId}",
                            entity.UserId, entity.AssetId);

                        var result = await container.UpsertItemAsync(
                            entity,
                            new PartitionKey(entity.UserId));

                        _logger.LogInformation(
                            "Cosmos upsert status code: {StatusCode}",
                            result.StatusCode);
                    }
                    catch (CosmosException cex)
                    {
                        _logger.LogError(cex,
                            "CosmosException when saving favorite (StatusCode={StatusCode})",
                            cex.StatusCode);
                    }
                    catch (Exception exInner)
                    {
                        _logger.LogError(exInner,
                            "Non-Cosmos exception in Cosmos save block: {Message}",
                            exInner.Message);
                    }
                }

                // 5) Skicka entiteten tillbaka till frontenden
                await response.WriteAsJsonAsync(entity);
                return response;
            }
            catch (Exception ex)
            {
                // Logga HELA exceptionen som text
                _logger.LogError(ex, "Unexpected error in AddFavorite (outer catch): {Error}", ex.ToString());
                await response.WriteStringAsync("Favorite received (internal error).");
                return response;
            }
        }


        private static async Task SaveToCosmosAsync(FavoriteAssetEntity entity)
        {
            // Vi har ingen logger här, så vi loggar via Console.WriteLine
            try
            {
                // OBS: vi har ingen ILogger här, så skicka in en "dummy"
                var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<FavoritesFunctionApi>.Instance;
                var container = GetContainer(logger);
                if (container == null)
                {
                    logger.LogWarning("Cosmos container is null, skipping save.");
                    return;
                }

                await container.UpsertItemAsync(entity, new PartitionKey(entity.UserId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save favorite to Cosmos: {ex}");
            }
        }

        // GET /api/favorites/{userId}
        [Function("GetFavorites")]
        public async Task<HttpResponseData> GetFavorites(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "favorites/{userId}")]
            HttpRequestData req,
            string userId)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);

            try
            {
                var container = GetContainer(_logger);
                if (container == null)
                {
                    await response.WriteAsJsonAsync(new List<FavoriteAssetEntity>());
                    return response;
                }

                var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                    .WithParameter("@userId", userId);

                var results = new List<FavoriteAssetEntity>();
                var iterator = container.GetItemQueryIterator<FavoriteAssetEntity>(
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

                await response.WriteAsJsonAsync(results);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFavorites");
                await response.WriteAsJsonAsync(new List<FavoriteAssetEntity>());
                return response;
            }
        }

        // DELETE /api/favorites/{userId}/{assetId}
        [Function("RemoveFavorite")]
        public async Task<HttpResponseData> RemoveFavorite(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "favorites/{userId}/{assetId}")]
            HttpRequestData req,
            string userId,
            string assetId)
        {
            var response = req.CreateResponse(HttpStatusCode.NoContent);

            try
            {
                var container = GetContainer(_logger);
                if (container == null)
                    return response;

                var id = $"{userId}_{assetId}";
                await container.DeleteItemAsync<FavoriteAssetEntity>(id, new PartitionKey(userId));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RemoveFavorite");
                return response;
            }
        }
    }
}

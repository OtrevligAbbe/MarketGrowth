using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using MarketGrowth.Api.Repositories;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // HttpClient för API-anrop
        services.AddHttpClient();

        // Cosmos-inställningar
        var cosmosConnection = context.Configuration["CosmosConnection"];
        var databaseName = context.Configuration["CosmosDbDatabase"];

        // CosmosClient som singleton
        var cosmosClient = new CosmosClient(cosmosConnection);
        services.AddSingleton(cosmosClient);

        // Repo för markethistory
        services.AddSingleton<IMarketSnapshotRepository>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var database = client.GetDatabase(databaseName);
            var container = database.GetContainer("markethistory");
            return new MarketSnapshotRepository(container);
        });

        // Repo för marketalerts
        services.AddSingleton<IMarketAlertRepository>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var database = client.GetDatabase(databaseName);
            var container = database.GetContainer("marketalerts");
            return new MarketAlertRepository(container);
        });
    })
    .Build();

host.Run();

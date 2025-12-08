using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using MarketGrowth.Api.Repositories;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient();

        var cosmosConnection = context.Configuration["CosmosConnection"];
        var databaseName = context.Configuration["CosmosDbDatabase"];

        var cosmosClient = new CosmosClient(cosmosConnection);
        services.AddSingleton(cosmosClient);

        services.AddSingleton<IMarketSnapshotRepository>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var database = client.GetDatabase(databaseName);
            var container = database.GetContainer("markethistory");
            return new MarketSnapshotRepository(container);
        });

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

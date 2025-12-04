using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Lägger till HttpClient för att kunna injicera det i dina Functions
        services.AddHttpClient();

        // Här kan du senare lägga till t.ex. Cosmos DB Client
        // services.AddSingleton<CosmosClient>(...);

    })
    .Build();

host.Run();

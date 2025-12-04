using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // 1. Lägger till HttpClient för att kunna injicera det i dina functions
        services.AddHttpClient();

        // Här kan du senare lägga till andra tjänster som Cosmos DB Client

    })
    .Build();

host.Run();
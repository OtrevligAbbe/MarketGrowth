using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Lägger till HttpClient för att kunna injicera det i dina Functions
        services.AddHttpClient();

    })
    .Build();

host.Run();

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1) Vanlig HttpClient för saker som ligger på samma adress som frontenden
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// 2) Named HttpClient för din Azure Functions-API (Coingecko + AlphaVantage)
builder.Services.AddHttpClient("MarketGrowth.Api", client =>
{
    // LOKALT: din Functions-adress
    client.BaseAddress = new Uri("http://localhost:7247/");
    // När du sedan deployar kan du byta till t.ex:
    // client.BaseAddress = new Uri("https://DIN-FUNCTION-APP.azurewebsites.net/");
});

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
});

await builder.Build().RunAsync();

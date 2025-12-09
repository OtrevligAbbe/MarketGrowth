# 📈 MarketGrowth

> Serverless molnbaserad marknadsplattform för finansiell analys i
> Microsoft Azure\
> Examensarbete - Cloud Developer (.NET)

------------------------------------------------------------------------

## 📖 Om Projektet

**MarketGrowth** är en fullstack, serverless webbapplikation utvecklad
som examensarbete inom Cloud Development. Projektet demonstrerar hur man
bygger en modern, skalbar och kostnadseffektiv marknadsplattform helt i
Microsoft Azure med fokus på:

-   Serverless-arkitektur
-   Externa API-integrationer
-   Realtidsdata
-   Säker hemlighetshantering
-   Automatiserad CI/CD
-   Produktionsövervakning

Plattformen låter användare: - Visa realtidskurser för kryptovalutor,
aktier och index - Följa prisförändringar grafiskt via sparklines -
Spara personliga favoriter - Ta del av automatiskt genererade
marknadsalerts

------------------------------------------------------------------------

## 📂 Dokumentation & Presentation

För en djupare insikt i projektets arkitektur, affärsnytta och tekniska
implementation, se bifogade dokument:

-   📊 **[Verktygspresentation
    (PDF)](docs/99albste_Examensarbete_Verktygspresentation.pdf)**\
    *En överblick av produkten, målgrupp, scenario och funktioner.*

-   📘 **[Teknisk Slutrapport
    (PDF)](docs/99albste_Examensarbete_TekniskDokumentation.pdf)**\
    *Djupgående teknisk dokumentation om arkitekturval, CI-CD, säkerhet
    och kodanalys.*

------------------------------------------------------------------------

## 🏗️ Systemarkitektur

MarketGrowth är byggt enligt en serverless trelagersarkitektur:

  ------------------------------------------------------------------------
  Lager           Teknik                Beskrivning
  --------------- --------------------- ----------------------------------
  Frontend        Blazor WebAssembly    Körs i webbläsaren via Azure
                                        Static Web Apps

  Backend/API     Azure Functions (.NET Hanterar affärslogik, caching,
                  8 Isolated)           snapshots och alerts

  Databas         Azure Cosmos DB (SQL  Lagrar favoriter, historik och
                  API)                  alerts

  Säkerhet        Azure Key Vault +     Skyddar alla hemligheter
                  Managed Identity      

  Övervakning     Application           Drift, fel och prestanda
                  Insights + Azure      
                  Monitor + Grafana     

  CI-CD           GitHub Actions        Automatisk build och deploy av
                                        frontend & backend
  ------------------------------------------------------------------------

------------------------------------------------------------------------

## ✨ Funktioner

-   ✅ Realtidsdata från externa marknads-API:n
-   ✅ Sparklines för visuell trendanalys
-   ✅ Favoriter sparas per användare i Cosmos DB
-   ✅ Alert-system via bakgrundsjobs (Timer Trigger)
-   ✅ Fallback-lösningar vid API-fel
-   ✅ Full serverless-drift
-   ✅ Säker hemlighetshantering via Key Vault

------------------------------------------------------------------------

## 🛠️ Teknisk Stack

### Frontend

-   C#
-   Blazor WebAssembly
-   HTML5 / CSS3

### Backend

-   Azure Functions v4 (.NET 8 Isolated)
-   Dependency Injection
-   REST-API
-   Timer Triggers

### Cloud & DevOps

-   Azure Static Web Apps
-   Azure Cosmos DB
-   Azure Key Vault
-   Application Insights
-   Azure Monitor
-   GitHub Actions (CI-CD)

------------------------------------------------------------------------

## 📁 Projektstruktur (Förenklad)

    MarketGrowth/
    │
    ├── api/MarketGrowth.Api/
    │   ├── Entities/
    │   ├── Functions/
    │   ├── Models/
    │   ├── Repositories/
    │   └── Program.cs
    │
    ├── frontend/
    │   ├── Pages/
    │   ├── Layout/
    │   ├── Shared/
    │   └── Program.cs
    │
    └── .github/workflows/
        ├── azure-static-web-apps.yml
        └── main_marketgrowth-api.yml

------------------------------------------------------------------------

## 🚀 Köra Projektet Lokalt

### Förkrav

-   .NET 8 SDK
-   Azure Functions Core Tools v4
-   Azure Cosmos DB (eller Emulator)

### 1. Klona repot

``` bash
git clone https://github.com/OtrevligAbbe/MarketGrowth.git
cd MarketGrowth
```

### 2. Konfigurera Backend

Skapa `local.settings.json` i:

    api/MarketGrowth.Api/

``` json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosConnection": "DIN_COSMOS_CONNECTION_STRING",
    "ALPHAVANTAGE_API_KEY": "DIN_API_NYCKEL"
  }
}
```

Starta backend:

``` bash
func start
```

### 3. Konfigurera Frontend

Sätt API-URL till:

    http://localhost:7071

Starta frontend:

``` bash
dotnet watch
```

------------------------------------------------------------------------

## 👤 Författare

**Albin Stenhoff**\
Cloud Developer Student\
Sverige

------------------------------------------------------------------------

## 📄 Licens

Projektet är licensierat under MIT-licens.

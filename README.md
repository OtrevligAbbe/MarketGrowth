# MarketGrowth - Cloud-Native Market Analytics Platform

MarketGrowth is a serverless, cloud-native market analytics platform
built on Microsoft Azure. It aggregates real-time market data (stocks,
crypto, and other assets) from multiple external APIs and exposes the
data through a secure, scalable API and a modern web frontend.

The project demonstrates modern cloud architecture, secure secret
management, CI/CD automation, and production-grade monitoring.

------------------------------------------------------------------------

## High-Level Architecture

Frontend (Blazor WebAssembly) → Azure Function API (MarketGrowth.Api) →
Azure Cosmos DB (Favorites, Users) → External APIs (AlphaVantage,
CoinGecko)

Supporting Services: - Azure Key Vault (Secure Secrets) - Azure
Application Insights (Monitoring & Logging) - GitHub Actions (CI/CD) -
Azure Alerts & Dashboards (Operations & Observability)

------------------------------------------------------------------------

## Azure Services Used

  Service                        Purpose
  ------------------------------ --------------------------------------
  Azure Functions                Serverless backend API
  Azure Cosmos DB                NoSQL database for favorites & users
  Azure Key Vault                Secure storage of API keys & secrets
  Azure Application Insights     Monitoring, logging & performance
  Azure Monitor Alerts           Automatic error detection
  Azure Dashboards & Workbooks   Operational monitoring
  GitHub Actions                 CI/CD pipeline
  Azure Static Web Apps          Frontend hosting
  Microsoft Entra ID             Authentication

------------------------------------------------------------------------

## Security Design

-   All secrets stored in Azure Key Vault
-   Managed Identity used for secure access
-   No API keys stored in code or GitHub
-   Environment variables resolved via Key Vault references
-   Authentication via Azure AD

------------------------------------------------------------------------

## CI/CD Pipeline

The project uses GitHub Actions for fully automated deployment:

-   Trigger on every push to main
-   Builds the .NET API
-   Deploys directly to Azure Function App
-   Uses Azure federated credentials (OIDC)
-   No secrets stored in GitHub

Pipeline location: .github/workflows/main_marketgrowth-api-astenhoff.yml

------------------------------------------------------------------------

## Monitoring & Observability

Implemented Monitoring Features: - Application Insights logging - Live
Metrics - Failure tracking - Request duration analysis - Custom Azure
Alerts - Azure Workbook dashboards - Grafana preview dashboards

------------------------------------------------------------------------

## Project Structure

MarketGrowth/

-   api/MarketGrowth.Api - Azure Function backend
-   frontend - Blazor WebAssembly frontend
-   .github/workflows - CI/CD pipeline
-   MarketGrowth_Dokumentation.md
-   README.md

------------------------------------------------------------------------

## Environment Configuration

Local Development (local.settings.json): - ALPHAVANTAGE_API_KEY -
COINGECKO_API_KEY - CosmosConnection - CosmosDbDatabase -
CosmosDbContainer

Production (Azure): All secrets are resolved using:
@Microsoft.KeyVault(SecretUri=...)

------------------------------------------------------------------------

## External APIs

  API            Purpose
  -------------- ----------------------------
  AlphaVantage   Stock market data
  CoinGecko      Cryptocurrency market data

------------------------------------------------------------------------

## Implemented Features

-   Market data aggregation
-   User authentication
-   Favorites system
-   Secure API key handling
-   Cloud-native backend
-   Auto-deploy CI/CD pipeline
-   Monitoring & alerts
-   Operational dashboards
-   Fault tracking & live logs

------------------------------------------------------------------------

## Project Goals

-   Demonstrate real-world cloud architecture
-   Apply secure DevOps practices
-   Build a production-ready serverless system
-   Use modern observability and monitoring
-   Learn Azure in depth (Functions, Vault, Monitor, CI/CD, Identity)

------------------------------------------------------------------------

## Testing Strategy

-   API tested via browser requests
-   Azure live logs
-   Application Insights queries
-   CI/CD pipeline automatically validates build on every push

------------------------------------------------------------------------

## Scalability & Performance

-   Serverless architecture scales automatically
-   Cosmos DB is fully managed and globally scalable
-   Frontend hosted on Azure Static Web Apps

------------------------------------------------------------------------

## Documentation

Additional documentation: - MarketGrowth_Dokumentation.md - Technical
design & architecture - README.md - Project overview - Azure dashboards
& logs for live system behavior

------------------------------------------------------------------------

## Author

Developed by Albin Stenhoff\
Cloud & Backend Development Student\
Focus: Azure, .NET, Serverless, DevOps

# MarketGrowth

MarketGrowth is a modern serverless cloud application built on Microsoft Azure that aggregates real-time market data such as stocks and cryptocurrencies using external APIs. The project demonstrates how to build a secure, scalable, and cost-effective cloud-native solution using multiple Azure services.

The system consists of a Blazor frontend, a serverless API backend using Azure Functions, and a cloud database powered by Azure Cosmos DB. Continuous deployment is handled through GitHub Actions, and all sensitive secrets are securely stored in Azure Key Vault.

---

## Architecture Overview

MarketGrowth is built with a fully serverless and event-driven architecture:

- Frontend: Blazor WebAssembly hosted in Azure Static Web Apps
- Backend API: Azure Functions (.NET 8 Isolated)
- Database: Azure Cosmos DB (NoSQL)
- Authentication: Azure AD B2C
- CI/CD: GitHub Actions
- Secrets Management: Azure Key Vault
- Monitoring & Logging: Application Insights
- Alerts & Dashboards: Azure Monitor, Alerts & Workbooks

---

## Core Features

- Real-time market data from external APIs (AlphaVantage, CoinGecko)
- Secure user authentication using Azure AD B2C
- Personal favorite tracking stored in Cosmos DB
- Fully automated CI/CD pipeline
- Production-grade monitoring with metrics, logs, alerts and dashboards
- Secure secret handling using Azure Key Vault references
- Scalable serverless backend with minimal cost footprint

---

## Technologies Used

- C#
- .NET 8
- Azure Functions
- Azure Cosmos DB
- Azure Static Web Apps
- Azure AD B2C
- Azure Key Vault
- Azure Application Insights
- Azure Monitor
- GitHub Actions
- Blazor WebAssembly

---

## CI/CD Pipeline

The project is automatically built and deployed using GitHub Actions:

- Push to main triggers build
- .NET project is compiled
- Azure login is performed using federated identity
- Azure Functions app is deployed automatically

This ensures zero-downtime deployments with full traceability.

---

## Security

- All API keys and connection strings are stored in Azure Key Vault
- No secrets are stored in code or GitHub
- Azure Managed Identity is used for secure access to Key Vault
- Authentication is handled by Azure AD B2C

---

## Monitoring & Observability

MarketGrowth is fully monitored using Azure-native tooling:

- Application Insights tracks requests, failures, latency, dependencies and exceptions
- Azure Alerts notify on failed requests and performance degradation
- Azure Workbooks visualize system health and traffic trends

---

## Project Structure

```
MarketGrowth/
│
├── api/MarketGrowth.Api       -> Azure Functions backend
├── frontend                  -> Blazor WebAssembly frontend
├── .github/workflows         -> GitHub CI/CD pipelines
├── README.md
└── MarketGrowth_Dokumentation.md
```

---

## Purpose of the Project

This project was developed as part of a cloud and system development examination to demonstrate:

- Serverless architecture
- Secure cloud authentication
- API integration
- Cloud database usage
- CI/CD automation
- Monitoring and alerting
- Production-grade cloud design principles

---

## Author

Developed by OtrevligAbbe

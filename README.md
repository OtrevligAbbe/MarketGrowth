# MarketGrowth
> **En Serverless Cloud plattform för Finansiell Analys i Microsoft Azure.**

---

## 📖 Om Projektet

**MarketGrowth** är ett examensarbete inom Cloud Development som demonstrerar hur man bygger en modern, skalbar och kostnadseffektiv finanstjänst helt utan servrar.

Plattformen aggregerar realtidsdata för kryptovalutor, aktier och index från externa API:er och presenterar detta i en blixtsnabb SPA (Single Page Application). Genom att utnyttja **Azure Serverless**-teknik skalas systemet automatiskt efter belastning samtidigt som driftkostnaderna minimeras.

---

## 📂 Dokumentation & Presentation

För en djupare insikt i projektets arkitektur, affärsnytta och tekniska implementation, se bifogade dokument:

- 📊 **[Verktygspresentation (PDF)](docs/99albste_Examensarbete_Verktygspresentation_MarketGrowth.pdf)**
  *En överblick av produkten, målgrupp, scenario och funktioner.*

- 📘 **[Teknisk Slutrapport (PDF)](docs/99albste_Examensarbete_TekniskDokumentation_MarketGrowth.pdf)**
  *Djupgående teknisk dokumentation om arkitekturval, CI/CD, säkerhet och kodanalys.*

---

## 🏗️ Systemarkitektur

Systemet är byggt enligt en händelsestyrd mikrotjänst arkitektur:

| Komponent | Teknik | Beskrivning                                                                   |
| :--- | :--- |:------------------------------------------------------------------------------|
| **Frontend** | Blazor WebAssembly | Körs i klientens webbläsare, hostad på **Azure Static Web Apps**.             |
| **API Gateway** | Azure Functions | .NET 8 Isolated Worker. Hanterar affärslogik, caching och proxy-anrop.        |
| **Databas** | Azure Cosmos DB | NoSQL databas partitionerad för hög prestanda. Lagrar favoriter och historik. |
| **Säkerhet** | Azure Key Vault | Lagrar alla hemligheter. Åtkomst via **Managed Identity**.                    |
| **Övervakning** | Application Insights | Realtidsloggning, prestandamätning och distributed tracing.                   |

*Systemet driftas i två separata resursgrupper för logisk separation av Compute och Data.*

---

## ✨ Huvudfunktioner

* **Realtidsdata:** Aggregering av live kurser från CoinGecko och Alpha Vantage.
* **Sparklines:** Visuell trendanalys (7 dagar) direkt i listvyn.
* **Favoriter:** Personlig bevakningslista som sparas persistent i molnet per användare.
* **Intelligenta Alerts:** Bakgrundsprocess (Timer Trigger) som övervakar marknaden och loggar stora prisrörelser.
* **Enterprise Security:** Inga hårdkodade lösenord. All konfiguration sker via Key Vault.

---

## 🛠️ Teknisk Stack

**Frontend:**
* C# / Blazor WASM
* HTML5 / CSS3 (Custom Dark Theme)

**Backend:**
* Azure Functions v4 (.NET 8 Isolated)
* Dependency Injection
* Entity Models / DTOs

**DevOps & Cloud:**
* **CI/CD:** GitHub Actions (Separata pipelines för Frontend och Backend)
* **IaC:** Infrastruktur hanteras via Azure Portal deployment
* **Database:** Cosmos DB (SQL API)

---

## 🚀 Kom igång (Lokalt)

För att köra projektet på din egen maskin:

### Förkrav
* .NET 8 SDK
* Azure Functions Core Tools v4
* En Cosmos DB instans (eller Emulator)

### 1. Klona repot
```bash
git clone [https://github.com/OtrevligAbbe/MarketGrowth.git](https://github.com/OtrevligAbbe/MarketGrowth.git)
cd MarketGrowth
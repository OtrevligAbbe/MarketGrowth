# MarketGrowth – Dokumentation

## 1. Projektbeskrivning
MarketGrowth är en webbaserad applikation för att visa realtidsdata för kryptovalutor och aktier med hjälp av en serverless-arkitektur i Azure.

## 2. Syfte
Syftet med projektet är att bygga en modern cloud-baserad lösning för finansiell marknadsdata samt att demonstrera användning av serverless-tjänster.

## 3. Systemöversikt
Systemet består av:
- Frontend: Azure Static Web Apps
- Backend: Azure Functions (serverless API)
- Externa API:er för marknadsdata
- CI/CD via GitHub Actions

## 4. Frontend
Frontend är byggd i Blazor WebAssembly och hostas via Azure Static Web Apps.

## 5. Backend
Backend består av Azure Functions som hämtar realtidsdata från externa API:er.

## 6. CI/CD
Projektet deployas automatiskt via GitHub Actions vid push till main-branch.

## 7. Säkerhet
API-nycklar lagras som secrets i Azure och GitHub.

## 8. Resultat
Applikationen visar aktuell marknadsdata i realtid.

## 9. Diskussion
Systemet är skalbart, kostnadseffektivt och lämpar sig väl för cloud-miljöer.

## 10. Framtida förbättringar
- Inloggning
- Favoritlistor
- Avancerade grafer

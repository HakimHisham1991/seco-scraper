# Seco Item Description Harvester

Production-ready ASP.NET Core (.NET 10) application that accepts hundreds or thousands of Seco item numbers, resolves **Item Description** values via DeepSeek (primary) and local HTML scraping (fallback), and exports CSV results.

**Version:** 1.0.6

## Features

- Paste or CSV upload of item numbers (10,000+ supported)
- SQLite persistence with resumable background processing
- DeepSeek API integration (Ollama local or OpenAI-compatible cloud endpoint)
- Fallback scraper for `https://www.secotools.com/article/p_{itemNumber}`
- REST API and Bootstrap-style dashboard (auto-refresh every 10 seconds)
- MIT-licensed dependencies only

## Quick start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Optional: [Ollama](https://ollama.com/) or DeepSeek API key for AI lookup

### Run locally

```bash
dotnet restore SecoItemHarvester.sln
dotnet run --project SecoItemHarvester.Web
```

Open `http://localhost:5074` (or the URL shown in the console). Use `dotnet run --launch-profile https` for HTTPS on port 7098.

### Configuration (`appsettings.json`)

```json
{
  "DeepSeek": {
    "BaseUrl": "http://localhost:11434",
    "ApiKey": "",
    "Model": "deepseek-r1",
    "Enabled": true
  },
  "Processing": {
    "BatchSize": 20,
    "MaxConcurrency": 5,
    "RetryCount": 3
  }
}
```

Set `DeepSeek:ApiKey` and `DeepSeek:UseOpenAiCompatibleApi` to `true` for `https://api.deepseek.com` (`BaseUrl` = `https://api.deepseek.com`).

## REST API

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/items/upload` | JSON body `{ "itemNumbers": ["02679365"] }` |
| POST | `/api/items/upload/csv` | Multipart CSV upload |
| POST | `/api/items/process` | Start background processing |
| POST | `/api/items/retry-failed` | Reset failed rows to pending |
| GET | `/api/items/status` | Queue counts |
| GET | `/api/items/results` | Paginated results |
| GET | `/api/items/export` | Download completed CSV |

Example output:

```csv
ItemNumber,ItemDescription
02679365,553055Z3.0-SIRON-A
```

## Docker

```bash
docker compose up --build
```

Dashboard: `http://localhost:8080`

## Tests

```bash
dotnet test SecoItemHarvester.sln
```

## Architecture

```
Controllers/     REST API
Services/        DeepSeek client, lookup orchestration, CSV, parsers
Repositories/    EF Core data access
Workers/         Background batch processor
Data/            DbContext + migrations
Models/ Dtos/    Domain and API models
Pages/           Razor dashboard
```

## License

MIT (application code). Third-party packages are MIT or Apache-2.0 compatible (.NET runtime, EF Core, etc.).

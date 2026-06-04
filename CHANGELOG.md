# Changelog

All notable changes to this project are documented in this file.

## [1.0.6] - 2026-06-04

### Changed

- **Auto-clear on page load/refresh** — database and dashboard reset automatically when you open or reload the app (no confirmation).
- API GET responses are marked no-cache; polling uses `cache: no-store` to avoid stale browser data.

## [1.0.5] - 2026-06-04

### Added

- **Clear Results** button — deletes all items from the database and resets the results table, dashboard counts, and timer (with confirmation).

### Changed

- **Clear Input** only clears the paste area and CSV file picker (no longer affects results).

## [1.0.4] - 2026-06-04

### Changed

- **Upload List** and **Upload CSV** now clear all previous items from the database and reset the results table, dashboard counts, and elapsed timer before loading the new list.

## [1.0.3] - 2026-06-04

### Changed

- Dashboard: added **No.** column before Item Number; renamed **Description** to **Designation**.
- Added **Elapsed Time** timer (TaeguTec-style) after Failed; starts on Start Processing / Retry Failed and stops when the queue is idle.

## [1.0.2] - 2026-06-04

### Changed

- Aligned dev startup with TaeguTec scraper: default `http` launch profile, built-in ASP.NET Core logging (removed Serilog console output), and standard “Now listening on…” messages.
- Default URL is `http://localhost:5074` (`https` profile: `https://localhost:7098`).

## [1.0.1] - 2026-06-04

### Fixed

- EF Core migrations were not registered correctly, so `ItemLookup` was never created on startup (`no such table: ItemLookup`). Regenerated migrations with `dotnet-ef` so `Database.MigrateAsync()` applies the schema.

## [1.0.0] - 2026-06-04

### Added

- Initial release of Seco Item Description Harvester (.NET 10)
- SQLite + EF Core `ItemLookup` queue with Pending / Processing / Completed / Failed states
- Background worker with configurable batch size and concurrency
- `IDeepSeekClient` / `DeepSeekClient` with retries, rate limiting, and structured logging
- Seco HTML fallback scraper (`SecoItemDescriptionParser`) for product pages
- REST API: upload, process, status, results, export, retry-failed
- Razor dashboard with auto-refresh, CSV upload, and export
- Unit tests, Dockerfile, and docker-compose

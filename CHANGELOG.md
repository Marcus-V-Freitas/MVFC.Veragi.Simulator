# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-04

### Added

- Initial release of **MVFC.Veragi.Simulator**, a high-fidelity card receivables simulator built on .NET 10, C# 13, Minimal APIs, and MongoDB with EF Core.
- Complete domain modeling and MediatR handlers for card receivables lifecycle:
  - Merchant management (`POST`, `GET`, `PATCH`, `DELETE` `/merchants`).
  - Catalog lookups for partner acquirers and payment arrangements.
  - Asynchronous receivables schedule queries with polling and outbox webhook delivery.
  - Credit anticipation contracts with collateral reservation, allocation, and balance depletion.
  - Bank reconciliation ledger with automatic proportional apportionment across active contracts.
- Dual schema support for schedule webhooks via `ScheduleWebhookSchema`:
  - `Schedule`: Standard schedule query notification schema (`ScheduleWebhookNotification`).
  - `ContractReceivables`: Contract receivables view schema (`ContractWebhookNotification`).
- Guaranteed canonical GUID format for `TraceId` across `ApiResponse<T>`, `ProblemDetails`, and exception logging while maintaining string typing.
- Local orchestration setup with **.NET Aspire** (`MVFC.Veragi.Simulator.AppHost`) and standalone Docker Compose support (`compose.yaml`).
- Light-weight webhook receiver worker Minimal API (`MVFC.Veragi.Simulator.WebhookWorker`) for local end-to-end event logging and testing.
- Automated HTTP testing scripts:
  - `scripts/00-complete-flow.http` executing 31 end-to-end lifecycle steps.
  - `scripts/cenarios/` containing 16 dedicated, self-contained business scenario tests (01 to 16).
- 633 unit and integration tests covering 100% lines and branches across all production assemblies.
- `build.cake` build script with automated test execution and strict 100% code coverage threshold enforcement.
- Complete technical and business documentation, including unified `README.md` and diagrams in `docs/handlers/`.

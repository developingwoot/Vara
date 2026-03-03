# Changelog

All notable changes to VARA are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning: [Semantic Versioning](https://semver.org/).

---

## [1.0.0] — 2026-03-02

### Added
- Video Analysis page — transcript + performance metrics via REST
- Niche Compare page — side-by-side niche competitive benchmarks
- Settings layout with Account sub-page (name/email edit, password change)
- Sidebar credits display (credits used / included)
- `recentAnalyses` persisted to localStorage
- Production Dockerfile (`.NET 10`) and `docker-compose.prod.yml`
- GitHub Actions CI — backend build/test + frontend check/build
- `adapter-node` for SvelteKit production deployment

### Changed
- Dockerfile SDK/runtime updated from `9.0` to `10.0`
- TypeScript interfaces replacing `any` in analysis result pages

### Fixed
- Database indexes on `usage_logs` and `keyword_volume_history` for query performance

---

## [0.11.0] — 2026-02-02

### Added
- SvelteKit 5 frontend with full design system (DM Sans/Mono, oklch tokens)
- SignalR hub (`/api/hub/analysis`) for real-time keyword + trend analysis
- Keyword Analysis page with live progress and AI Insights tier gate
- Trend Detection page with rising/declining/new columns
- Channel Management page (add, list, remove)
- Plugins page with Outlier Detection execution
- Auth pages (login, register) with JWT + refresh token flow
- Sidebar with active route highlighting and tier badge

### Fixed
- SignalR `CancellationToken` parameter counted as required client argument — removed from hub method signature
- JWT claim mapping: `"sub"` vs `ClaimTypes.NameIdentifier` with `MapInboundClaims = false`
- Channel add request field mismatch (`youtubeUrl` → `handleOrUrl`)

---

## [0.10.0] — 2026-01-05

### Added
- Background trend analysis job (`TrendAnalysisBackgroundService`)
- Background job health monitor endpoint (`GET /health`)
- Niche comparison service and endpoint (`POST /api/analysis/niche/compare`)
- Global exception middleware with structured error responses

---

## [0.9.0] — 2025-12-07

### Added
- Plugin management system (`PluginRegistry`, `PluginDiscoveryService`, `PluginExecutionService`)
- Outlier Detection plugin — identifies underperforming videos vs channel average
- Plugin endpoints (`GET /api/plugins`, `POST /api/plugins/{name}/execute`)

---

## [0.8.0] — 2025-11-02

### Added
- Multi-provider LLM orchestration (Anthropic Claude, OpenAI GPT, Groq)
- AI-generated keyword insights (Creator tier)
- Transcript analysis service
- Polly resilience pipelines for all HTTP clients

---

## [0.7.0] — 2025-10-05

### Added
- Trend detection service — rising, declining, and new keyword trends
- Keyword volume history tracking
- Trend analysis endpoints (`GET /api/analysis/trends`)

---

## [0.6.0] — 2025-09-07

### Added
- Enhanced keyword analyzer with competition scoring
- Keyword intent classification
- Usage metering and quota enforcement per tier

---

## [0.5.0] — 2025-08-03

### Added
- Video analysis endpoints (`POST /api/analysis/videos`)
- Transcript fetching service
- YouTube channel management (`POST|GET /api/channels`)

---

## [0.4.0] — 2025-07-06

### Added
- Plan enforcer (Free / Creator tier feature gating)
- Keyword service and endpoints (`POST|GET /api/keywords`)
- YouTube video search and metadata endpoints

---

## [0.3.0] — 2025-06-01

### Added
- YouTube Data API v3 client with caching (`VideoCache`)
- HTTP resilience (retry + timeout) via `Microsoft.Extensions.Http.Resilience`

---

## [0.2.0] — 2025-05-04

### Added
- JWT authentication with refresh token rotation
- User registration and login endpoints
- FluentValidation request validation

---

## [0.1.0] — 2025-04-06

### Added
- Initial project setup: ASP.NET Core 10, EF Core, PostgreSQL
- Docker Compose development environment
- Serilog structured logging
- OpenAPI (Scalar) documentation

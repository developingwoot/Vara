# VARA — Video Analyzer Research Assistant

> AI-powered content intelligence for YouTube creators. Keyword research, trend detection, competitive analysis, and AI-generated insights — all in one platform.

![CI](https://github.com/YOUR_USERNAME/vara/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/badge/license-MIT-blue.svg)

---

## Features

- **Keyword Analysis** — competition scores, trend direction, intent classification, AI insights
- **Trend Detection** — rising/declining/new keyword trends by niche
- **Video Analysis** — transcript extraction, performance metrics, outlier detection
- **Niche Comparison** — side-by-side niche competitive benchmarks
- **Channel Management** — track your own and competitor channels
- **Plugin System** — extensible analysis plugins (Outlier Detection included)
- **Real-time Updates** — SignalR WebSocket for live analysis progress
- **Tier System** — Free and Creator plans with usage quotas

---

## Quick Start

### Prerequisites

- Docker + Docker Compose
- A YouTube Data API v3 key
- (Optional) API keys for Anthropic, OpenAI, or Groq for AI insights

### Development

```bash
git clone https://github.com/YOUR_USERNAME/vara.git
cd vara

# Copy and fill in your secrets
cp infrastructure/.env.example infrastructure/.env

# Start Postgres + API with hot reload
docker compose -f infrastructure/docker-compose.yml watch
```

API is available at `http://localhost:5000` with Scalar docs at `http://localhost:5000/scalar/v1`.

```bash
# Frontend (separate terminal)
cd src/vara-frontend
npm install
npm run dev
```

Frontend is available at `http://localhost:5173`.

### Production

```bash
cp infrastructure/.env.example infrastructure/.env.prod
# Fill in production values

docker compose -f infrastructure/docker-compose.prod.yml up -d
```

---

## Architecture

```
vara/
├── src/
│   ├── Vara.Api/          # ASP.NET Core 10 REST API + SignalR
│   └── vara-frontend/     # SvelteKit 5 frontend
├── tests/
│   └── Vara.Tests/        # xUnit integration tests (122 passing)
├── plugins/
│   └── outlier-detection/ # Bundled analysis plugin
├── infrastructure/
│   ├── docker-compose.yml       # Development
│   └── docker-compose.prod.yml  # Production
└── docs/                  # Full specification and design docs
```

**Backend stack**: .NET 10 · EF Core · PostgreSQL · Serilog · FluentValidation · SignalR · Polly

**Frontend stack**: SvelteKit 5 · Svelte 5 · TypeScript · Tailwind CSS v4 · DM Sans/Mono

---

## Configuration Reference

All secrets go in `infrastructure/.env` (never committed). See `infrastructure/.env.example`.

| Variable | Required | Description |
|---|---|---|
| `JWT_SECRET` | Yes | Min 32-char random string for JWT signing |
| `YOUTUBE_API_KEY` | Yes | YouTube Data API v3 key |
| `ANTHROPIC_API_KEY` | No | Enables Claude AI insights |
| `OPENAI_API_KEY` | No | Enables GPT AI insights |
| `GROQ_API_KEY` | No | Enables Groq AI insights |

At least one LLM key is needed for AI Insights features.

---

## Running Tests

```bash
dotnet test tests/Vara.Tests/
```

Tests use an in-memory SQLite database; no external services required.

---

## Deployment

See [docs/INFRASTRUCTURE.md](docs/INFRASTRUCTURE.md) for the full Hetzner + Terraform deployment guide.

---

## Documentation

| Doc | Description |
|---|---|
| [Complete Specification](docs/COMPLETE_SPEC.md) | Feature definitions & data models |
| [Episode Roadmap](docs/EPISODE_ROADMAP.md) | Build-in-public development series |
| [Plugin & LLM Architecture](docs/PLUGIN_LLM_ARCHITECTURE.md) | System design deep-dive |
| [UI Design System](docs/UI_DESIGN_SYSTEM.md) | Token system, component specs |
| [Infrastructure Guide](docs/INFRASTRUCTURE.md) | Hetzner + Terraform + CI/CD |
| [Monetization Strategy](docs/MONETIZATION.md) | Pricing & business model |

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE) © 2026

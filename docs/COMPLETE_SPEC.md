# VARA: Complete Specification
## Video Analyzer Research Assistant

**Version:** 2.1 (Final MVP Edition)  
**Last Updated:** February 2026  
**Tech Stack:** .NET 10 (C#) + PostgreSQL + Svelte + SignalR  
**Development Model:** Solo developer, ~20 hrs/month, 10-core + 3-bonus episodes (12 months to MVP)  
**Release Strategy:** Monthly "state of VARA" videos (15-25 min, showing progress + learnings)

---

## Table of Contents
1. Executive Summary
2. Feature Definitions
3. Architecture Overview
4. Database Schema
5. API Design
6. Core Technologies & Rationale
7. Getting Started Checklist

---

## Executive Summary

VARA is an AI-powered content research platform for YouTube creators. It analyzes videos, identifies keyword opportunities, detects trends, and compares niches using intelligent multi-provider LLM orchestration.

**Core Value:**
- Keyword research with LLM insights
- Video pattern analysis (titles, tags, engagement)
- Trend detection (rising keywords by momentum)
- Niche comparison (gap analysis)
- **Community extensible via plugins**
- **Monetizable SaaS layer** (convenience + compute)

**Key Differentiator:** Not "just another YouTube analytics tool"—sophisticated LLM orchestration (Claude for depth, GPT-4o-mini for speed, Groq for cost) that intelligently selects the right model per task.

---

## Feature Definitions

### Feature 1: Keyword Research
**Purpose:** Identify high-opportunity keywords for video creators

**Inputs:**
- Keyword (required)
- Niche (optional)
- Volume/difficulty constraints (optional)

**Outputs:**
- Search volume (0-100 scale)
- Competition score (0-100)
- Trend direction (rising/flat/declining)
- Keyword intent (educational, entertainment, how-to, opinion, news)
- **With LLM (Pro tier):** Strategic insights—why creators should care, positioning strategies, content gaps, video angle ideas

**Data Model:**
```
SearchVolume: relative_scale (1-100)
CompetitionScore: 0-100 (based on top 10 videos)
TrendScore: -100 to +100 (growth %)
KeywordIntent: enum
```

---

### Feature 2: Video Analysis
**Purpose:** Discover patterns in successful videos

**Inputs:**
- Keyword or video IDs
- Sample size (default: 20)

**Outputs:**
- Title length stats (mean, median, range)
- Description length stats
- Tag count and common tag clusters
- Duration patterns
- Engagement rate analysis
- Thumbnail text analysis
- **With LLM (Pro tier):** Why these patterns work, structural recommendations

**Analysis Includes:**
- Correlation: does duration affect engagement?
- Common tags across top videos
- Upload patterns (day of week, time trends)
- Most successful video "formula" for niche

---

### Feature 3: Trend Detection
**Purpose:** Identify rising topics and keywords

**Inputs:**
- Niche
- Timeframe (7/30/90 days)
- Minimum volume threshold

**Outputs:**
- Rising keywords ranked by momentum
- Growth rate (WoW, MoM, YoY)
- Trend lifecycle (emerging/growing/mature/declining)
- Related video recommendations
- **With LLM (Pro tier):** Context on why trending, how to capitalize

**Momentum Score:**
```
growth_rate = (current_volume / previous_volume - 1) * 100
momentum = growth_rate * log(absolute_volume)
trend_lifecycle = classify(momentum, historical_data)
normalized_score = (momentum - min) / (max - min) * 100
```

---

### Feature 4: Niche Comparison
**Purpose:** Compare keywords and content patterns across niches

**Inputs:**
- Primary niche (required)
- 1-3 adjacent niches to compare
- Comparison dimension (keywords, video length, upload frequency, etc.)

**Outputs:**
- Side-by-side metrics (keywords, video length, engagement)
- Gap analysis (keywords in Niche A but not B)
- Opportunity scoring
- **With LLM (Pro tier):** Strategic recommendations, positioning advice

**Comparison Dimensions:**
- Top keywords (with volume)
- Video length distribution
- Upload frequency
- Engagement patterns
- Tag strategies
- Audience maturity

---

## Architecture Overview

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│         Svelte Web Frontend                          │
│  (Dashboard, real-time progress, auth, tier limits) │
└─────────────────┬───────────────────────────────────┘
                  │
      ┌───────────┼───────────┬──────────┐
      │           │           │          │
   ┌──▼──┐  ┌────▼───┐  ┌───▼───┐  ┌──▼──┐
   │REST │  │WebSocket│  │File   │  │Auth │
   │API  │  │/SignalR │  │Upload │  │     │
   └──┬──┘  └────┬───┘  └───┬───┘  └──┬──┘
      │          │          │         │
      └──────────┼──────────┼─────────┘
                 │          │
    ┌────────────▼──────────▼─────────┐
    │  .NET 10 Backend (API Layer)    │
    │  - Authentication/JWT           │
    │  - Usage Metering               │
    │  - Plugin Discovery             │
    │  - Tier/Plan Enforcement        │
    └────────────┬────────────────────┘
                 │
    ┌────────────▼────────────────────┐
    │  Analysis Service Layer         │
    │  - KeywordAnalyzer              │
    │  - VideoAnalyzer                │
    │  - TrendDetection               │
    │  - NicheComparison              │
    │  - [Community Plugins]          │
    └────────────┬────────────────────┘
                 │
    ┌────────────▼────────────────────┐
    │  LLM Orchestration              │
    │  - Multi-provider abstraction   │
    │  - Cost tracking per call       │
    │  - Prompt templating            │
    │  - Provider selection logic     │
    └────────────┬────────────────────┘
                 │
    ┌────────────▼────────────────────┐
    │  Data Processing Layer          │
    │  - Normalization                │
    │  - Statistical analysis         │
    │  - Caching                      │
    └────────────┬────────────────────┘
                 │
    ┌────────────▼────────────────────┐
    │  External APIs                  │
    │  - YouTube Data API v3          │
    │  - OpenAI, Anthropic, Groq      │
    └────────────┬────────────────────┘
                 │
    ┌────────────▼────────────────────┐
    │  PostgreSQL Database            │
    │  - Users, Subscriptions         │
    │  - Videos, Keywords, Analysis   │
    │  - Usage logs (for billing)     │
    │  - Plugin metadata              │
    └─────────────────────────────────┘
```

### Directory Structure

```
vara/
├── README.md
├── .env.example
├── docker-compose.yml
├── appsettings.json
├── .github/
│   └── workflows/
│       └── deploy.yml              # GitHub Actions CI/CD
├── src/
│   ├── Program.cs                  # Entry point, DI setup
│   ├── Models/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Video.cs
│   │   │   ├── Keyword.cs
│   │   │   ├── Analysis.cs
│   │   │   ├── LlmCost.cs
│   │   │   └── PluginMetadata.cs
│   │   └── Dtos/
│   │       ├── KeywordAnalysisRequest.cs
│   │       └── KeywordAnalysisResponse.cs
│   ├── Data/
│   │   ├── VaraContext.cs          # EF Core DbContext
│   │   └── Migrations/
│   ├── Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── AnalysisController.cs
│   │   │   └── PluginsController.cs
│   │   └── Hubs/
│   │       └── AnalysisHub.cs      # SignalR
│   ├── Services/
│   │   ├── Analysis/
│   │   │   ├── KeywordAnalyzer.cs
│   │   │   ├── VideoAnalyzer.cs
│   │   │   ├── TrendDetection.cs
│   │   │   └── NicheComparison.cs
│   │   ├── Llm/
│   │   │   ├── ILlmProvider.cs
│   │   │   ├── OpenAiProvider.cs
│   │   │   ├── AnthropicProvider.cs
│   │   │   ├── GroqProvider.cs
│   │   │   └── LlmOrchestrator.cs
│   │   ├── YouTube/
│   │   │   ├── YouTubeClient.cs
│   │   │   └── TranscriptFetcher.cs
│   │   ├── Auth/
│   │   │   └── TokenService.cs
│   │   ├── Usage/
│   │   │   └── UsageMeter.cs
│   │   └── Plugins/
│   │       └── PluginDiscovery.cs
│   └── Utils/
│       ├── Logger.cs
│       └── Validators.cs
├── tests/
│   └── Vara.Tests.csproj
└── .gitignore

vara-frontend/
├── README.md
├── package.json
├── svelte.config.js
├── vite.config.js
├── src/
│   ├── App.svelte
│   ├── lib/
│   │   ├── components/
│   │   │   ├── Nav.svelte
│   │   │   ├── Dashboard.svelte
│   │   │   ├── AnalysisProgress.svelte
│   │   │   └── ResultsDisplay.svelte
│   │   ├── stores/
│   │   │   ├── auth.ts
│   │   │   └── analysis.ts
│   │   └── api/
│   │       ├── client.ts
│   │       └── signalr.ts
│   └── routes/
│       ├── +page.svelte
│       ├── +layout.svelte
│       ├── login/+page.svelte
│       └── analysis/
│           ├── keywords/+page.svelte
│           ├── videos/+page.svelte
│           ├── trends/+page.svelte
│           └── niches/+page.svelte
└── .gitignore

infrastructure/
├── main.tf
├── variables.tf
├── outputs.tf
└── terraform.tfvars
```

---

## Database Schema (PostgreSQL)

### Users Table
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    subscription_tier VARCHAR(50) DEFAULT 'free',  -- free, pro, enterprise
    subscription_expires_at TIMESTAMP,
    settings JSONB DEFAULT '{}'
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_subscription_tier ON users(subscription_tier);
```

### Videos Table
```sql
CREATE TABLE videos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    youtube_id VARCHAR(11) UNIQUE NOT NULL,
    title VARCHAR(255),
    description TEXT,
    channel_name VARCHAR(255),
    duration_seconds INT,
    upload_date TIMESTAMP,
    view_count BIGINT DEFAULT 0,
    like_count INT DEFAULT 0,
    comment_count INT DEFAULT 0,
    thumbnail_url VARCHAR(512),
    transcript_url VARCHAR(512),
    transcript_text TEXT,
    metadata_fetched_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT unique_user_video UNIQUE(user_id, youtube_id)
);

CREATE INDEX idx_videos_user_id ON videos(user_id);
CREATE INDEX idx_videos_upload_date ON videos(upload_date);
```

### Keywords Table
```sql
CREATE TABLE keywords (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    keyword VARCHAR(255) NOT NULL,
    niche VARCHAR(100),
    search_volume_relative SMALLINT,
    competition_score SMALLINT,
    trend_direction VARCHAR(20),
    keyword_intent VARCHAR(50),
    last_analyzed TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT unique_user_keyword UNIQUE(user_id, keyword, niche)
);

CREATE INDEX idx_keywords_user_id ON keywords(user_id);
CREATE INDEX idx_keywords_trend ON keywords(trend_direction);
```

### Analyses Table
```sql
CREATE TABLE analyses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    analysis_type VARCHAR(50) NOT NULL,
    input_params JSONB NOT NULL,
    results JSONB NOT NULL,
    llm_enhanced BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP,
    
    CONSTRAINT valid_type CHECK (analysis_type IN ('keyword', 'video', 'trend', 'niche_comparison'))
);

CREATE INDEX idx_analyses_user_id ON analyses(user_id);
CREATE INDEX idx_analyses_type ON analyses(analysis_type);
CREATE INDEX idx_analyses_expires ON analyses(expires_at);
```

### LLM Costs Table
```sql
CREATE TABLE llm_costs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    analysis_id UUID REFERENCES analyses(id),
    provider VARCHAR(50) NOT NULL,
    model VARCHAR(100) NOT NULL,
    prompt_tokens INT,
    completion_tokens INT,
    cost_usd DECIMAL(8, 6),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_llm_costs_user_id ON llm_costs(user_id);
CREATE INDEX idx_llm_costs_created ON llm_costs(created_at);
```

### Usage Logs Table
```sql
CREATE TABLE usage_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    feature VARCHAR(100) NOT NULL,
    unit_count INT DEFAULT 1,
    billing_period DATE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_usage_logs_user_id ON usage_logs(user_id);
CREATE INDEX idx_usage_logs_period ON usage_logs(billing_period);
```

### Plugin Metadata Table
```sql
CREATE TABLE plugin_metadata (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plugin_id VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    version VARCHAR(20),
    author VARCHAR(255),
    description TEXT,
    tier VARCHAR(50) DEFAULT 'free',
    required_llm_providers VARCHAR(255)[],
    ui_component_name VARCHAR(255),
    input_types VARCHAR(50)[],
    output_schema JSONB,
    plugin_url VARCHAR(512),
    enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_plugin_tier ON plugin_metadata(tier);
CREATE INDEX idx_plugin_enabled ON plugin_metadata(enabled);
```

---

## API Design

### REST Endpoints

**Authentication:**
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
```

**Analysis:**
```
POST   /api/analysis/keywords
GET    /api/analysis/keywords/:id
POST   /api/analysis/videos
GET    /api/analysis/videos/:id
POST   /api/analysis/trends
GET    /api/analysis/trends/:id
POST   /api/analysis/niche-comparison
GET    /api/analysis/niche-comparison/:id
GET    /api/analysis/history
```

**Usage & Billing:**
```
GET    /api/usage/current
GET    /api/usage/history
GET    /api/subscription
```

**Plugins:**
```
GET    /api/plugins
GET    /api/plugins/:id
POST   /api/plugins/:id/enable
POST   /api/plugins/:id/disable
GET    /api/plugins/results/:analysisId
```

### SignalR Hub: AnalysisHub

**Client → Server:**
```javascript
connection.invoke("StartAnalysis", {
    type: "keyword",
    keyword: "machine learning",
    includeInsights: true
});
```

**Server → Client:**
```javascript
connection.on("AnalysisProgress", (message) => {
    // { step: 1, stage: "Fetching YouTube data", percent: 25 }
});

connection.on("AnalysisComplete", (result) => {
    // { analysisId, data, llmInsights }
});

connection.on("AnalysisError", (error) => {
    // { message, code }
});
```

---

## Core Technologies & Rationale

### .NET 10 + C# (Minimal APIs)
**Why:**
- Type safety catches bugs on camera
- Entity Framework Core is excellent
- SignalR native (best for real-time)
- Single compiled binary for deployment
- Async/await native (critical for LLM APIs)

### PostgreSQL
**Why:**
- Complex schema support (JSONB fields, arrays)
- Expansion-ready without migration pain
- Industry standard
- Free and open-source

### Svelte + SvelteKit
**Why:**
- Small bundle size
- Less boilerplate than React
- Excellent TypeScript support
- Great for real-time apps (SignalR integration smooth)

### SignalR
**Why:**
- Native to .NET
- Automatic fallbacks (WebSocket → long polling)
- Built-in connection management
- Perfect for progress updates

### Multi-Provider LLM
**Why:**
- Different models excel at different tasks
- Cost varies significantly (Groq cheapest, Claude best quality)
- Mitigates vendor lock-in
- Easy to test and compare providers

---

## Getting Started Checklist

**Before Episode 1 Recording:**

```
Secrets & Configuration:
  ☐ GitHub Secrets configured (YOUTUBE_API_KEY, OPENAI_API_KEY, ANTHROPIC_API_KEY, GROQ_API_KEY)
  ☐ .env.example created in repo
  ☐ .env in .gitignore
  ☐ appsettings.json configured for local development

Infrastructure:
  ☐ Hetzner account setup
  ☐ Basic Terraform files created (main.tf, variables.tf, outputs.tf)
  ☐ docker-compose.yml ready (PostgreSQL + .NET API)
  ☐ SSH key pair for Hetzner deployment

GitHub Setup:
  ☐ Repository created (yourusername/vara)
  ☐ Secrets added to repo settings
  ☐ README.md with basic overview
  ☐ CONTRIBUTING.md for community guidelines
  ☐ GitHub Discussions enabled

Development Environment:
  ☐ .NET 10 SDK installed
  ☐ PostgreSQL available (docker or local)
  ☐ Git configured
  ☐ IDE setup (VS Code, Rider, or Visual Studio)

Release Planning:
  ☐ YouTube channel created/ready
  ☐ First video publish date scheduled (e.g., first Thursday monthly)
  ☐ Recording setup tested (audio, screen capture quality)
  ☐ Backup strategy for episode recordings

Documentation:
  ☐ Copy spec to /docs in repo
  ☐ Episode roadmap linked in README
  ☐ Architecture diagrams saved as images
```

---

## What "Building in Public" Means

**Monthly video format (15-25 minutes):**

1. **What was planned vs. what happened** (2 min)
   - "Planned: Finish LLM layer. Actually: Still debugging async issues."
   - Honest about blockers and learnings

2. **Code walkthrough of main feature** (12-18 min)
   - Live demo of working code
   - Show tests passing
   - Explain design decisions made

3. **What's breaking, what's next** (3-5 min)
   - Known issues
   - Next month's plan
   - Call for feedback

**This approach:**
- Forces weekly progress toward 20-hr commitment
- Creates authentic feedback loop (community suggests things mid-build)
- Shows debugging and refactoring (people respect this)
- Builds audience throughout year (not waiting 12 months)

---

## Success Metrics

**By Month 12:**
- ✅ All 10 core episodes released
- ✅ Working SaaS application
- ✅ Plugin system functional
- ✅ 50+ GitHub stars
- ✅ 10+ community members engaged

**By Month 18:**
- ✅ Real billing integrated (Stripe)
- ✅ 100+ registered users
- ✅ 20+ community plugins
- ✅ $2-5K/month revenue potential

**By Year 2:**
- ✅ Self-sustaining business
- ✅ Potential for contractor help
- ✅ 1K+ users
- ✅ Industry recognition

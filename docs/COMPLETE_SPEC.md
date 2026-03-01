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
- **With LLM (Creator tier):** Strategic insights—why creators should care, positioning strategies, content gaps, video angle ideas

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
- **With LLM (Creator tier):** Why these patterns work, structural recommendations

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
- **With LLM (Creator tier):** Context on why trending, how to capitalize

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
- **With LLM (Creator tier):** Strategic recommendations, positioning advice

**Comparison Dimensions:**
- Top keywords (with volume)
- Video length distribution
- Upload frequency
- Engagement patterns
- Tag strategies
- Audience maturity

---

### Feature 5: Outlier Detection
**Purpose:** Find videos that dramatically overperformed their channel size — the
"niche arbitrage" signal that reveals underserved topics before they go mainstream

**Inputs:**
- Keyword or topic (required)
- Minimum outlier ratio (default: 5× — views relative to subscriber count)
- Maximum channel size (default: 500K subscribers — filters out established creators)
- Max video age in days (default: 730)
- Include LLM insights (optional — Creator tier only)

**Outputs:**
- Ranked list of outlier videos with ratio, score, and strength classification
- Per-video metrics: view count, subscriber count, outlier ratio, upload age
- Summary stats: total outliers found, strong outlier count, average ratio
- Common patterns observed across outliers (title format, length, recency)
- **With LLM (Creator tier):** Why each outlier succeeded and how to capitalize

**Outlier Formula:**
```
outlier_ratio   = video.view_count / channel.subscriber_count

Strong Outlier:   ratio ≥ 10×  🔥
Moderate Outlier: ratio ≥ 5×   ⚡
Mild Outlier:     ratio ≥ 2×   📈

final_score = outlier_ratio
            × recency_bonus   (videos < 90 days old score higher)
            × age_penalty     (videos > 2 years old score lower)
            × size_penalty    (larger channels reduce signal strength)
```

**Tier:** Free (base analysis) — LLM insights require Creator tier (2 credits)
**Quota cost:** 102 units per run (100 search + 1 batch video fetch + 1 batch channel fetch)
**Reference implementation:** `plugins/outlier-detection/` — used as the canonical
community plugin example in Episode 10

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

### Cost Engineering Principles

VARA has three cost centers that must be actively managed to keep unit economics
healthy at the $7/channel/month price point.

**YouTube Data API — Two-Pass Strategy**

The daily free quota is 10,000 units. `search.list` costs 100 units per call.
`videos.list` costs 1 unit per call regardless of how many IDs are batched (up to 50).
Never fetch metadata inside a search loop. Always separate ID discovery from
metadata retrieval:

```csharp
// Pass 1: search returns IDs only (100 quota units)
var videoIds = await _youtube.SearchAsync(keyword, maxResults: 20);

// Pass 2: batch-fetch full metadata in one call (1 quota unit)
var details = await _youtube.GetVideosAsync(videoIds);
```

Defined quota budget per analysis type:

```csharp
public static class YouTubeQuotaBudgets
{
    public const int DailyTotal   = 10_000;
    public const int DailyUsable  = 8_000;  // 2,000 reserved for overhead

    public static Dictionary<string, int> PerAnalysis = new()
    {
        ["keyword_search"]    = 101,  // 100 (search) + 1 (batch details)
        ["video_analysis"]    = 101,
        ["channel_sync_100"]  = 102,  // 2 playlist pages + 1 batch details
        ["trend_detection"]   = 303,  // 3 keyword searches
        ["outlier_detection"] = 102,  // 100 (search) + 1 (videos) + 1 (channels)
    };
}
```

**LLM Pricing — Config-Driven, Never Hardcoded**

Provider pricing changes without notice. All cost calculations must read from
`IConfiguration` so prices can be updated via config without a redeploy:

```json
"Llm": {
  "Pricing": {
    "claude-3-5-sonnet-20241022": { "InputPerMToken": 3.00,  "OutputPerMToken": 15.00 },
    "gpt-4o":                     { "InputPerMToken": 2.50,  "OutputPerMToken": 10.00 },
    "gpt-4o-mini":                { "InputPerMToken": 0.15,  "OutputPerMToken": 0.60  },
    "mixtral-8x7b-32768":         { "InputPerMToken": 0.27,  "OutputPerMToken": 0.27  }
  }
}
```

```csharp
private decimal CalculateCost(string model, int inputTokens, int outputTokens)
{
    var section = _config.GetSection($"Llm:Pricing:{model}");
    var inputRate  = section.GetValue<decimal>("InputPerMToken");
    var outputRate = section.GetValue<decimal>("OutputPerMToken");

    return (inputTokens  / 1_000_000m * inputRate)
         + (outputTokens / 1_000_000m * outputRate);
}
```

Actual cost per task type at current Claude 3.5 Sonnet pricing:

| Task                  | ~Input Tokens | ~Output Tokens | Est. Cost |
|-----------------------|--------------|----------------|-----------|
| Keyword insights      | 300          | 500            | ~$0.009   |
| Video analysis        | 400          | 600            | ~$0.010   |
| Transcript analysis   | 8,000        | 1,000          | ~$0.039   |
| Niche comparison      | 500          | 800            | ~$0.014   |

**Caching TTLs**

All external API responses must be cached. LLM responses are cached by a
SHA-256 hash of the input prompt so identical queries across different users
share the same cached response:

```csharp
public static class CacheTtls
{
    public static readonly TimeSpan VideoMetadata      = TimeSpan.FromHours(24);
    public static readonly TimeSpan SearchResults      = TimeSpan.FromHours(6);
    public static readonly TimeSpan Transcript         = TimeSpan.FromDays(30);
    public static readonly TimeSpan ChannelStats       = TimeSpan.FromHours(12);
    public static readonly TimeSpan KeywordInsights    = TimeSpan.FromDays(7);
    public static readonly TimeSpan TranscriptAnalysis = TimeSpan.FromDays(14);
    public static readonly TimeSpan TrendDetection     = TimeSpan.FromHours(3);
    public static readonly TimeSpan OutlierDetection   = TimeSpan.FromHours(6);
}
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
│   │   │   ├── TrackedChannel.cs
│   │   │   ├── Analysis.cs
│   │   │   ├── LlmCost.cs
│   │   │   ├── PluginMetadata.cs
│   │   │   └── UserApiKey.cs         -- Phase 3: BYOT key storage entity (scaffolded)
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
│   │   │   ├── IYouTubeClient.cs
│   │   │   ├── YouTubeClient.cs
│   │   │   ├── TranscriptFetcher.cs
│   │   │   └── VideoCache.cs
│   │   ├── Auth/
│   │   │   └── TokenService.cs
│   │   ├── Usage/
│   │   │   └── UsageMeter.cs
│   │   ├── ApiKeys/
│   │   │   └── UserApiKeyService.cs  -- Phase 3: BYOT key management (scaffolded)
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
    subscription_tier VARCHAR(50) DEFAULT 'free',  -- free, creator
    subscription_expires_at TIMESTAMP,             -- when current paid period ends
    paddle_customer_id  VARCHAR(100),              -- Paddle customer ID (set on first purchase)
    trial_ends_at TIMESTAMP,                       -- reserved for future trial feature
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
    youtube_id VARCHAR(11) NOT NULL,
    title VARCHAR(255),
    description TEXT,
    channel_name VARCHAR(255),
    channel_id VARCHAR(24),              -- YouTube channel ID (UCxxxxxxx...)
    duration_seconds INT,
    upload_date TIMESTAMP,
    view_count BIGINT DEFAULT 0,
    like_count INT DEFAULT 0,
    comment_count INT DEFAULT 0,
    thumbnail_url VARCHAR(512),
    transcript_text TEXT,                -- transcript_url removed; text stored directly
    metadata_fetched_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_user_video UNIQUE(user_id, youtube_id)
);

CREATE INDEX idx_videos_user_id ON videos(user_id);
CREATE INDEX idx_videos_channel_id ON videos(channel_id);
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

### Tracked Channels Table
```sql
-- Stores channels a user is monitoring — either their own or a competitor's.
-- Ownership is self-reported until verified via Google OAuth (Episode 6+).
CREATE TABLE tracked_channels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    youtube_channel_id VARCHAR(24) NOT NULL,    -- e.g. UCBcRF18a7Qf58cCRy5xuWwQ
    handle VARCHAR(100),                        -- e.g. @mkbhd (optional, for display)
    display_name VARCHAR(255),                  -- channel title from YouTube API
    thumbnail_url VARCHAR(512),
    subscriber_count BIGINT,
    video_count INT,
    total_view_count BIGINT,
    is_owner BOOLEAN NOT NULL DEFAULT FALSE,    -- self-reported: "this is my channel"
    is_verified BOOLEAN NOT NULL DEFAULT FALSE, -- TRUE only after Google OAuth confirms ownership
    last_synced_at TIMESTAMP,                   -- when we last crawled this channel's videos
    added_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_user_channel UNIQUE(user_id, youtube_channel_id)
);

CREATE INDEX idx_tracked_channels_user_id ON tracked_channels(user_id);
CREATE INDEX idx_tracked_channels_youtube_id ON tracked_channels(youtube_channel_id);
CREATE INDEX idx_tracked_channels_is_owner ON tracked_channels(user_id, is_owner);
```

**Design notes:**
- One user can track multiple channels (their own + competitors).
- `is_owner = TRUE` means the user claims ownership — gates owner-specific features.
- `is_verified` flips to `TRUE` only after OAuth (`channels?mine=true`) confirms the claim — added in a later episode when the YouTube Analytics API is integrated.
- Public metrics (subscriber count, view count) are refreshed on `last_synced_at` cadence via the YouTube Data API (no OAuth needed).
- Private metrics (CTR, impressions, average view duration) require OAuth — out of scope until Episode 6+.

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
    
    CONSTRAINT valid_type CHECK (analysis_type IN (
        'keyword', 'video', 'trend', 'niche_comparison', 'outlier'
    ))
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
    is_byot BOOLEAN NOT NULL DEFAULT FALSE,    -- TRUE when user's own API key was used
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

### Channel Subscriptions Table
```sql
-- Tracks active per-channel subscriptions
CREATE TABLE channel_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    channel_id UUID NOT NULL REFERENCES tracked_channels(id) ON DELETE CASCADE,
    paddle_subscription_id VARCHAR(100) NOT NULL,
    billing_interval VARCHAR(20) NOT NULL,     -- 'monthly' or 'annual'
    status VARCHAR(50) NOT NULL,               -- 'active', 'cancelled', 'past_due'
    current_period_ends_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_channel_subscription UNIQUE(channel_id)
);

CREATE INDEX idx_channel_subs_user_id ON channel_subscriptions(user_id);
CREATE INDEX idx_channel_subs_status ON channel_subscriptions(status);
CREATE INDEX idx_channel_subs_expires ON channel_subscriptions(current_period_ends_at);
```

### Credit Grants Table
```sql
-- Tracks purchased Research Pack credits (Phase 2)
CREATE TABLE credit_grants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    credits INT NOT NULL,
    credits_remaining INT NOT NULL,
    source VARCHAR(100) NOT NULL,              -- e.g. 'research_pack:standard'
    paddle_transaction_id VARCHAR(100),
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP                       -- NULL = no expiry
);

CREATE INDEX idx_credit_grants_user_id ON credit_grants(user_id);
CREATE INDEX idx_credit_grants_remaining ON credit_grants(user_id, credits_remaining)
    WHERE credits_remaining > 0;
```

### User API Keys Table
```sql
-- Scaffolded for Phase 3: Bring Your Own Token (BYOT)
-- This table is created during MVP but no application logic reads or writes it
-- until Phase 3 is implemented. Creating it now avoids a migration against live data later.
CREATE TABLE user_api_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    provider VARCHAR(50) NOT NULL,             -- 'Anthropic', 'OpenAI', 'Groq'
    encrypted_key TEXT NOT NULL,               -- AES-256 encrypted, never stored plain
    key_hint VARCHAR(10),                      -- last 4 chars of key for UI display e.g. '...a3kF'
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    added_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP,
    last_validated_at TIMESTAMP,               -- when we last confirmed key works
    last_validation_status VARCHAR(20),        -- 'valid', 'invalid', 'unknown'

    CONSTRAINT unique_user_provider_key UNIQUE(user_id, provider)
);

CREATE INDEX idx_user_api_keys_user_id ON user_api_keys(user_id);
CREATE INDEX idx_user_api_keys_provider ON user_api_keys(user_id, provider)
    WHERE is_active = TRUE;
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

**Channels:**
```
POST   /api/channels                  -- Add a channel to track (by handle or ID)
GET    /api/channels                  -- List all tracked channels for current user
GET    /api/channels/{id}             -- Get a single tracked channel + summary stats
DELETE /api/channels/{id}             -- Stop tracking a channel
POST   /api/channels/{id}/sync        -- Trigger a manual crawl of all videos
GET    /api/channels/{id}/videos      -- List videos crawled from this channel
GET    /api/channels/{id}/stats       -- Aggregated stats (avg views, top 5, bottom 5, cadence)
```

**Channel ownership verification (Episode 6+, requires OAuth):**
```
GET    /api/channels/connect/google   -- Begin Google OAuth flow
GET    /api/channels/connect/callback -- OAuth callback; sets is_verified = true
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

**Billing (Paddle):**
```
GET    /api/billing/status                    -- subscription status + credit balance
GET    /api/billing/credits                   -- credit grant history
POST   /api/billing/webhook                   -- Paddle webhook receiver
GET    /api/billing/packs                     -- available Research Pack products
POST   /api/billing/checkout/subscription     -- initiate Paddle subscription checkout
POST   /api/billing/checkout/pack             -- initiate Research Pack checkout
DELETE /api/billing/subscription/{channelId}  -- cancel channel subscription
```

**API Key Management (Phase 3 — BYOT):**
```
GET    /api/settings/api-keys                     -- list connected providers (hint only, never full key)
POST   /api/settings/api-keys                     -- add or replace a provider key
DELETE /api/settings/api-keys/{provider}          -- remove a provider key
POST   /api/settings/api-keys/{provider}/validate -- test key before saving
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

## Phase 3: Bring Your Own Token (BYOT)

**Timeline:** 9–12 months post-launch (after Phase 2 Research Packs)
**Target users:** Technical creators, developers, power users already paying
for Anthropic/OpenAI API access
**Business impact:** BYOT users are the highest-margin customers — full
channel subscription revenue, zero LLM inference cost to VARA

---

### Overview

BYOT allows Creator tier subscribers to connect their own Anthropic, OpenAI,
or Groq API keys. When a BYOT key is active for a provider, VARA uses it for
that user's LLM calls instead of VARA's managed keys. The user pays their
AI provider directly; VARA charges nothing extra.

**What changes for BYOT users:**
- AI credit cap is bypassed entirely (they pay per-token to their provider)
- No Research Pack needed for heavy usage
- Model selection can be more flexible (they can use Claude Opus if they want)
- VARA's LLM cost for this user drops to $0

**What stays the same:**
- Channel subscription ($7/month) — the product value is the platform, not inference
- All other VARA features, limits, and billing

---

### Security Architecture

API key security is the most critical concern for BYOT. The approach:

**Encryption:**
Keys are encrypted with AES-256-GCM before storage. The encryption key is
derived from a combination of the user's ID and a server-side secret, meaning:
- VARA's database alone cannot decrypt the key (server secret required)
- A breach of the server secret alone cannot decrypt without user IDs
- VARA employees cannot read user keys in the normal operational path

```csharp
public class ApiKeyEncryptionService
{
    private readonly byte[] _serverSecret; // from environment, never in config files

    public string Encrypt(string apiKey, Guid userId)
    {
        // Derive a unique key per user using HKDF
        var keyMaterial = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ikm: _serverSecret,
            outputLength: 32,
            salt: userId.ToByteArray(),
            info: Encoding.UTF8.GetBytes("vara-api-key-encryption"));

        using var aes = new AesGcm(keyMaterial, AesGcm.TagByteSizes.MaxSize);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var plaintext = Encoding.UTF8.GetBytes(apiKey);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Store as: base64(nonce) + "." + base64(ciphertext) + "." + base64(tag)
        return $"{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(ciphertext)}.{Convert.ToBase64String(tag)}";
    }

    public string Decrypt(string encryptedKey, Guid userId)
    {
        var parts = encryptedKey.Split('.');
        var nonce = Convert.FromBase64String(parts[0]);
        var ciphertext = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);

        var keyMaterial = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            ikm: _serverSecret,
            outputLength: 32,
            salt: userId.ToByteArray(),
            info: Encoding.UTF8.GetBytes("vara-api-key-encryption"));

        using var aes = new AesGcm(keyMaterial, AesGcm.TagByteSizes.MaxSize);
        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
```

**Key validation before storage:**
Before saving any key, make a minimal test call (e.g., a 1-token completion)
to confirm it works. Store `last_validation_status` and surface clearly in UI.

```csharp
public class UserApiKeyService
{
    public async Task<KeyValidationResult> ValidateAndStoreAsync(
        Guid userId,
        string provider,
        string rawApiKey)
    {
        // Step 1: Validate key works before storing
        var validation = await ValidateKeyAsync(provider, rawApiKey);
        if (!validation.IsValid)
            return validation; // Return error, don't store

        // Step 2: Encrypt
        var encrypted = _encryptionService.Encrypt(rawApiKey, userId);

        // Step 3: Store (upsert — one key per provider per user)
        await _db.UserApiKeys.UpsertAsync(new UserApiKey
        {
            UserId = userId,
            Provider = provider,
            EncryptedKey = encrypted,
            KeyHint = "..." + rawApiKey[^4..], // last 4 chars only
            IsActive = true,
            AddedAt = DateTime.UtcNow,
            LastValidatedAt = DateTime.UtcNow,
            LastValidationStatus = "valid"
        });

        return KeyValidationResult.Success();
    }

    private async Task<KeyValidationResult> ValidateKeyAsync(
        string provider,
        string apiKey)
    {
        try
        {
            // Use factory to create temporary provider with this key
            var tempProvider = _providerFactory.CreateWithUserKey(provider, apiKey);
            await tempProvider.GenerateAsync(
                "Say 'ok'",
                new LlmOptions { MaxTokens = 5 });
            return KeyValidationResult.Success();
        }
        catch (AuthenticationException)
        {
            return KeyValidationResult.Failure("Invalid API key.");
        }
        catch (Exception ex)
        {
            return KeyValidationResult.Failure($"Could not validate key: {ex.Message}");
        }
    }
}
```

---

### Orchestrator BYOT Flow (Phase 3 completion of scaffold)

In Phase 3, `LlmProviderFactory.CreateWithUserKey` is implemented:

```csharp
public ILlmProvider CreateWithUserKey(string providerName, string apiKey)
{
    return providerName switch
    {
        "Anthropic" => new AnthropicProvider(apiKey, _httpClientFactory, _config),
        "OpenAI"    => new OpenAiProvider(apiKey, _httpClientFactory, _config),
        "Groq"      => new GroqProvider(apiKey, _httpClientFactory, _config),
        _ => throw new ArgumentException($"BYOT not supported for provider: {providerName}")
    };
}
```

The `AnalysisService` layer resolves whether to populate `ByotApiKey` in the
`LlmExecutionContext` before calling the orchestrator:

```csharp
public class ByotContextResolver
{
    public async Task<LlmExecutionContext> ResolveAsync(
        Guid userId,
        string taskType)
    {
        // Determine preferred provider for this task
        var preferredProvider = _config.TaskProviderMapping
            .GetValueOrDefault(taskType, "OpenAI");

        // Check if user has a BYOT key for this provider
        var keyRecord = await _db.UserApiKeys
            .FirstOrDefaultAsync(k =>
                k.UserId == userId &&
                k.Provider == preferredProvider &&
                k.IsActive);

        if (keyRecord is null)
        {
            // No BYOT key — use VARA managed inference
            return new LlmExecutionContext
            {
                UserId = userId,
                TaskType = taskType,
                ByotApiKey = null,
                ByotProvider = null
            };
        }

        // Decrypt and use BYOT key
        var decryptedKey = _encryptionService.Decrypt(keyRecord.EncryptedKey, userId);

        return new LlmExecutionContext
        {
            UserId = userId,
            TaskType = taskType,
            ByotApiKey = decryptedKey,
            ByotProvider = preferredProvider
        };
    }
}
```

---

### Credit Enforcement With BYOT

BYOT users bypass the monthly credit cap entirely. The `PlanEnforcer` checks
for an active BYOT key before applying credit limits:

```csharp
public async Task EnforceLlmAccessAsync(
    Guid userId,
    string channelId,
    string taskType)
{
    var limits = await GetTierLimitsAsync(userId);

    if (!limits.CanAccessLlm)
        throw new FeatureAccessDeniedException(
            "AI insights require the Creator tier.");

    // BYOT users skip credit enforcement
    var hasByotKey = await _db.UserApiKeys
        .AnyAsync(k => k.UserId == userId && k.IsActive);

    if (hasByotKey) return; // unlimited via their own key

    // Managed users: enforce weighted credit cap as normal
    var weight = LlmCallWeights.ByTaskType.GetValueOrDefault(taskType, 1);
    var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
    var used = await _usageRepo.GetWeightedCreditsUsedAsync(userId, channelId, currentMonth);

    var remaining = limits.MonthlyWeightedCredits - used;
    if (remaining >= weight) return;

    // Check Research Pack credits
    var packCredits = await _creditRepo.GetAvailablePackCreditsAsync(userId);
    if (packCredits >= weight)
    {
        await _creditRepo.DeductPackCreditsAsync(userId, weight);
        return;
    }

    throw new CreditLimitExceededException(
        "Monthly AI credits exhausted. Purchase a Research Pack, " +
        "or connect your own API key in Settings for unlimited access.");
}
```

Note the updated error message — it surfaces BYOT as an alternative to purchasing
packs. This is intentional: power users who hit limits repeatedly should discover
BYOT as the natural next step.

---

### Settings UI (Phase 3)

```svelte
<!-- ApiKeySettings.svelte -->
<script>
  import { onMount } from 'svelte';

  let keys = {};  // { Anthropic: { hint: '...a3kF', status: 'valid' }, ... }
  const providers = ['Anthropic', 'OpenAI', 'Groq'];

  async function addKey(provider) {
    const rawKey = prompt(`Paste your ${provider} API key:`);
    if (!rawKey) return;

    const res = await fetch('/api/settings/api-keys', {
      method: 'POST',
      body: JSON.stringify({ provider, apiKey: rawKey }),
      headers: { 'Content-Type': 'application/json' }
    });

    if (res.ok) {
      const data = await res.json();
      keys[provider] = { hint: data.keyHint, status: 'valid' };
    } else {
      alert(`Could not validate key: ${(await res.json()).error}`);
    }
  }

  async function removeKey(provider) {
    await fetch(`/api/settings/api-keys/${provider}`, { method: 'DELETE' });
    delete keys[provider];
    keys = keys;
  }
</script>

<section class="api-keys">
  <h2>🔑 Bring Your Own API Key</h2>
  <p>
    Connect your own AI provider keys to use unlimited analyses.
    Your keys are encrypted and never shared. You'll be billed directly
    by your provider — VARA charges nothing extra.
  </p>

  <div class="provider-list">
    {#each providers as provider}
      <div class="provider-row">
        <span class="provider-name">{provider}</span>

        {#if keys[provider]}
          <span class="key-hint">{keys[provider].hint}</span>
          <span class="status {keys[provider].status}">
            {keys[provider].status === 'valid' ? '✓ Active' : '⚠ Invalid'}
          </span>
          <button class="remove" on:click={() => removeKey(provider)}>Remove</button>
        {:else}
          <span class="not-connected">Not connected</span>
          <button class="connect" on:click={() => addKey(provider)}>
            Connect {provider}
          </button>
        {/if}
      </div>
    {/each}
  </div>

  <p class="note">
    💡 When a key is active, your monthly AI credit cap is removed for that provider.
    <a href="/docs/byot">Learn more</a>
  </p>
</section>
```

---

### Phase 3 Episode Candidate

This feature is documented in the episode roadmap as **Bonus Episode E**.

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
  ☐ user_api_keys table created in Episode 1 DB setup (scaffolded for Phase 3 BYOT — empty until Phase 3)

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
- ✅ Real billing integrated (Paddle)
- ✅ 100+ registered users
- ✅ 20+ community plugins
- ✅ $2-5K/month revenue potential

**By Year 2:**
- ✅ Self-sustaining business
- ✅ Potential for contractor help
- ✅ 1K+ users
- ✅ Industry recognition

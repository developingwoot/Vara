Plan: Channel Intelligence Hub
Context
Channels are buried under Settings, making them feel like a configuration task rather than a core feature. The goal is to elevate Channels into a first-class intelligence section — giving creators a clear picture of how their channel is performing, where the gaps are vs their own top content, and specific improvements they can make. This replaces a config-style page with a strategic command center.

The scope has two layers:

Channel Hub + Audits — Restructure navigation, Quick Scan, Deep Dive, Video Audit page
YouTube Analytics OAuth — Connect the channel owner's Google account to unlock rich private metrics (CTR, watch time, impressions, avg view %)
Navigation Changes
Remove "Channels" tab from Settings (keep only "Account")
Add "Channels" as a top-level sidebar nav item (after Dashboard, before the Analyze group)
Channel management (add/delete) moves into the Channels hub
Frontend Pages
/channels — Channel Hub
My Channels section (isOwner=true): thumbnail, name, subs, VARA health score badge (Bronze/Silver/Gold), key earned badges, "View Audit →"
Tracked Channels section (competitors): channel info, "Compare Video →"
Add Channel form (identical to current settings/channels form)
If a channel hasn't been synced: show "Sync to unlock analysis" prompt
If user hasn't connected YouTube Analytics: show "Connect YouTube for rich metrics" banner
/channels/[id] — Channel Audit
VARA's Assessment

Score 0–100 computed from metrics vs channel's own historical top performers
1-2 sentence plain-English verdict (template-based, no LLM cost for Quick Scan)
Earned badge display (achievements + performance tiers)
Quick Scan — Recent vs Your Best
Compares the channel's last 5 videos against its top 5 all-time videos. All fields are derived from public YouTube API data unless YouTube Analytics is connected.

Metric	Source
Views	Public API
Likes + Comments (engagement rate)	Public API
Upload frequency / consistency	Public API (upload dates)
Duration patterns	Public API
Niche alignment (title keyword match)	Public API + niche metadata
CTR (impression click-through rate)	YouTube Analytics OAuth only
Avg view duration / watch time	YouTube Analytics OAuth only
Avg view percentage	YouTube Analytics OAuth only
If metrics are locked, show them greyed out with a "Connect YouTube →" link.

Priorities panel — Top 3 ranked actions, color-coded by severity (critical 🔴, improve 🟡, maintain 🟢). Generated from the gap between recent and top-performing videos.

Badges & Awards
Achievement badges:

consistent-publisher (Bronze→Gold): ≥4/mo for 3+ months
century-club (Silver): ≥100 synced videos
rising-star (Bronze): >10K subscribers
Performance badges (relative to their own best):

on-a-streak: recent videos performing within 20% of channel avg
top-performer: recent video beats channel median by 2x+
Deep Dive (Creator tier) — button at bottom of audit page

Fetches transcript of most recent video + the channel's highest-performing outlier video
LLM compares them: pacing differences, storytelling structure, hook quality, CTA approach
Returns what the top video does differently and concrete ways to replicate it in future videos
/channels/[id]/videos/[videoId] — Per-Video Audit
Reachable from the audit page's top/recent video list. Shows:

Video metadata (title, views, likes, comments, duration)
YouTube Analytics metrics if connected (CTR, watch time, avg view %)
Transcript stats (word count, reading time, transcript available flag)
LLM analysis (Creator tier): hooks, pacing, CTAs, content gaps, what's working — reuses ITranscriptAnalysisService.AnalyzeAsync which already exists
/channels/[id]/compare — Video Comparison (Creator tier)
Pick one of your synced videos from the left
Pick a tracked competitor channel + one of their synced videos on the right
Channels must be synced first; if not, show sync prompt
LLM analyzes both transcripts: pacing comparison, hook styles, where yours excels, where to improve
YouTube Analytics OAuth Integration
Backend
New OAuth endpoints in ChannelAuditEndpoints.cs (or separate YouTubeOAuthEndpoints.cs):

GET /api/youtube/oauth/connect — redirects user to Google OAuth consent screen
Scopes: yt-analytics.readonly, youtube.readonly
GET /api/youtube/oauth/callback — handles OAuth callback, stores tokens
DELETE /api/youtube/oauth/disconnect — revokes and deletes tokens
New DB entity YouTubeOAuthToken:

UserId (FK)
AccessToken (encrypted)
RefreshToken (encrypted)
ExpiresAt
ConnectedAt
YoutubeChannelId (which channel this token applies to)
New service YouTubeAnalyticsClient.cs in Services/YouTube/:

GetVideoAnalyticsAsync(videoId, startDate, endDate) → CTR, watch time, avg view %
Handles token refresh via Polly (existing retry infrastructure)
Uses https://youtubeanalytics.googleapis.com/v2/reports
Migration — new EF Core migration for YouTubeOAuthTokens table

Frontend
Connect YouTube button/banner in channel hub and audit pages
After connecting, analytics metrics unlock automatically on next page load
Disconnect option in /settings/account
Backend Audit Services
New: ChannelAuditService.cs in Services/Analysis/

QuickScanAsync(Guid userId, Guid channelId) → ChannelQuickScanResult
DeepAuditAsync(Guid userId, Guid channelId) → ChannelDeepAuditResult  [Creator]
CompareVideosAsync(Guid userId, string videoId1, string videoId2) → VideoComparisonResult  [Creator]
Quick Scan logic:

Fetch channel + synced videos from DB
Split into "recent 5" vs "top 5 by views"
Compute gap metrics (views, engagement, frequency)
If YouTube Analytics connected: pull CTR/watch time for those videos
Compute health score (weighted average of gap percentages)
Generate priorities from gaps
Evaluate badge eligibility
Build template-based VARA assessment text
New Prompt Templates in PromptTemplates.cs
ChannelDeepAudit(recentTranscript, recentTitle, outlierTranscript, outlierTitle) — what the outlier does differently
VideoComparison(transcript1, title1, transcript2, title2) — pacing, hooks, CTAs, improvements
Key Files
Action	File
Modify	src/vara-frontend/src/lib/components/Sidebar.svelte
Modify	src/vara-frontend/src/routes/(app)/settings/+layout.svelte
Delete	src/vara-frontend/src/routes/(app)/settings/channels/+page.svelte
Create	src/vara-frontend/src/routes/(app)/channels/+page.svelte
Create	src/vara-frontend/src/routes/(app)/channels/[id]/+page.svelte
Create	src/vara-frontend/src/routes/(app)/channels/[id]/compare/+page.svelte
Create	src/vara-frontend/src/routes/(app)/channels/[id]/videos/[videoId]/+page.svelte
Create	src/Vara.Api/Services/Analysis/ChannelAuditService.cs
Create	src/Vara.Api/Services/YouTube/YouTubeAnalyticsClient.cs
Create	src/Vara.Api/Endpoints/ChannelAuditEndpoints.cs
Create	src/Vara.Api/Endpoints/YouTubeOAuthEndpoints.cs
Create	EF Core migration for YouTubeOAuthTokens table
Modify	src/Vara.Api/Services/Analysis/PromptTemplates.cs
Modify	src/Vara.Api/Program.cs
Reuse Existing
Badge.svelte — badge chips
TierGate.svelte — Creator tier gating
EmptyState.svelte — no channels state
ErrorAlert.svelte — error handling
ITranscriptAnalysisService + existing GetOrFetchTranscriptAsync logic — transcript fetching
PlanEnforcer — Creator tier enforcement in audit service
ILlmOrchestrator — LLM calls
TranscriptFetcher — YouTube transcript fetching
fetchApi client — all frontend API calls
Polly retry policies (existing) — wrap YouTube Analytics API calls
Implementation Order (recommended)
Navigation restructure + Channel Hub (move channels, new /channels page)
Quick Scan audit (metadata-only, no OAuth)
Deep Dive + Video Comparison (LLM, uses existing transcript infra)
Per-video audit page (reuses existing TranscriptAnalysisService)
YouTube Analytics OAuth (unlocks locked metrics in Quick Scan)
Verification
Start the app (docker compose up in infrastructure/)
Verify /settings only shows Account tab
Verify sidebar shows "Channels" and links to /channels
Add a channel, sync it → verify channel hub shows health score and badges
Open channel audit → verify Quick Scan shows recent vs top metrics with gap indicators
As Creator: run Deep Dive → verify LLM comparison of recent vs outlier video
Open a video from the audit → verify per-video transcript analysis
Go to Compare, select your video + competitor video → verify comparison output
Click "Connect YouTube" → complete OAuth flow → verify CTR/watch time metrics unlock
Run dotnet test → all 122 tests should pass
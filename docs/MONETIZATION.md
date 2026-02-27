# VARA: Monetization Strategy
## From Community to Sustainable Business

---

## Core Principle

**Open-source the entire product. Monetize convenience, compute, and premium features.**

The open-source version is NOT limited. Users can self-host everything for free. The SaaS
version charges for:
- Managed hosting (no setup needed)
- Compute costs (LLM API calls)
- Premium features (coming later)
- Enterprise support

---

## Merchant of Record: Paddle

VARA uses **Paddle** as its payment processor and merchant of record. This handles:
- VAT collection and remittance (EU, UK, Australia, etc.)
- US state sales tax compliance
- Global currency support
- Subscription and one-time purchase management

**Paddle fee structure:**
- Subscriptions: ~5% + $0.50 per transaction
- One-time purchases: ~5% + $0.50 per transaction
- Annual billing: flat fee becomes proportionally cheaper (one $0.50 charge vs. 12)

**Why Paddle over Stripe:** As a solo developer, Stripe requires you to register for
sales tax in every US state where you have customers (economic nexus), manage VAT
registration in the EU, and handle compliance yourself. Paddle assumes that liability
entirely. The higher fee (vs. Stripe's 2.9% + $0.30) is worth the compliance offset
at this scale.

---

## Competitive Context

VARA is priced to undercut the meaningful AI tiers of existing tools:

| Tool          | Entry Paid Tier | Real AI Features Start At |
|---------------|-----------------|---------------------------|
| VidIQ         | ~$10/month      | $25/month (AI coaching)   |
| TubeBuddy     | ~$5/month       | $49/month (AI features)   |
| **VARA**      | **$7/month**    | **$7/month**              |

Target customer: a creator running 1–2 channels seriously, frustrated that competitor
AI features are gated behind $25–$49/month tiers, technical enough to appreciate a
transparently-built open-source tool.

---

## Tier Structure

### Free — $0/month

**What's included:**
- 1 channel tracked
- Unlimited channel syncs (metadata only)
- Basic video metadata dashboard (views, likes, duration, upload cadence)
- Channel stats (avg views, top/bottom 5 videos, posts per month)
- Raw keyword search results (no AI scoring or insights)
- Raw trend data (no LLM context)
- Community plugins (free tier only)

**What's excluded:**
- All LLM-powered insights
- Transcript fetching and analysis
- Competition scoring
- Niche comparison
- More than 1 channel

**Why this costs the developer nothing:**
Channel sync for a 200-video channel costs ~5 YouTube quota units, cached for 12 hours.
No LLM calls are made. Storage is negligible. At 1,000 free users this consumes
~5,000 quota units/day against a 10,000 daily budget. The free tier is an acquisition
engine, not a cost center.

---

### Creator — $7/month per channel  |  $70/year per channel (save 17%)

**What's included:**
- Everything in Free
- AI-powered keyword insights
- Video pattern analysis with LLM
- Transcript fetching and analysis
- Niche comparison
- **20 weighted AI credits per channel per month (hard cap — no overage charges)**
- Community plugins (free + creator tier)

**Hard cap rationale:** At the $7 price point, surprise overage charges would feel
disproportionate and generate support burden. Users who exhaust their credits wait
until next month (free) or purchase a Research Pack (see Phase 2). No automatic
charges beyond the subscription.

**Credit weights by task:**

```csharp
public static class LlmCallWeights
{
    public static Dictionary<string, int> ByTaskType = new()
    {
        ["KeywordInsights"]    = 2,
        ["VideoInsights"]      = 2,
        ["NicheComparison"]    = 4,
        ["TranscriptAnalysis"] = 8,
    };
}
```

**Credit cost reference (shown to users in UI):**

| Analysis Type         | Credits Used | What You Get                         |
|-----------------------|-------------|--------------------------------------|
| Keyword AI insight    | 2 credits   | Strategic positioning, angle ideas   |
| Video pattern analysis| 2 credits   | Pattern detection, recommendations   |
| Niche comparison      | 4 credits   | Gap analysis, opportunity scoring    |
| Transcript analysis   | 8 credits   | Content breakdown, hooks, gaps       |

---

## Margin Analysis

### Monthly billing ($7/month per channel)

| Item                                      | Amount   |
|-------------------------------------------|----------|
| Gross revenue                             | $7.00    |
| Paddle fee (~5% + $0.50)                  | −$0.85   |
| Net revenue                               | $6.15    |
| Infrastructure share                      | −$0.04   |
| LLM budget (20 weighted credits, mixed)   | −$0.50   |
| **Net margin**                            | **$5.61 (80%)** ✅ |

### Annual billing ($70/year per channel)

| Item                                      | Amount   |
|-------------------------------------------|----------|
| Annual gross revenue                      | $70.00   |
| Paddle fee on $70 (~5% + $0.50)           | −$4.00   |
| Net annual revenue                        | $66.00   |
| Infrastructure (12 months)                | −$0.48   |
| LLM budget (12 months × $0.50)            | −$6.00   |
| **Annual net margin**                     | **$59.52 (85%)** ✅ |

Annual billing hits the upper end of the 75–85% target and is preferred from a
cash flow perspective (payment upfront).

---

## Tier Enforcement in Code

```csharp
public static class TierLimits
{
    public static Dictionary<string, TierLimit> ByTier = new()
    {
        ["free"] = new TierLimit
        {
            MaxChannels = 1,
            MonthlyAnalyses = int.MaxValue,   // unlimited non-LLM operations
            CanAccessLlm = false,
            CanAccessTranscripts = false,
            AllowedPlugins = new[] { "free" },
            MonthlyWeightedCredits = 0
        },
        ["creator"] = new TierLimit
        {
            MaxChannels = int.MaxValue,        // billed per channel
            MonthlyAnalyses = int.MaxValue,
            CanAccessLlm = true,
            CanAccessTranscripts = true,
            AllowedPlugins = new[] { "free", "creator" },
            MonthlyWeightedCredits = 20,       // per channel, hard cap
            AllowCreditPurchase = true         // Research Packs (Phase 2)
        }
    };
}
```

```csharp
public class PlanEnforcer
{
    public async Task EnforceLlmAccessAsync(Guid userId, string channelId, string taskType)
    {
        var user = await _userRepo.GetAsync(userId);
        var limits = TierLimits.ByTier[user.SubscriptionTier];

        if (!limits.CanAccessLlm)
            throw new FeatureAccessDeniedException(
                "AI insights require the Creator tier.");

        var weight = LlmCallWeights.ByTaskType.GetValueOrDefault(taskType, 1);
        var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow);
        var usedCredits = await _usageRepo.GetWeightedCreditsUsedAsync(
            userId, channelId, currentMonth);

        // Check subscription credits
        var remainingSubscription = limits.MonthlyWeightedCredits - usedCredits;

        if (remainingSubscription >= weight)
            return; // covered by subscription

        // Check purchased Research Pack credits (Phase 2)
        var packCredits = await _creditRepo.GetAvailablePackCreditsAsync(userId);

        if (packCredits >= weight)
        {
            await _creditRepo.DeductPackCreditsAsync(userId, weight);
            return;
        }

        // Hard cap — no overage
        throw new CreditLimitExceededException(
            $"Monthly AI credits exhausted. Purchase a Research Pack or wait until next month.");
    }
}
```

---

## Phase 2: Research Packs (Month 6+ Post-Launch)

Research Packs are one-time credit purchases via Paddle, introduced after 6 months
of real usage data confirms how users hit their limits.

**Why wait 6 months:** Real usage data will reveal whether users cluster around
transcript analysis (high credit cost) or keyword/video analysis (low cost). Pack
pricing can then be tuned to actual patterns rather than guesses.

**Pack structure:**

| Pack           | Price  | Credits | Effective Cost/Credit | Paddle Net  |
|----------------|--------|---------|-----------------------|-------------|
| Starter Pack   | $4.99  | 15      | $0.33                 | ~$4.24      |
| Standard Pack  | $9.99  | 35      | $0.29 (12% bonus)     | ~$9.24      |
| Pro Pack       | $19.99 | 80      | $0.25 (23% bonus)     | ~$18.99     |

**Developer cost per credit: ~$0.004–$0.016 depending on task mix**
**Margin on pack purchases: 80–98%** — highest-margin product in the lineup.

**Pack credits:**
- Do not expire (no artificial urgency)
- Apply after subscription credits are exhausted for the month
- Shared across all channels on the account
- Visible in a simple credit balance UI

```csharp
public class ResearchPackService
{
    // Called by Paddle webhook on successful one-time purchase
    public async Task GrantPackCreditsAsync(Guid userId, string packId)
    {
        var credits = packId switch
        {
            "starter"  => 15,
            "standard" => 35,
            "pro"      => 80,
            _ => throw new ArgumentException($"Unknown pack: {packId}")
        };

        await _creditRepo.AddCreditsAsync(new CreditGrant
        {
            UserId = userId,
            Credits = credits,
            Source = $"research_pack:{packId}",
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = null  // no expiry
        });
    }
}
```

---

## Phase 3: Bring Your Own Token (BYOT)

**Timeline:** Month 9–12 post-launch

BYOT is offered as a free feature within the Creator tier subscription.
There is no additional charge for connecting a user's own API key.

**Revenue impact:** Neutral on subscription revenue. Positive on margin.

| User type        | Subscription revenue | VARA LLM cost | Gross margin |
|------------------|---------------------|---------------|--------------|
| Managed (no BYOT)| $6.15 net/channel   | −$0.50        | 92%          |
| BYOT active      | $6.15 net/channel   | $0.00         | **100%**     |

BYOT users are the most profitable segment. They pay the channel subscription
and bring their own inference. Encouraging migration to BYOT for heavy users
is financially beneficial even though it removes a Research Pack revenue stream
for that user — the margin improvement exceeds the lost pack revenue at any
realistic usage level.

**Credit exhaustion messaging as a BYOT funnel:**
When a managed user hits their credit cap, the error message explicitly surfaces
BYOT as an alternative to purchasing packs:

```
"Monthly AI credits exhausted.
 Purchase a Research Pack to continue, or connect your own API key
 in Settings for unlimited access."
```

This positions BYOT as a natural upgrade path for power users without
cannibalizing pack sales from casual users (who will not want to manage an
API key).

---

## Paddle Integration (Technical)

```csharp
public class PaddleWebhookController : ControllerBase
{
    [HttpPost("/api/billing/webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] PaddleWebhookPayload payload)
    {
        // Verify webhook signature
        if (!_paddleService.VerifySignature(payload, Request.Headers["Paddle-Signature"]))
            return Unauthorized();

        switch (payload.EventType)
        {
            // Subscription created or renewed
            case "subscription.created":
            case "subscription.updated":
                await _billingService.ActivateCreatorTierAsync(
                    payload.CustomData.UserId,
                    payload.SubscriptionId,
                    payload.CurrentBillingPeriod.EndsAt);
                break;

            // Subscription cancelled (access until period end)
            case "subscription.canceled":
                await _billingService.ScheduleDowngradeAsync(
                    payload.CustomData.UserId,
                    payload.CurrentBillingPeriod.EndsAt);
                break;

            // One-time Research Pack purchase
            case "transaction.completed":
                if (payload.CustomData.ProductType == "research_pack")
                    await _researchPackService.GrantPackCreditsAsync(
                        payload.CustomData.UserId,
                        payload.CustomData.PackId);
                break;
        }

        return Ok();
    }
}
```

**Paddle custom data (passed at checkout):**
```json
{
  "userId": "uuid-of-user",
  "productType": "subscription | research_pack",
  "packId": "starter | standard | pro",
  "channelId": "uuid-of-channel"
}
```

---

## Billing UI Requirements (Bonus Episode B)

The billing dashboard must show:

```
Channel: @mkbhd
  AI Credits used this month:  14 / 20
  Credits remaining:           6
  Pack credits available:      35

  [Buy Research Pack ▼]        [Manage Subscription]

Subscription: Creator · $7/month
  Next billing date: March 7, 2026
  [Switch to Annual — save 17%]
```

---

## Revenue Projections

### Year 1 (MVP + Build in Public)
- Free signups: 300
- Paid channels: 50 (some users with 1–2 channels each)
- Revenue: 50 × $7 × 12 = $4,200
- Goal: community feedback, real usage data for Phase 2 pack design

### Year 2
- Free signups: 3,000
- Paid channels: 500
- Research pack revenue: ~$2,000
- Annual revenue: (500 × $7 × 12) + $2,000 = **$44,000**
- Costs (infra + LLM + Paddle): ~$9,000
- **Net: ~$35,000**

### Year 3
- Paid channels: 2,000
- Research pack revenue: ~$15,000
- Annual revenue: (2,000 × $7 × 12) + $15,000 = **$183,000**
- Costs: ~$35,000
- **Net: ~$148,000**

---

## Self-Hosting Note

Self-hosters get unlimited everything — their own YouTube API quota, their own LLM
API keys, their own infrastructure. The SaaS tier charges for the convenience of
not managing any of that. This is the open-core model and it works.

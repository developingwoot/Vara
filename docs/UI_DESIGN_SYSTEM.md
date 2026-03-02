# VARA UI Design System
## Visual Language, Component Standards & Design Decisions

**Version:** 1.0
**Stack:** SvelteKit + shadcn-svelte + Tailwind CSS
**Theme:** Dark-first
**Last Updated:** March 2026

---

## Table of Contents

1. [Design Philosophy](#1-design-philosophy)
2. [Reference Inspirations](#2-reference-inspirations)
3. [Color System](#3-color-system)
4. [Typography](#4-typography)
5. [Spacing & Layout](#5-spacing--layout)
6. [Navigation Architecture](#6-navigation-architecture)
7. [Component Patterns](#7-component-patterns)
8. [Data Visualization](#8-data-visualization)
9. [Score Display Convention](#9-score-display-convention)
10. [State Patterns](#10-state-patterns)
11. [Tier Gating Pattern](#11-tier-gating-pattern)
12. [Motion & Animation](#12-motion--animation)
13. [Page Templates](#13-page-templates)
14. [AI Prompt Snippet](#14-ai-prompt-snippet)

---

## 1. Design Philosophy

### The Guiding Principle: Calm Confidence

VARA is a tool for creators who are serious about their craft. The design should reflect that seriousness without being intimidating. Every screen should feel like it *knows the answer* — it just needs you to ask the question.

**Calm** means: no competing elements, no visual noise, no badges screaming for attention. The UI recedes so the insights come forward.

**Confidence** means: decisive typography, strong use of color where it matters, data that's presented as conclusions not raw numbers. VARA doesn't just show you 74 — it tells you that means "Moderate opportunity, worth pursuing."

### What VARA Is Not

VARA is not TubeBuddy or VidIQ. Both tools were designed by engineers to show *everything they know* on a single screen. The result is dashboards that feel like spreadsheets. Creators who aren't data-literate bounce immediately. Creators who are data-literate waste time parsing noise.

VARA's design makes a different bet: **one screen answers one question**. Depth is available, but never forced.

### The Three Rules

1. **Lead with the answer, follow with the evidence.** Every results page opens with a headline conclusion. Data tables and breakdowns live below the fold or behind an expand.

2. **Color earns its place.** Color is used to communicate meaning — status, trend direction, tier, score range. It is never decorative. A screen with no color is a screen where nothing needs your attention. A screen with amber means "take a look at this."

3. **Progressive disclosure over information density.** Show the minimum. Reveal depth on demand. This is the opposite of the competition.

---

## 2. Reference Inspirations

These are not "copy these sites." They are sources of *specific ideas* to internalize.

### Linear.app — Atmosphere & Typography
**What to take:** The quality of their dark backgrounds (deep navy, not pure black), the restraint in how rarely color appears, and their use of typography weight to create hierarchy without needing size differences. Also: their sidebar — thin, iconic, always visible but never dominant.

**What to avoid:** Their extreme minimalism. VARA has more data to present and can afford slightly more visual richness.

**URL:** linear.app

---

### Raycast — Progressive Disclosure & Utility Confidence
**What to take:** The idea that a power tool can feel delightful. Their use of keyboard shortcuts as a design element (not just a feature). Most importantly: how they reveal complexity only when the user is ready for it. The default state is always clean. Power users discover depth.

**What to avoid:** Their reliance on blur/glassmorphism effects — they're heavy and can feel dated.

**URL:** raycast.com

---

### Vercel Dashboard — Data with Intention
**What to take:** How they headline every view with a single key metric before showing supporting data. Their chart aesthetic — minimal grid lines, color used deliberately, data density that feels readable rather than crowded. Also: their empty states, which are actually instructional rather than just blank.

**What to avoid:** Their very corporate blue. VARA's indigo/violet is warmer and more distinctive.

**URL:** vercel.com/dashboard

---

### Resend.com — Dark SaaS Done Right
**What to take:** A masterclass in using near-black (not pure black) backgrounds to create depth. Their card system — subtle borders, slight surface elevation, generous internal padding. The feeling that every element has been placed deliberately. Clean tables with just enough visual structure.

**What to avoid:** Their almost-too-minimal approach to empty states.

**URL:** resend.com

---

## 3. Color System

All colors are defined as CSS custom properties in `app.css`. **No hardcoded hex or oklch values anywhere else in the codebase.** Every component references a token.

### CSS Custom Properties

```css
/* ============================================================
   VARA Design Tokens — app.css
   ============================================================ */

:root {
  /* --- Backgrounds --- */
  /* Not pure black. A deep navy with blue-indigo undertone.     */
  /* This creates depth and signals "serious tool" vs "app."     */
  --background:       oklch(0.11 0.015 265);  /* page background  */
  --surface:          oklch(0.16 0.015 265);  /* cards, panels    */
  --surface-2:        oklch(0.21 0.012 265);  /* elevated, hover  */
  --surface-3:        oklch(0.26 0.010 265);  /* modals, overlays */

  /* --- Borders --- */
  --border:           oklch(0.28 0.012 265);  /* standard divider */
  --border-subtle:    oklch(0.22 0.010 265);  /* very faint lines */
  --border-strong:    oklch(0.38 0.015 265);  /* focused inputs   */

  /* --- Text --- */
  /* Not pure white — slightly warm, easier on eyes in dark mode */
  --text:             oklch(0.93 0.008 265);  /* primary content  */
  --text-muted:       oklch(0.58 0.015 265);  /* labels, metadata */
  --text-subtle:      oklch(0.38 0.010 265);  /* placeholder, off */
  --text-inverse:     oklch(0.11 0.015 265);  /* text on light bg */

  /* --- Brand: Indigo/Violet --- */
  /* Analytical, trustworthy. Distinct from TubeBuddy orange     */
  /* and VidIQ teal. The indigo-to-violet range is VARA's space. */
  --primary:          oklch(0.62 0.22 268);   /* CTA buttons, links */
  --primary-hover:    oklch(0.68 0.22 268);   /* hover state        */
  --primary-active:   oklch(0.56 0.22 268);   /* pressed state      */
  --primary-muted:    oklch(0.22 0.08 268);   /* tinted bg behind   */
                                              /* primary elements   */
  --primary-text:     oklch(0.93 0.008 265);  /* text on --primary  */

  /* --- Semantic Colors --- */
  /* Used for status, scores, trend direction. Never decorative.  */
  --success:          oklch(0.70 0.18 155);   /* green — growth, positive   */
  --success-muted:    oklch(0.20 0.06 155);   /* tinted bg for success      */
  --warning:          oklch(0.78 0.18  80);   /* amber — moderate, notable  */
  --warning-muted:    oklch(0.22 0.06  80);   /* tinted bg for warning      */
  --danger:           oklch(0.62 0.22  25);   /* red — declining, error     */
  --danger-muted:     oklch(0.22 0.08  25);   /* tinted bg for danger       */
  --info:             oklch(0.65 0.16 225);   /* blue — neutral info        */
  --info-muted:       oklch(0.20 0.06 225);   /* tinted bg for info         */

  /* --- Chart Palette --- */
  /* These are SEMANTICALLY ASSIGNED for VARA.                   */
  /* Do not reorder or repurpose without updating this doc.      */
  --chart-1:          oklch(0.62 0.22 268);   /* indigo  — primary series, views      */
  --chart-2:          oklch(0.70 0.18 155);   /* emerald — growth, positive trends    */
  --chart-3:          oklch(0.78 0.20  80);   /* amber   — opportunity score          */
  --chart-4:          oklch(0.65 0.20 320);   /* violet  — trend lines, projections   */
  --chart-5:          oklch(0.62 0.22  25);   /* coral   — outliers and anomalies     */

  /* --- Tier Identity --- */
  --tier-free:        oklch(0.58 0.015 265);  /* slate  — free tier, understated  */
  --tier-free-bg:     oklch(0.20 0.010 265);  /* muted background                 */
  --tier-creator:     oklch(0.72 0.18 295);   /* purple — creator, feels premium  */
  --tier-creator-bg:  oklch(0.20 0.08 295);   /* tinted purple background         */

  /* --- Shadcn-Svelte Mappings --- */
  /* These map VARA tokens to the names shadcn-svelte expects.  */
  --background:       var(--background);
  --foreground:       var(--text);
  --card:             var(--surface);
  --card-foreground:  var(--text);
  --primary:          var(--primary);
  --primary-foreground: var(--primary-text);
  --secondary:        var(--surface-2);
  --secondary-foreground: var(--text);
  --muted:            var(--surface-2);
  --muted-foreground: var(--text-muted);
  --accent:           var(--primary-muted);
  --accent-foreground: var(--primary);
  --destructive:      var(--danger);
  --border:           var(--border);
  --input:            var(--border);
  --ring:             var(--primary);

  /* --- Border Radius --- */
  --radius:           0.5rem;     /* default — cards, panels  */
  --radius-sm:        0.25rem;    /* tags, badges, small elem */
  --radius-lg:        0.75rem;    /* modals, large panels     */
  --radius-full:      9999px;     /* pills, avatars           */
}
```

### Color Usage Quick Reference

| Token | Use |
|---|---|
| `--background` | Page background only |
| `--surface` | Cards, sidebars, panels |
| `--surface-2` | Hover states, secondary cards, inputs |
| `--border` | All dividers and outlines |
| `--text` | Body copy, headings |
| `--text-muted` | Labels, metadata, timestamps |
| `--primary` | Buttons, links, active nav |
| `--success` | Score 80+, rising trend, positive delta |
| `--warning` | Score 50-79, moderate, watch |
| `--danger` | Score 0-49, declining, error |
| `--chart-1..5` | Charts only — see semantics above |
| `--tier-creator` | Creator badge, locked feature accent |

### What Color Is NOT Used For

- Decorative illustration or background gradients
- Differentiating sections of a page (use spacing and typography instead)
- Making the UI feel "lively" — motion handles that, not color

---

## 4. Typography

### Font Stack

```css
/* In app.css */
@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600&family=DM+Mono:wght@400;500&display=swap');

:root {
  --font-sans: 'DM Sans', system-ui, sans-serif;
  --font-mono: 'DM Mono', 'JetBrains Mono', monospace;
}
```

**Why DM Sans:** It's clean and functional like Inter but has slightly more personality — subtly rounded terminals that soften the analytical data-heavy context without losing professionalism. It reads exceptionally well at small sizes in dark mode.

**Why DM Mono:** Pairs naturally with DM Sans (same design family). Used for all numeric data, scores, metrics, and code. Monospace numbers prevent layout jumping when values update in real time.

### Type Scale

| Name | Size | Weight | Usage |
|---|---|---|---|
| `text-xs` | 11px | 500 | Timestamps, fine print, tags |
| `text-sm` | 13px | 400/500 | Table cells, metadata, captions |
| `text-base` | 15px | 400 | Body copy, form labels |
| `text-md` | 17px | 500 | Card titles, section headers |
| `text-lg` | 20px | 600 | Page section headings |
| `text-xl` | 24px | 600 | Page titles |
| `text-2xl` | 32px | 600 | Headline metrics (the "answer") |
| `text-3xl` | 42px | 600 | Hero numbers on results pages |

### Typography Rules

**Numbers use monospace always.** Any numeric value — view counts, scores, percentages, credits — uses `--font-mono`. This prevents layout shift when numbers update and signals "this is data."

```svelte
<!-- Correct -->
<span class="font-mono text-2xl">74</span>

<!-- Wrong -->
<span class="text-2xl">74</span>
```

**Never use `font-bold` (700) in dark mode.** Bold text on dark backgrounds creates visual harshness. Use `font-semibold` (600) for emphasis — it reads as strong without aggression.

**Heading hierarchy uses weight, not just size.** A 15px/600 label reads as more important than 15px/400 body text. Don't always reach for a larger size to create hierarchy.

**Line height for data-dense views:** `leading-snug` (1.375) for tables and metrics. `leading-relaxed` (1.625) for explanatory text and LLM insights.

---

## 5. Spacing & Layout

### Base Unit: 4px

All spacing is a multiple of 4px. Use Tailwind's spacing scale directly:

| Tailwind | Value | Use |
|---|---|---|
| `p-1` | 4px | Tight internal padding (tags, badges) |
| `p-2` | 8px | Compact elements |
| `p-3` | 12px | Small card padding |
| `p-4` | 16px | Standard padding |
| `p-6` | 24px | Card internal padding |
| `p-8` | 32px | Section padding |
| `p-12` | 48px | Page-level vertical rhythm |
| `gap-2` / `gap-4` / `gap-6` | 8/16/24px | Component gaps |

**No half-values.** Never `p-2.5` or `mt-3.5`. If something looks better with an odd value, the containing layout needs adjustment.

### Page Layout

```
┌─────────────────────────────────────────────┐
│  64px sidebar  │  Page content (flex-1)     │
│                │                            │
│  VARA logo     │  ┌──────────────────────┐  │
│  ──────────    │  │  Page header (56px)  │  │
│  Nav items     │  └──────────────────────┘  │
│  (48px each)   │                            │
│                │  ┌──────────────────────┐  │
│  ──────────    │  │  Content area        │  │
│  Settings      │  │  max-w-5xl centered  │  │
│  Tier + credits│  │  px-8 py-8           │  │
└─────────────────────────────────────────────┘
```

**Sidebar width:** 240px expanded, 64px icon-only on small viewports.

**Content max-width:** `max-w-5xl` (1024px) for most pages. `max-w-2xl` (672px) for focused forms (analysis trigger pages). Never full-bleed content — white space on large screens is intentional.

**Page header:** Fixed 56px top bar within the content area. Contains page title, breadcrumb, and page-level actions. Not the same as the sidebar nav.

---

## 6. Navigation Architecture

### Sidebar Structure

```
VARA (logo + wordmark)
─────────────────────
🏠  Dashboard          /
🔍  Keyword Research   /analyze/keyword
📈  Trend Detection    /analyze/trends
🎬  Video Analysis     /analyze/video
🔭  Niche Compare      /analyze/niche
─────────────────────
🧩  Plugins            /plugins
─────────────────────
⚙️   Settings           /settings

[Channel switcher]
[Tier badge + credits]
```

### Active State

Active route: `--primary` colored left border (3px), slightly lighter background (`--surface-2`), icon and text in `--primary`.

Inactive: icon in `--text-muted`, text in `--text-muted`. On hover: background `--surface-2`, text `--text`.

```svelte
<!-- Nav item active state -->
<a
  class="flex items-center gap-3 px-4 py-3 rounded-lg transition-colors
         border-l-[3px] border-primary bg-surface-2 text-primary"
>
```

### Route Groups in SvelteKit

```
src/routes/
  (auth)/            ← No sidebar layout
    login/
    register/
  (app)/             ← Full sidebar layout
    +layout.svelte   ← Sidebar + top bar
    /                ← Dashboard
    analyze/
      keyword/
      trends/
      video/
      niche/
    results/
      [id]/
    plugins/
    settings/
```

---

## 7. Component Patterns

### Buttons

Four variants, used in strict contexts:

```
primary   → The one action you want the user to take. One per screen.
secondary → Supporting actions. "View results", "Export", "Save"
ghost     → Tertiary. Nav items, subtle toggles, inline actions
danger    → Destructive only. "Delete channel", "Remove"
```

Size rule: `md` for all primary actions, `sm` for inline table actions and compact UIs.

Loading state: replace button text with spinner + present-tense verb:
- "Run Analysis" → `[spinner] Analyzing...`
- "Save" → `[spinner] Saving...`

Never disable a button without explaining why. Use `title` attribute for tooltip.

### Cards

Cards are the primary container for grouped information.

```svelte
<!-- Standard card -->
<div class="bg-surface border border-border rounded-lg p-6">

<!-- Highlighted card (attention needed) -->
<div class="bg-surface border border-warning/50 rounded-lg p-6">

<!-- Locked card (tier gated) -->
<div class="bg-surface border border-border rounded-lg p-6 opacity-60 relative">
  <TierLock tier="creator" />
</div>
```

Cards never have drop shadows. Separation is achieved through the `--border` token on a `--surface` background. Shadows imply a floating/modal pattern, which cards are not.

### Badges / Tags

Four semantic variants only:

```svelte
<!-- Score badge — color driven by score value -->
<Badge variant="success">Strong</Badge>
<Badge variant="warning">Moderate</Badge>
<Badge variant="danger">Competitive</Badge>

<!-- Tier badge -->
<Badge variant="creator">Creator</Badge>
<Badge variant="free">Free</Badge>
```

### Data Tables

Tables are the workhorse of VARA's results pages. Rules:

- No vertical gridlines — use column spacing instead
- Horizontal dividers between rows: `border-b border-border-subtle`
- Header row: `text-text-muted text-xs font-mono uppercase tracking-wider`
- Sortable columns: subtle arrow indicator in header, click toggles asc/desc
- Numbers right-aligned, text left-aligned
- Row hover: `bg-surface-2` transition
- Max 8 columns visible at once — hide/collapse less important columns on smaller viewports

### Forms

```
Validation timing: on blur (leaving a field), never on keystroke
Error position: inline below the input, never toast for field errors
Required fields: asterisk (*) in text-danger, explained in form intro
Submit state: button disabled + loading, all inputs read-only
Success: redirect or inline success state — never a modal
```

---

## 8. Data Visualization

VARA uses **shadcn-svelte's Chart component** (built on LayerChart) for all visualizations.

### Chart Color Semantics

This is the canonical assignment. **Do not use chart colors for anything else. Do not use other colors in charts.**

| Token | Color | VARA Meaning | Used For |
|---|---|---|---|
| `--chart-1` | Indigo | Primary data series | View counts, main metric |
| `--chart-2` | Emerald | Growth / positive | Rising trends, positive delta |
| `--chart-3` | Amber | Opportunity | Score highlights, notable data |
| `--chart-4` | Violet | Trend/projection | Trend lines, forecast |
| `--chart-5` | Coral | Outlier / anomaly | Outlier videos, anomaly markers |

### Chart Config Pattern

Every chart in VARA defines a `chartConfig` object that maps series to chart tokens:

```typescript
// Keyword trend chart
const keywordChartConfig = {
  views:   { label: "Views",        color: "var(--chart-1)" },
  trend:   { label: "Trend",        color: "var(--chart-4)" },
} satisfies ChartConfig;

// Outlier detection chart
const outlierChartConfig = {
  expected: { label: "Expected Views",  color: "var(--chart-1)" },
  actual:   { label: "Actual Views",    color: "var(--chart-5)" }, // coral = outlier
} satisfies ChartConfig;
```

### Chart Aesthetic Rules

```
Grid lines: horizontal only, color: --border-subtle, opacity: 0.4
Axis labels: text-xs, font-mono, color: --text-muted
Legend: only when >2 series. Position: top-right. Font: text-xs/font-mono
Tooltips: --surface-3 background, --border border, standard VARA typography
Bar radius: 4px (rounded top corners, never pill-shaped bars)
Line charts: strokeWidth 2, no dots unless interactive hover
Area charts: fill opacity 0.15 — just enough to anchor the line
Animation: 400ms ease-out on mount, no looping animations
```

### Chart Sizing

| Context | Height |
|---|---|
| Mini sparkline (table row) | 32px |
| Card-level chart | 180px |
| Section chart | 300px |
| Full-page chart | 400px |

---

## 9. Score Display Convention

VARA produces 0-100 scores for keywords, opportunities, and video analysis. **These are always displayed identically, everywhere in the app.**

### The Pattern

```
[Score number] / 100  [Colored label badge]
```

Example:
```svelte
<ScoreDisplay score={74} />
<!-- Renders: "74 / 100  [amber badge: Moderate]" -->
```

### Thresholds

| Score Range | Color Token | Label | Meaning for creator |
|---|---|---|---|
| 80 – 100 | `--success` | Strong | Real opportunity, move fast |
| 50 – 79 | `--warning` | Moderate | Worth considering, more research needed |
| 0 – 49 | `--danger` | Competitive | Saturated — needs differentiation or pass |

### Score Component

```svelte
<!-- ScoreDisplay.svelte -->
<script lang="ts">
  export let score: number;

  $: variant = score >= 80 ? 'success'
              : score >= 50 ? 'warning'
              : 'danger';
  $: label = score >= 80 ? 'Strong'
           : score >= 50 ? 'Moderate'
           : 'Competitive';
</script>

<div class="flex items-baseline gap-2">
  <span class="font-mono text-2xl text-text">{score}</span>
  <span class="font-mono text-sm text-muted">/ 100</span>
  <Badge {variant}>{label}</Badge>
</div>
```

---

## 10. State Patterns

Every data view has exactly three states. All three must be designed before a component is considered complete.

### Loading State — Skeleton Screens

Never use a spinner for page-level loading. Use skeleton screens that match the *shape* of the content that will appear. This reduces perceived load time and prevents layout jump.

```svelte
<!-- Skeleton for a results card -->
<div class="bg-surface border border-border rounded-lg p-6 animate-pulse">
  <div class="h-8 w-24 bg-surface-2 rounded mb-4" />   <!-- Score placeholder -->
  <div class="h-4 w-48 bg-surface-2 rounded mb-2" />   <!-- Label placeholder -->
  <div class="h-4 w-36 bg-surface-2 rounded" />        <!-- Sublabel placeholder -->
</div>
```

**Exception:** The analysis progress view intentionally shows a real progress bar with percentage and status text. This is a feature, not a loading state — it teaches the user what VARA is doing.

### Empty State — Instructional

Never blank space. Every empty state has: an icon, a headline that explains the situation, and a CTA button that takes them somewhere useful.

```svelte
<!-- EmptyState.svelte -->
<div class="flex flex-col items-center justify-center py-16 gap-4 text-center">
  <div class="text-4xl">{icon}</div>
  <h3 class="text-md font-semibold text-text">{headline}</h3>
  <p class="text-sm text-muted max-w-sm">{description}</p>
  {#if ctaLabel}
    <Button variant="primary" href={ctaHref}>{ctaLabel}</Button>
  {/if}
</div>
```

Examples:
- No analyses run yet → "Run your first analysis" → [Run Keyword Analysis]
- No channels added → "Add a channel to get started" → [Add Channel]
- Keyword returns no data → "No videos found for this keyword" → [Try Different Keyword]

### Error State — Inline, Actionable

Errors appear inline near where they occurred, not as modals. Every error message has a retry action where applicable.

```svelte
<div class="flex items-start gap-3 p-4 bg-danger-muted border border-danger/30 rounded-lg">
  <AlertCircle class="text-danger mt-0.5 shrink-0" size={16} />
  <div>
    <p class="text-sm text-text">{message}</p>
    {#if onRetry}
      <button class="text-sm text-primary mt-1" on:click={onRetry}>Try again</button>
    {/if}
  </div>
</div>
```

### Toast Notifications

Used only for transient feedback that isn't tied to a specific UI location.

```
Success:  --success color, auto-dismiss after 3 seconds
Error:    --danger color, persists until manually dismissed
Info:     --info color, auto-dismiss after 4 seconds
Warning:  --warning color, auto-dismiss after 5 seconds

Position: top-right, stacked if multiple
Never:    use modals for transient messages
```

---

## 11. Tier Gating Pattern

Creator-only features are **always visible, never hidden**. Hiding features removes the passive upgrade prompt and reduces perceived value of the Creator tier.

### The Pattern

Locked features appear at full opacity with a lock overlay on hover that explains what they unlock and shows the upgrade path.

```svelte
<!-- TierGate.svelte -->
<script lang="ts">
  import { Lock } from 'lucide-svelte';
  export let tier: 'creator' = 'creator';
  export let feature: string;
</script>

<div class="relative group">
  <!-- The actual content, slightly muted -->
  <div class="opacity-50 pointer-events-none">
    <slot />
  </div>

  <!-- Lock overlay -->
  <div class="absolute inset-0 flex items-center justify-center
              opacity-0 group-hover:opacity-100 transition-opacity
              bg-surface/80 rounded-lg backdrop-blur-sm">
    <div class="flex flex-col items-center gap-2 text-center p-4">
      <Lock class="text-tier-creator" size={20} />
      <p class="text-sm font-medium text-text">{feature}</p>
      <p class="text-xs text-muted">Available on Creator plan</p>
      <a href="/settings/upgrade"
         class="text-xs text-tier-creator font-medium hover:underline">
        Upgrade — $7/month →
      </a>
    </div>
  </div>
</div>
```

### Where Tier Gating Appears

| Feature | Gated Element |
|---|---|
| LLM insights on keyword results | The insights panel below score |
| AI Insights toggle on analysis form | The toggle input itself |
| Niche comparison detail view | The gap analysis section |
| Transcript analysis | The transcript panel |
| >1 channel | The "Add Channel" button after first channel |

---

## 12. Motion & Animation

Motion is used sparingly. It communicates state changes and guides attention — it never decorates.

### Principles

- **Functional motion only.** If removing an animation doesn't change what the user understands, remove it.
- **Fast entrances, slow exits.** Elements appear quickly (150ms) so the interface feels snappy. They leave slowly (300ms) so the user has time to register what happened.
- **No looping animations** except the analysis progress bar.

### Standard Durations

```css
--duration-fast:    150ms;   /* hover states, button presses   */
--duration-base:    250ms;   /* panel opens, tab switches      */
--duration-slow:    400ms;   /* page transitions, chart mount  */
--ease-out:         cubic-bezier(0.16, 1, 0.3, 1);   /* snappy, feels native */
--ease-in-out:      cubic-bezier(0.4, 0, 0.2, 1);    /* smooth transitions   */
```

### Specific Animations

**Chart mount:** Bars animate up from 0, lines draw left-to-right, 400ms ease-out. Staggered if multiple series (50ms delay between each).

**Progress bar (analysis running):** Smooth continuous fill. Shows percentage text. Status message updates with a 200ms fade transition.

**Skeleton → content:** 150ms fade-in. No slide or scale — just opacity.

**Toast enter/exit:** Slide in from right (250ms), fade out in place (200ms).

**Tier gate hover:** Opacity crossfade 200ms — overlay appears, content dims simultaneously.

---

## 13. Page Templates

### Template A: Analysis Trigger Page
*Used for: /analyze/keyword, /analyze/trends, /analyze/video, /analyze/niche*

```
┌─────────────────────────────────────────┐
│  Page title           [Quota indicator] │
│  One-line description                   │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────────────────────────────┐    │
│  │  Analysis form (max-w-2xl)      │    │
│  │  - Primary input (keyword etc)  │    │
│  │  - Optional settings (collapse) │    │
│  │  - Tier-gated toggle (if any)   │    │
│  │  - [Run Analysis] button        │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ── When running ─────────────────────  │
│  ┌─────────────────────────────────┐    │
│  │  Progress bar + % + status msg  │    │
│  │  [Cancel]                       │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ── Recent runs ───────────────────── ▼ │
│  Last 5 analyses, link to full results  │
└─────────────────────────────────────────┘
```

---

### Template B: Results Page
*Used for: /results/[id]*

```
┌─────────────────────────────────────────┐
│  ← Back to [analysis type]             │
│  "sourdough starter" · Keyword · 2h ago │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────────────────────────────┐    │
│  │  THE ANSWER (headline card)     │    │
│  │                                 │    │
│  │  74 / 100  [Moderate]           │    │
│  │  "Moderate competition,         │    │
│  │   high educational intent"      │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ┌──────────┐  ┌──────────┐            │
│  │ Score    │  │ Trend    │            │
│  │ breakdown│  │ chart    │            │
│  └──────────┘  └──────────┘            │
│                                         │
│  ── LLM Insights ── [creator only] ── ▼ │
│  ── Outlier Videos ────────────────── ▼ │
│  ── All Results (table) ───────────── ▼ │
└─────────────────────────────────────────┘
```

---

### Template C: Dashboard / Hub
*Used for: /*

```
┌─────────────────────────────────────────┐
│  Good morning · [Channel name]          │
│  [Tier badge] · [Credits: 18/20]        │
├─────────────────────────────────────────┤
│                                         │
│  ┌──────────┐  ┌──────────┐            │
│  │🔍 Keyword│  │📈 Trends │            │
│  │ Research │  │Detection │            │
│  └──────────┘  └──────────┘            │
│  ┌──────────┐  ┌──────────┐            │
│  │🎬 Video  │  │🔭 Niche  │            │
│  │ Analysis │  │ Compare  │            │
│  └──────────┘  └──────────┘            │
│                                         │
│  ── Recent Analyses ─────────────────── │
│  [Table: keyword · type · date · score] │
└─────────────────────────────────────────┘
```

---

### Template D: Settings Page
*Used for: /settings*

```
┌─────────────────────────────────────────┐
│  Settings                               │
├───────────────┬─────────────────────────┤
│ Account       │  [Content area]         │
│ Channels      │                         │
│ Subscription  │                         │
│ API Keys      │                         │
│ Appearance    │                         │
└───────────────┴─────────────────────────┘
```

Two-column settings layout. Left is a secondary nav. Right is the active settings panel. Avoids accordion hell where all settings compete for attention.

---

## 14. AI Prompt Snippet

When asking Claude (or any AI) to build a VARA component, prepend this to your request. It encodes the design system so generated code is immediately consistent:

```
You are building a component for VARA, a YouTube analytics SaaS tool.

DESIGN SYSTEM:
- Stack: SvelteKit + shadcn-svelte + Tailwind CSS
- Theme: Dark-first. Background --background (deep navy oklch(0.11 0.015 265)),
  not pure black.
- Philosophy: "Calm confidence." One screen answers one question.
  Color is functional, never decorative.
- Typography: DM Sans (UI), DM Mono (all numbers/metrics)
- Spacing: 4px base unit, multiples only
- Never font-bold (700), use font-semibold (600) for emphasis

COLOR TOKENS (reference as CSS variables):
- Backgrounds: --background, --surface, --surface-2
- Text: --text, --text-muted, --text-subtle
- Brand: --primary, --primary-hover, --primary-muted
- Semantic: --success, --warning, --danger, --info (each has -muted variant)
- Charts: --chart-1 (indigo/views), --chart-2 (emerald/growth),
  --chart-3 (amber/opportunity), --chart-4 (violet/trend),
  --chart-5 (coral/outlier)
- Tiers: --tier-free, --tier-creator

SCORES always display as: "[number] / 100 [Badge]"
  80-100 → success + "Strong"
  50-79  → warning + "Moderate"
  0-49   → danger  + "Competitive"

STATES: Every data component needs loading (skeleton),
  empty (icon + headline + CTA), and error (inline, with retry) states.

TIER GATING: Creator features are visible but dimmed (opacity-50),
  with a lock overlay on hover showing upgrade CTA. Never hidden entirely.

INSPIRATION: Linear.app atmosphere, Vercel dashboard data clarity,
  Raycast progressive disclosure.
```

---

*This document is the source of truth for all frontend design decisions in VARA. When in doubt, refer here. When this doc doesn't cover something, add it here before building it.*

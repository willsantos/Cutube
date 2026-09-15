---
name: Cutube
description: Dark Oroborus identity for a terminal-first YouTube downloader CLI — the GitHub Pages page and the CLI share one chromatic hierarchy.
colors:
  bg: "#0e0e13"
  surface-low: "#131318"
  surface: "#19191f"
  card: "#1f1f26"
  card-raised: "#25252c"
  ink: "#f8f5fd"
  muted: "#acaab1"
  border: "#48474d"
  border-strong: "#76747b"
  primary: "#b3a1ff"
  primary-intense: "#7a53ff"
  primary-container: "#a690ff"
  secondary: "#ff6a9d"
  secondary-deep: "#ba005d"
  grad-a: "#5b2eff"
  grad-b: "#ff2e88"
  ok: "#4ade80"
  warn: "#fbbf24"
  err: "#ff6e84"
typography:
  display:
    fontFamily: "\"Space Grotesk\", \"Manrope\", sans-serif"
    fontSize: "clamp(2.5rem, 5.4vw, 4rem)"
    fontWeight: 700
    lineHeight: 1.04
    letterSpacing: "-0.03em"
  headline:
    fontFamily: "\"Space Grotesk\", \"Manrope\", sans-serif"
    fontSize: "clamp(1.5rem, 2.6vw, 2rem)"
    fontWeight: 700
    letterSpacing: "-0.02em"
  body:
    fontFamily: "\"Manrope\", \"Segoe UI\", sans-serif"
    fontSize: "1.0625rem"
    fontWeight: 400
    lineHeight: 1.65
  lede:
    fontFamily: "\"Manrope\", \"Segoe UI\", sans-serif"
    fontSize: "1.14rem"
    fontWeight: 400
  label:
    fontFamily: "\"Manrope\", \"Segoe UI\", sans-serif"
    fontSize: "0.95rem"
    fontWeight: 600
  mono:
    fontFamily: "\"Geist Mono\", \"Cascadia Code\", ui-monospace, monospace"
    fontSize: "0.95rem"
    fontWeight: 400
    lineHeight: 1.7
rounded:
  sm: "6px"
  md: "10px"
  lg: "12px"
spacing:
  sm: "16px"
  md: "24px"
  lg: "32px"
  xl: "64px"
components:
  btn-primary:
    backgroundColor: "{colors.primary}"
    textColor: "#0b0b10"
    rounded: "{rounded.md}"
    padding: "13px 24px"
  btn-primary-hover:
    backgroundColor: "{colors.primary-container}"
  btn-ghost:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "13px 24px"
  terminal-panel:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.lg}"
  copy-button:
    backgroundColor: "{colors.card}"
    textColor: "{colors.muted}"
    rounded: "7px"
    padding: "6px 11px"
  inline-code:
    backgroundColor: "{colors.card}"
    textColor: "{colors.primary}"
    rounded: "{rounded.sm}"
    padding: "2px 7px"
  status-card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "15px 18px"
---

# Design System: Cutube

## Overview

**Creative North Star: "The Operational Terminal"**

Cutube looks like an operations surface, not a brochure. The install command is
the hero of every composition; everything else supports the act of copying,
pasting, and confirming. The committed direction contract called it a
"página-terminal operacional" — a terminal page — and explicitly rejected the
category default of a generic hero plus three identical feature cards. The
ground is near-black with a violet cast (`--bg` #0e0e13), surfaces climb a
quiet five-step tonal staircase, and color is rationed: lavender is the single
voice of action, pink is punctuation, and the Oroborus gradient appears only
where identity is meant to flash.

The world spans two media that behave as one identity. The GitHub Pages page
(`docs/index.html`) carries the full token expression. The CLI
(`src/Cutube.Cli/Theming/ConsoleTheme.cs`) carries the same hierarchy as
truecolor ANSI — and degrades to plain, undecorated text the moment output is
redirected or `NO_COLOR` is set, because the terminal is the product and the
pipe is sacred (PRODUCT.md: "Una identidade, superfícies honestas" — one
identity, honest surfaces).

Density is comfortable and technical: generous section padding, prose measures
capped at 46–62ch, commands always in mono inside bordered panels. There is
exactly one authored animation moment (the hero's staggered rise and the
blinking caret), and it vanishes completely under `prefers-reduced-motion`.

**Key Characteristics:**
- Dark tonal staircase (`#0e0e13` → `#25252c`); depth comes from surface steps and 1px borders, not shadows
- Lavender `#b3a1ff` is the only interactive color; pink `#ff6a9d` punctuates but never invites
- Brand gradient `#5b2eff` → `#ff2e88` rationed to one hero word and micro brand marks
- Commands are always Geist Mono inside terminal panels with one-click copy
- One entrance moment (rise + caret blink), fully removed under reduced motion
- WCAG 2.1 AA: ≥4.5:1 text contrast, visible lavender focus rings, state never carried by color alone

## Colors

A violet-tinted near-black world where lavender acts, pink punctuates, and the
brand gradient flashes only on cue. Token names below are the CSS custom
properties defined in `:root` of `docs/index.html`; they are normative.

### Primary
- **Lavender Voice** (#b3a1ff / `--primary`): the action color. Links, primary
  CTA fill, URL/path highlights inside commands, inline code chips, list
  markers, focus rings, the terminal caret — and, in the CLI, titles, steps and
  progress. Hover on the primary button lifts to **Container Lavender**
  (#a690ff / `--primary-container`). **Intense Lavender** (#7a53ff /
  `--primary-intense`) is spec-sanctioned (FR-4) for intense states; the
  current build reserves it unused.

### Secondary
- **Signal Pink** (#ff6a9d / `--secondary`): punctuation, not interaction —
  terminal prompts (`$`, `PS>`) and the hover color of body links.
  **Deep Pink** (#ba005d / `--secondary-deep`) is the spec-sanctioned
  high-contrast container (FR-5); currently unexercised in the build.

### Tertiary — Brand Gradient
- **Oroborus Gradient** (#5b2eff → #ff2e88 / `--grad-a` → `--grad-b`): the
  identity signature, sanctioned in exactly three placements — the
  gradient-clipped word in the hero H1, the 2px brandline hairline under the
  hero, and the small circular marks (14px wordmark orb, 9px footer dot). Never
  a fill for buttons, cards, or text blocks.

### Semantic States
- **Success Green** (#4ade80 / `--ok`): status-list check icons, the copy
  button's copied confirmation; CLI success lines.
- **Warning Amber** (#fbbf24 / `--warn`): conditional-requirement icons (the
  FFmpeg note); CLI warnings.
- **Error Coral** (#ff6e84 / `--err`): CLI errors; reserved on the page for
  failure states — none occur in the shipped install flow.

### Neutral
- **Void** (#0e0e13 / `--bg`): the page ground; also the text color painted
  under lavender text selection. Primary buttons use the darker ink #0b0b10 for
  their label.
- **Surface Low** (#131318 / `--surface-low`): header strips of terminal panels
  and install cards, the fallback disclosure background.
- **Surface** (#19191f / `--surface`): the workhorse panel color — terminal
  bodies, install cards, status cards.
- **Card** (#1f1f26 / `--card`): copy-button fill and inline-code chips.
- **Card Raised** (#25252c / `--card-raised`): the terminal-bar dots, the top
  of the surface staircase.
- **Ink** (#f8f5fd / `--ink`): primary text and command text.
- **Muted** (#acaab1 / `--muted`): secondary text — ledes, hints, nav links at
  rest, footer.
- **Border** (#48474d / `--border`): all 1px panel and control borders,
  scrollbar thumb; rendered translucently (45–60% via `color-mix`) for hairline
  dividers.
- **Border Strong** (#76747b / `--border-strong`): scrollbar thumb hover and
  the higher-contrast border option when a panel sits on a busy ground.

### Named Rules
**The One-Word Gradient Rule.** The brand gradient is rationed to one
gradient-clipped word in the hero, the brandline hairline, and the orb/dot
marks. It never fills buttons, cards, backgrounds, or running text.

**The Action-Voice Rule.** Lavender is the only color that invites action. Pink
punctuates (prompts, link hover) but is never the at-rest color of a clickable
element; interactive things sit in lavender or ink.

**The Color-Plus-Label Rule.** Semantic color is never the sole signal: status
icons always sit beside labeled text on the page, and CLI state messages
survive with every escape code stripped (spec FR-13/FR-16).

## Typography

**Display Font:** Space Grotesk (fallback Manrope, then sans-serif) — titles,
section headings, the wordmark.
**Body Font:** Manrope (fallback Segoe UI, then sans-serif) — body, ledes,
labels, buttons, nav.
**Label/Mono Font:** Geist Mono (fallback Cascadia Code, then ui-monospace/
monospace) — commands, terminal bodies, the panel label.

**Character:** A grotesk/mono pairing that reads like a well-set man page —
tight-tracked Space Grotesk for authority, quiet Manrope for prose, and mono
reserved strictly for things a user types or copies.

### Hierarchy
- **Display** (Space Grotesk 700, clamp(2.5rem, 5.4vw, 4rem), line-height 1.04,
  letter-spacing -0.03em): the hero H1 only. Steps down to
  clamp(2.1rem, 9vw, 2.6rem) under 640px. The gradient word lives inside it.
- **Headline** (Space Grotesk 700, clamp(1.5rem, 2.6vw, 2rem), letter-spacing
  -0.02em): H2 section titles.
- **Lede** (Manrope 400, 1.14rem): the hero support line, capped at 46ch;
  `strong` spans brighten to ink at weight 600. Steps to 1.05rem on mobile.
- **Body** (Manrope 400, 1.0625rem, line-height 1.65): page default; drops to
  1rem under 640px. Section ledes cap at 62ch.
- **Label** (Manrope 600, 0.95rem): nav links and fallback summary. Buttons and
  OS labels take weight 800 (1rem and 0.95rem respectively); the copy button
  label is 600 at 0.8rem; footer runs 0.92rem.
- **Mono** (Geist Mono 400, 0.95rem, line-height 1.7): terminal bodies; 0.85rem
  under 640px; inline code at 0.88em.

### Named Rules
**The Mono-Is-For-Commands Rule.** Anything copyable or typed — install
commands, CLI invocations — is set in Geist Mono on a terminal surface with the
URL/path segment highlighted in lavender. Mono never styles prose.

## Layout

A single-column landing on a centered 1080px container (`.wrap`, 24px side
padding, 18px under 640px). The hero is a two-column grid (1.05fr / 0.95fr,
56px gap) with copy left and the terminal panel right, vertically centered;
padding 84px top / 72px bottom (56px / 52px under 900px). Sections carry 64px
vertical padding (48px under 640px) and are separated by 1px translucent
hairlines (`color-mix` of `--border` at 45%).

Spacing rhythm: 16px micro gaps, 24px grid gaps and container padding, 32px
block spacing after ledes. Breakpoints: **900px** collapses the hero and
install grids to one column (40px hero gap); **640px** steps body type down,
stacks buttons full-width, hides nav text labels (leaving the GitHub glyph),
and shrinks terminal type. Prose measures: ledes 46–62ch, status list capped at
720px. No horizontal overflow at any viewport (FR-14).

## Elevation & Depth

Depth is tonal first: the five-step surface staircase plus 1px borders carries
nearly all structure. Borders themselves soften with distance — internal
separators and dividers render `--border` translucently at 45–60%. There is
exactly one shadow in the entire system.

### Shadow Vocabulary
- **Terminal Lift** (`box-shadow: 0 18px 50px rgba(0, 0, 0, 0.42)`): reserved
  for the hero terminal panel — the single lifted object on the page, marking
  the install command as the product's center of gravity. Install cards and
  status cards use the same surface color with no shadow at all.

### Named Rules
**The One Lift Rule.** One shadow per page, and it belongs to the hero
terminal. Everything below the fold is flat and bordered.

## Shapes

Soft-technical corners on a three-step radius scale: panels take the largest
radius (12px), interactive controls the middle step (10px), small chips the
smallest (6px, with the copy button's 7px as its one-off). Every border is 1px;
there are no thick outlines outside the focus ring (2px lavender, 2px offset,
4px corner radius). The signature geometry is circular and gradient-filled:
the 14px wordmark orb, the 9px footer dot, the 10px terminal-bar dots. The
horizontal signature is the 2px brandline — transparent → #5b2eff → #ff2e88 →
transparent at 0.75 opacity.

## Components

Components are panel-first: most content lives in a bordered surface with a
low header strip, echoing a terminal window.

### Buttons
- **Shape:** gently rounded (10px), Manrope 800 at 1rem, padding 13px 24px,
  inline icon at 15–16px.
- **Primary:** lavender fill (#b3a1ff) with near-black text (#0b0b10) and a
  matching 1px border; hover fills Container Lavender (#a690ff); active presses
  down 1px.
- **Ghost:** transparent with ink text and a 1px #48474d border; hover turns
  both border and text lavender.
- **Transitions:** 0.18s — transform on cubic-bezier(0.22, 1, 0.36, 1), colors
  on ease.
- **Mobile (≤640px):** full-width, stacked, centered labels.

### Terminal Panel (signature component)
- **Shape:** 12px radius, 1px border, Surface (#19191f) background; the hero
  instance alone carries the Terminal Lift shadow.
- **Bar:** Surface Low header with three 10px dots (Card Raised fill, bordered)
  and a right-aligned mono label ("terminal", 0.78rem, muted).
- **Body:** Geist Mono 0.95rem, line-height 1.7, padding 20px 18px. Prompt in
  pink (`$` or `PS>`), command in ink, URL/path segment highlighted lavender, a
  9px × 1.15em lavender block caret (blinking at 1.1s steps(1) when motion is
  allowed).
- **Copy button:** Card fill, muted label, 7px radius, 6px 11px padding; hover
  shifts border and label to lavender; success shifts both to green and swaps
  "copiar" → "copiado" for 2s; falls back to text selection when the Clipboard
  API is unavailable.
- **Install card variant:** the same panel with an OS header strip
  (Surface Low, Manrope 800 0.95rem, lavender platform icon) and a tighter
  body padding (16px).
- **Fallback disclosure:** a Surface Low `<details>` with muted summary text
  that hovers to lavender; its chevron rotates 90° when open (0.18s ease).

### Status List
- Bordered Surface cards (10px radius, 15px 18px padding) with an 18px leading
  icon — green check-circle for included features, amber warning-triangle for
  conditional ones — each paired with a bold ink lead-in and a muted hint.
  Color is confirmation, never the message itself.

### Inline Code
- Card-fill chips (6px radius, 2px 7px padding, mono at 0.88em) in lavender,
  used for CLI invocations inside prose (`cutube --version`).

### Navigation
- **Topbar:** wordmark left (Space Grotesk 700 1.25rem with the 14px gradient
  orb), muted 0.95rem links right that hover to lavender with an underline;
  hairline bottom border. **Footer:** mirrors the topbar below a hairline with
  the 9px gradient dot beside "Cutube — uma ferramenta Oroborus".

### CLI Output (ANSI mapping)
The same hierarchy in truecolor SGR, defined in
`src/Cutube.Cli/Theming/ConsoleTheme.cs` and applied through
`IConsoleService` extensions — only when output is not redirected and
`NO_COLOR` is unset:

| Element | Color | SGR |
| --- | --- | --- |
| Titles, sections, progress | Primary #b3a1ff | `\x1b[38;2;179;161;255m` |
| Success | #4ade80 | `\x1b[38;2;74;222;128m` |
| Warning | #fbbf24 | `\x1b[38;2;251;191;36m` |
| Error | #ff6e84 | `\x1b[38;2;255;110;132m` |
| Reset | — | `\x1b[0m` |

Redirected output, pipes, and CI get byte-identical plain text (FR-16/FR-18).
Windows conhost receives best-effort VT enablement; the progress bar keeps its
geometry and cadence — only emphasis is colored (FR-19). No dependency was
added to paint the CLI (FR-21).

### Motion
One authored moment, visible by default and gated behind
`prefers-reduced-motion: no-preference`: hero intro children rise 0.7s
(translateY 14px → 0, cubic-bezier(0.22, 1, 0.36, 1)) with a 0.07s stagger;
the terminal rises 0.8s at 0.12s delay; the caret blinks 1.1s steps(1).
Under `prefers-reduced-motion: reduce`, transitions drop to `none`, the caret
freezes, and scrolling goes instant (FR-15).

## Do's and Don'ts

### Do:
- **Do** consume the CSS custom properties from `:root` of `docs/index.html`
  (`--primary`, `--surface`, `--grad-a`, `--font-mono`, …) — those exact names
  are the normative token vocabulary for any new page surface.
- **Do** put every command in Geist Mono on a terminal panel with a lavender
  URL highlight and a one-click copy affordance.
- **Do** pair every semantic color with an icon or label and keep text at
  WCAG 2.1 AA (≥4.5:1); verify focus rings are visible on dark.
- **Do** gate entrance animation and the caret blink behind
  `prefers-reduced-motion: no-preference`.
- **Do** reuse the terminal-panel pattern (bar + dots + mono body + copy) for
  any new command block on any Oroborus surface.

### Don't:
- **Don't** reintroduce the legacy GitHub Pages blue or any independent palette
  as identity (FR-8) — dark Oroborus is the only voice.
- **Don't** apply the brand gradient beyond its sanctioned placements: one hero
  word, the brandline, and orb/dot marks. No gradient buttons, cards, or text
  blocks.
- **Don't** add shadows beyond the hero terminal's single lift; structure comes
  from surface steps and 1px borders.
- **Don't** emit ANSI color from the CLI when output is redirected or
  `NO_COLOR` is set, and never let color change a message's meaning
  (FR-16/FR-18).
- **Don't** add a dependency just to color CLI output (FR-21).

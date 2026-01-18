# Darwin Mobile UI/UX Guidelines (Light Theme First)

> Document purpose: Define a consistent, future-proof UI/UX foundation for **Darwin.Mobile.Consumer** and **Darwin.Mobile.Business**, covering navigation and screen structure for **all roadmap phases**, while implementing functionality only where Phase 1 requires it.
>
> Status: Light theme (v1) — Dark theme planned (same token system, different values).
>
> Language: App UI in **German** for Phase 1. English is added later. Code comments remain **English**.


## Table of Contents

- [1. Design Goals](#1-design-goals)
- [2. UX Principles (Non-Negotiable)](#2-ux-principles-non-negotiable)
- [3. Brand and Visual Tone (Gold Logo Alignment)](#3-brand-and-visual-tone-gold-logo-alignment)
- [4. Theming Strategy (Light now, Dark later)](#4-theming-strategy-light-now-dark-later)
- [5. Localization Strategy (German first)](#5-localization-strategy-german-first)
- [6. Phase-Aware UI (All phases visible, limited behavior)](#6-phase-aware-ui-all-phases-visible-limited-behavior)
- [7. Document Structure (Next Sections)](#7-document-structure-next-sections)
- [8. Color System & Design Tokens (Gold-First, Light Theme v1)](#8-color-system--design-tokens-gold-first-light-theme-v1)
- [9. Typography, Spacing, Elevation, Grid](#9-typography-spacing-elevation-grid)
- [10. Core Components (Behavior, States, Consistency)](#10-core-components-behavior-states-consistency)
- [11. Navigation Patterns (All Phases, Phase 1 Implementation Constraints)](#11-navigation-patterns-all-phases-phase-1-implementation-constraints)
- [12. Screen Blueprints (All Phases)](#12-screen-blueprints-all-phases)
  - [12.2 Consumer App Screens (Darwin.Mobile.Consumer)](#122-consumer-app-screens-darwinmobileconsumer)
  - [12.3 Business App Screens (Darwin.Mobile.Business)](#123-business-app-screens-darwinmobilebusiness)
- [13. Error, Loading, Offline, and Empty States (Unified Standard)](#13-error-loading-offline-and-empty-states-unified-standard)
- [14. Accessibility Checklist (Light Theme v1, Dark Theme Ready)](#14-accessibility-checklist-light-theme-v1-dark-theme-ready)
- [15. MAUI Implementation Notes (Tokens, Themes, Routing, Localization-Ready)](#15-maui-implementation-notes-tokens-themes-routing-localization-ready)
- [16. UI Deliverables & Checklist per Phase (Operational Plan)](#16-ui-deliverables--checklist-per-phase-operational-plan)
- [17. Route & Screen Registry (Stable Keys for Implementation)](#17-route--screen-registry-stable-keys-for-implementation)


---

## 1. Design Goals

1. **No UI tech debt across phases**
   - Build the complete navigation skeleton and screen placeholders for all phases.
   - Phase >1 screens exist visually but remain non-functional (no navigation or show "Coming soon" content), per implementation plan.

2. **Brand-consistent, premium, trustworthy**
   - The logo is **gold**. The visual language should feel premium (subtle, not flashy), with high contrast and accessibility.

3. **Two app personas, one design system**
   - Consumer: friendly, discovery-driven, personal rewards.
   - Business: operational, fast scanning, low friction, kiosk/tablet readiness.
   - Shared tokens/components ensure consistency while allowing small variations.

4. **Theme-ready from day one**
   - All colors must be driven by **semantic design tokens**, not hard-coded.
   - Dark theme becomes a token-value swap, not a refactor.

---

## 2. UX Principles (Non-Negotiable)

### 2.1 Clarity over cleverness
- Primary actions must be obvious (one primary CTA per screen where possible).
- Use plain German labels. Avoid ambiguous icons without text.

### 2.2 Fast path for primary flows
- Consumer primary flow: **Login → QR → Reward confirmation feedback**
- Business primary flow: **Login → Scan → Validate → Confirm**
- Reduce taps and cognitive load in these flows.

### 2.3 Predictable navigation patterns
- Use the same placement and behavior for:
  - Back navigation
  - Top bars
  - Primary actions
  - Error states & retry patterns

### 2.4 Progressive disclosure
- Show only what is needed now; hide advanced controls under secondary actions.
- Phase >1 features are visible as placeholders but clearly labeled as not available yet.

### 2.5 Accessibility and contrast
- Minimum contrast should remain high (especially for gold on light backgrounds).
- Never rely on color alone to indicate status.

---

## 3. Brand and Visual Tone (Gold Logo Alignment)

### 3.1 How to use gold correctly
- Gold is the **brand accent**, not the background color.
- Prefer gold for:
  - Primary button accents (carefully)
  - Key highlights (badges, progress, loyalty emphasis)
  - App identity elements (top bar accent line, selected tab indicator)
- Avoid large gold surfaces that reduce legibility.

### 3.2 Overall tone
- Light theme: clean whites + warm neutrals + restrained gold.
- Typography: modern, readable, strong hierarchy.

---

## 4. Theming Strategy (Light now, Dark later)

### 4.1 Token-first approach
All UI colors must be referenced via **semantic tokens**, e.g.:
- `Color.Brand.Primary`
- `Color.Text.Primary`
- `Color.Surface.Default`
- `Color.Border.Subtle`
- `Color.Status.Success`

Never reference raw hex values directly in UI code except in the token definition files.

### 4.2 Light theme characteristics (v1)
- Backgrounds: off-white / warm white
- Surfaces: slightly elevated cards
- Borders: subtle, low-contrast
- Gold: used as accent, with adequate contrast for text and icons

### 4.3 Dark theme compatibility (v2)
- Same semantic tokens, different values
- Ensure gold remains readable on dark surfaces and does not vibrate (avoid oversaturation)

---

## 5. Localization Strategy (German first)

### 5.1 UI language rules
- All user-facing strings must be localizable from day one (even if only German exists now).
- Do not embed German strings directly inside UI components where avoidable; use resource keys.

### 5.2 Text expansion readiness
- Layouts must survive longer English strings later.
- Avoid fixed-width button labels; prefer responsive layout.

---

## 6. Phase-Aware UI (All phases visible, limited behavior)

### 6.1 Navigation rule
- Future-phase features appear in menus/navigation as **disabled items** or **non-navigating buttons**.
- Interaction pattern options (choose one consistently across the app):
  1) Disabled state + subtitle "Bald verfügbar"
  2) Tap allowed → opens a "Coming Soon" informational sheet
- Do not silently do nothing; always provide feedback.

### 6.2 Data rule
- Future-phase screens never show fake data.
- Either show:
  - Empty state with explanation, or
  - Skeleton layout without real content and with "Coming soon" message

---

## 7. Document Structure (Next Sections)

- Section 8: Color System & Design Tokens (Gold + neutrals + semantic states)
- Section 9: Typography, Spacing, Elevation, Grid
- Section 10: Core Components (Buttons, Inputs, Cards, Lists, Badges, Toasts)
- Section 11: Navigation Patterns (Tabs, Drawer, Deep links, Modals)
- Section 12: Screen Blueprints (All phases, Consumer & Business)
- Section 13: Error/Loading/Offline States
- Section 14: Accessibility Checklist
- Section 15: Implementation Notes for MAUI (ResourceDictionaries, token mapping)

---

## 8. Color System & Design Tokens (Gold-First, Light Theme v1)

### 8.1 Principles
- **Gold is an accent, not a canvas.** Use it to signal brand and key actions, not as large backgrounds.
- **Semantic tokens only.** UI code references semantic tokens (e.g., `Color.Brand.Primary`) instead of raw hex values.
- **Contrast-first.** Avoid gold text on white. If gold is used as a background, the text must be near-black (not white).

---

### 8.2 Token Layers (Required)

#### 8.2.1 Base Palette (raw colors)
These are the *only* places where hex values are allowed.

**Brand (Gold)**
- `Base.BrandGold.500`  (main accent)
- `Base.BrandGold.600`  (pressed/active)
- `Base.BrandGold.300`  (subtle highlight)
- `Base.BrandGold.800`  (dark gold for icons/borders if needed)

**Neutrals (warm, premium)**
- `Base.Neutral.0`      (pure white)
- `Base.Neutral.50`     (warm off-white background)
- `Base.Neutral.100`    (surface)
- `Base.Neutral.200`    (border)
- `Base.Neutral.600`    (secondary text)
- `Base.Neutral.900`    (primary text)

**Status**
- `Base.Success.*`
- `Base.Warning.*`
- `Base.Error.*`
- `Base.Info.*`

> Note: We will set exact hex values after the final logo gold is confirmed (or extracted). Until then, keep placeholders and enforce semantic usage.

#### 8.2.2 Semantic Tokens (UI-facing)
These are used everywhere in UI.

**Brand**
- `Color.Brand.Primary`        -> `Base.BrandGold.500`
- `Color.Brand.PrimaryPressed` -> `Base.BrandGold.600`
- `Color.Brand.Subtle`         -> `Base.BrandGold.300`
- `Color.Brand.Outline`        -> `Base.BrandGold.800`

**Text**
- `Color.Text.Primary`         -> `Base.Neutral.900`
- `Color.Text.Secondary`       -> `Base.Neutral.600`
- `Color.Text.OnBrand`         -> `Base.Neutral.900` (important: avoid white text on gold in light theme)
- `Color.Text.Disabled`        -> derived from neutrals (e.g., 40–60% opacity)

**Surfaces**
- `Color.Surface.Background`   -> `Base.Neutral.50`
- `Color.Surface.Default`      -> `Base.Neutral.0`
- `Color.Surface.Elevated`     -> `Base.Neutral.0` (with shadow/elevation)
- `Color.Surface.Disabled`     -> `Base.Neutral.100`

**Borders / Dividers**
- `Color.Border.Subtle`        -> `Base.Neutral.200`
- `Color.Border.Strong`        -> `Base.Neutral.600` (rare)

**Status**
- `Color.Status.Success`
- `Color.Status.Warning`
- `Color.Status.Error`
- `Color.Status.Info`

**States**
- `Color.State.FocusRing`      -> brand or info (must be visible)
- `Color.State.Selection`      -> `Color.Brand.Subtle` (light tint)
- `Color.State.OverlayScrim`   -> neutral w/ opacity

---

### 8.3 Gold Usage Rules (Practical)

#### Allowed (recommended)
- Primary CTA button background using `Color.Brand.Primary` with **dark text** (`Color.Text.OnBrand`).
- Selected tab indicator / active chip outline.
- Loyalty emphasis: points badge, progress bar fill, “reward unlocked” accent.
- Small decorative line under top bar, or subtle header highlight.

#### Avoid
- Gold text on white backgrounds (low contrast).
- Large gold panels or full-screen gold backgrounds.
- Using multiple gold shades in the same component without clear hierarchy.

---

### 8.4 Component-Level Color Mapping (Light Theme)

**Buttons**
- Primary: background `Color.Brand.Primary`, text `Color.Text.OnBrand`, pressed `Color.Brand.PrimaryPressed`
- Secondary: background `Color.Surface.Default`, border `Color.Border.Subtle`, text `Color.Text.Primary`
- Tertiary: text-only using `Color.Brand.Outline` (or `Color.Text.Primary` if not a brand action)
- Disabled: `Color.Surface.Disabled` + `Color.Text.Disabled`

**Cards / Lists**
- Card background: `Color.Surface.Default`
- Separator: `Color.Border.Subtle`
- Selected row: `Color.State.Selection`

**Inputs**
- Border default: `Color.Border.Subtle`
- Focus border / ring: `Color.State.FocusRing`
- Error border: `Color.Status.Error`

---

### 8.5 Cross-App Consistency (Consumer vs Business)

Both apps must share the same tokens. Differences should be limited to:
- **Density** (spacing) and component sizing
- Optional: Business may use stronger borders and higher contrast for operational clarity

Do not create separate color palettes per app; only adjust semantics if absolutely necessary.

---

### 8.6 Dark Theme Readiness (Rules Now)
- Do not assume `Color.Text.OnBrand` is always dark; it must be **token-based** so it can flip in dark theme if needed.
- Any opacity-based overlays must use semantic tokens (scrims) to avoid unreadable overlays later.

---

## 9. Typography, Spacing, Elevation, Grid

### 9.1 Principles
- **Readable first.** The apps must be comfortable for long usage sessions (especially Business on tablets).
- **Strong hierarchy.** Headings and primary actions must be visually dominant without looking aggressive.
- **Consistent rhythm.** Spacing must follow a simple scale to keep screens clean and predictable.
- **Touch-friendly.** Minimum touch targets must be respected everywhere.

---

### 9.2 Typography Scale (Token-Based)

Define a token set for typography so later changes do not require refactoring UI layouts.

#### 9.2.1 Font families
- Use the platform-default font for best readability and performance.
- If a brand font is introduced later, it must map into the same typography tokens.

#### 9.2.2 Type tokens
- `Type.Display`   (very rare: onboarding hero titles)
- `Type.H1`        (screen titles)
- `Type.H2`        (section headers)
- `Type.Body`      (default text)
- `Type.BodyStrong`(emphasis within body)
- `Type.Caption`   (secondary/meta text)
- `Type.Overline`  (labels, chips - avoid overuse)

#### 9.2.3 Suggested sizing (starting point)
These values can be adjusted during implementation, but keep the scale and naming stable.

- `Type.Display`: 28–32
- `Type.H1`: 22–24
- `Type.H2`: 18–20
- `Type.Body`: 15–16
- `Type.BodyStrong`: 15–16 (semi-bold)
- `Type.Caption`: 12–13
- `Type.Overline`: 11–12 (medium)

#### 9.2.4 Weight rules
- Titles: semi-bold
- Body: regular
- Emphasis: semi-bold
- Avoid full bold paragraphs; use weight sparingly.

#### 9.2.5 Line height
- Body text must not feel cramped.
- Use consistent line height tokens (e.g., 1.35–1.5 for body).

---

### 9.3 Spacing System (8pt Grid)

Use an 8pt grid with a small set of spacing tokens.

#### 9.3.1 Spacing tokens
- `Space.2`  = 2
- `Space.4`  = 4
- `Space.8`  = 8
- `Space.12` = 12
- `Space.16` = 16
- `Space.24` = 24
- `Space.32` = 32
- `Space.40` = 40
- `Space.48` = 48

Rules:
- Screen padding: typically `Space.16` (phone) and `Space.24` (tablet)
- Card padding: `Space.16`
- Between sections: `Space.24` or `Space.32`
- List row vertical padding: `Space.12` to `Space.16`

---

### 9.4 Touch Targets & Interaction Density

#### 9.4.1 Minimum targets (must)
- Minimum touch target: **44x44 dp**
- Primary buttons: height >= 48
- List rows: height >= 52 for comfortable tapping

#### 9.4.2 Density profiles
We will keep the same component set but allow different density presets:

- **Consumer (Comfortable)**
  - More breathing room, richer imagery, larger cards.
- **Business (Compact/Operational)**
  - Slightly denser layouts, but never below minimum touch targets.
  - Critical actions (Confirm/Cancel) remain large.

---

### 9.5 Layout Grid and Breakpoints

#### 9.5.1 Phone baseline
- Single-column layout
- Use full-width cards and lists
- Avoid side-by-side elements unless short and clearly grouped

#### 9.5.2 Tablet baseline (Business first)
- Allow two-column layouts where appropriate:
  - Left: scan/customer snapshot
  - Right: actions (accrual/redemption) and confirmation
- Ensure the primary action area remains reachable and obvious.

#### 9.5.3 Responsive rules
- Use flexible containers; avoid fixed widths.
- Allow text wrapping; do not truncate critical labels.

---

### 9.6 Elevation, Cards, and Shadows

#### 9.6.1 Elevation tokens
- `Elevation.None`
- `Elevation.1` (default cards)
- `Elevation.2` (modals, floating panels)
- `Elevation.3` (rare: very prominent overlays)

#### 9.6.2 Usage rules
- Background is flat.
- Most surfaces are cards with subtle elevation.
- Never stack many elevated layers; it creates visual noise.

#### 9.6.3 Borders vs shadows
- Prefer subtle shadow + clean surfaces.
- Use borders only when necessary for clarity (Business can use borders slightly more).

---

### 9.7 Corner Radius (Consistency)

#### 9.7.1 Radius tokens
- `Radius.8`  (small)
- `Radius.12` (default)
- `Radius.16` (large, hero cards)
- `Radius.Pill` (chips)

Rules:
- Default cards: `Radius.12`
- Primary buttons: `Radius.12` or `Radius.Pill` (choose one and stick to it)
- Chips/badges: `Radius.Pill`

---

### 9.8 Iconography

- Use a single icon set across both apps.
- Icons must not carry meaning alone; pair with text for important actions.
- Use icon tokens for sizes:
  - `Icon.16` (inline)
  - `Icon.20` (default)
  - `Icon.24` (primary actions)

---

### 9.9 Content Patterns (Readable Screens)

#### 9.9.1 Titles and sections
- Screen title (H1) at top.
- Optional subtitle/caption for guidance.
- Sections labeled by H2, separated by consistent spacing.

#### 9.9.2 Numbers and points
- For loyalty points, use a distinct but consistent style:
  - Value large (H1/H2), label small (Caption)
  - Keep formatting localized later (decimal separators etc.)

---

## 10. Core Components (Behavior, States, Consistency)

### 10.1 Principles
- Components must be **reusable**, **token-driven**, and **state-complete** (default, pressed, disabled, loading, error).
- Phase >1 UI uses the same components but may display empty/coming-soon content.
- Avoid one-off styling per screen; prefer composing screens from shared components.

---

## 10.2 Buttons

### 10.2.1 Button variants
- **PrimaryButton**
  - Use for the single most important action on a screen.
  - Example: "Anmelden", "Bestätigen", "QR anzeigen"
- **SecondaryButton**
  - Alternative or less important actions.
  - Example: "Später", "Abbrechen" (when not destructive)
- **TertiaryButton (TextButton)**
  - Low emphasis actions; mostly navigation or optional actions.
  - Example: "Details", "Mehr anzeigen"
- **DestructiveButton**
  - For irreversible actions (rare in Phase 1).
  - Must use `Color.Status.Error` semantics.

### 10.2.2 Button states (required)
- Default
- Pressed
- Disabled
- Loading (shows spinner, disables repeated taps)
- Focused (keyboard/TV/tablet accessibility)

### 10.2.3 Label rules
- German labels, short verbs.
- Avoid ALL CAPS.
- Prefer 1–2 words; if longer, allow wrapping rather than truncation.

---

## 10.3 Inputs (TextField, PasswordField, SearchField)

### 10.3.1 Input variants
- **TextField**
- **PasswordField**
  - Show/hide toggle
- **SearchField**
  - With clear button

### 10.3.2 Input states (required)
- Default
- Focused
- Filled
- Disabled
- Error (validation)
- ReadOnly (rare)

### 10.3.3 Validation rules
- Validate on submit; optionally show inline validation after first submit attempt.
- Error message must be specific and actionable (German).
- Do not show multiple stacked errors; prioritize the most important.

---

## 10.4 Cards

### 10.4.1 Card types
- **StandardCard**
  - Default container for grouped content.
- **ActionCard**
  - Entire card tappable; shows right chevron.
- **SummaryCard**
  - Key metrics: points, visit count, business summary.

### 10.4.2 Card content structure
- Title (H2 or BodyStrong)
- Optional subtitle (Caption)
- Optional trailing info (e.g., distance, status)
- Optional footer actions (Secondary/Tertiary)

### 10.4.3 Elevation and borders
- Use `Elevation.1` by default.
- Use border only for Business clarity or to separate dense lists.

---

## 10.5 Lists and Rows

### 10.5.1 List row anatomy
- Leading: optional icon/avatar/logo
- Middle: primary text + secondary text
- Trailing: chevron, badge, distance, or quick action

### 10.5.2 Row interaction rules
- If a row is tappable, it must show an affordance (chevron or clear styling).
- Rows must have minimum height >= 52dp and padding tokens.

### 10.5.3 Empty lists
- Always show an empty state component (see 10.9).
- Never show a blank screen.

---

## 10.6 Badges, Chips, and Tags

### 10.6.1 Badge usage
- Use for statuses (e.g., "Neu", "Aktiv", "In Prüfung") and loyalty states.
- Keep text short.

### 10.6.2 Chip usage
- Filters (Phase 2+), categories, favorites.
- Selected state uses `Color.State.Selection` semantics.
- Avoid excessive chip rows that overflow; keep it controlled.

---

## 10.7 Feedback Components (Toast, Snackbar, Inline Message)

### 10.7.1 Toast/Snackbar rules
- Use for short, transient feedback:
  - "Erfolgreich gespeichert"
  - "Keine Verbindung. Erneut versuchen?"
- Must not hide critical UI.

### 10.7.2 Inline message (recommended for forms)
- For validation summaries or important guidance.
- Should include action when relevant ("Erneut versuchen", "Einstellungen öffnen").

---

## 10.8 Dialogs and Bottom Sheets

### 10.8.1 Dialog rules
- Use for confirmations and destructive actions.
- Keep copy short; one clear primary action.

### 10.8.2 Bottom sheet rules
- Use for quick actions and context details.
- Do not put complex multi-step forms inside a sheet (use full screen instead).

---

## 10.9 Loading, Skeletons, and Empty States

### 10.9.1 Loading patterns
- Initial load: show skeleton layout matching final UI (preferred).
- Inline load: show spinner within the component.
- Avoid full-screen spinners unless absolutely necessary.

### 10.9.2 Empty state component (required)
A reusable component with:
- Icon/illustration (optional)
- Title (H2)
- Description (Caption/Body)
- Optional action button

Examples (German):
- "Noch keine Favoriten" + "Fügen Sie Unternehmen hinzu…"
- "Keine Daten verfügbar" + "Bald verfügbar"

---

## 10.10 Error States (Network, Unauthorized, Server)

### 10.10.1 Error component (required)
Reusable error state with:
- Title
- Short description
- Primary action: Retry
- Secondary action: Contact/Help (Phase 2+)

### 10.10.2 Unauthorized handling
- If session expired:
  - show message and route to login
  - keep user context minimal and secure

---

## 10.11 “Coming Soon” Pattern (Phase >1)

### 10.11.1 When to use
- Screens included for future phases (visible in navigation).
- Features not ready but planned.

### 10.11.2 Behavior options (choose one and use everywhere)
Option A (recommended for clarity):
- Tapping a future feature opens a "Coming Soon" bottom sheet with:
  - Title: "Bald verfügbar"
  - 1–2 bullet points of what it will do
  - Button: "Schließen"

Option B:
- Disabled navigation item with subtitle "Bald verfügbar"
- No tap interaction

### 10.11.3 Data policy
- No fake data.
- Show placeholders only if clearly labeled.

---

## 10.12 QR & Scanner UI Components (Critical)

### 10.12.1 Consumer QR Display
- Large QR area centered
- Timer/validity indicator (subtle)
- Refresh behavior must be clear (auto or manual, but consistent)

### 10.12.2 Business Scanner Screen
- Full-screen camera preview where possible
- Clear guidance text (German)
- Manual entry fallback for token (Phase 2+ optional)
- Immediate feedback after scan:
  - success: show next action
  - failure: show reason + retry

Security note:
- Never show PII on scan results beyond what is necessary for confirmation.

---

## 11. Navigation Patterns (All Phases, Phase 1 Implementation Constraints)

### 11.1 Principles
- Navigation must be **predictable**, **phase-aware**, and **deep-link ready**.
- Phase >1 destinations are part of the navigation structure, but do not navigate yet.
- Keep navigation architecture stable so later phases add behavior, not new structure.

---

## 11.2 App-Level Navigation Architecture

Both apps follow the same high-level pattern:
- **Auth Stack**
  - Login, optional onboarding
- **Main Shell**
  - Primary navigation (tabs or sidebar)
  - Standard top bar patterns
- **Modal Layer**
  - Bottom sheets, dialogs, scan confirmation

Token rule:
- Navigation styling uses the same semantic tokens (no hard-coded colors).

---

## 11.3 Consumer App Navigation (Recommended)

### 11.3.1 Primary navigation: Bottom Tabs
Consumer is discovery-driven and benefits from persistent tabs.

Recommended tabs (include all phases):
1. **QR**
   - Phase 1: QR display + refresh handling
2. **Entdecken**
   - Phase 1: map/list baseline
   - Phase 2+: details, favorites, reviews
3. **Belohnungen**
   - Phase 1: dashboard baseline
   - Phase 2+: history, promotions
4. **Profil**
   - Phase 1: profile view/edit
   - Phase 2+: settings, notifications

### 11.3.2 Secondary navigation: In-tab stacks
Each tab has its own stack.
- Example: Entdecken → Business details (Phase 2+ placeholder now)

### 11.3.3 Global top bar rules
- Screen title matches tab context
- Optional actions on the right (e.g., filter/search later)
- Avoid mixing search + filter + settings everywhere; keep them contextual.

---

## 11.4 Business App Navigation (Recommended)

### 11.4.1 Primary navigation: Sidebar / Top Tabs (Tablet-first)
Business is operational and often used on larger screens.

Preferred structure (all phases):
- **Scanner** (Phase 1 functional)
- **Transaktionen** (Phase 2+ placeholder)
- **Kunden** (Phase 2+ placeholder)
- **Rewards** (Phase 2+ placeholder)
- **Berichte** (Phase 2+ placeholder)
- **Einstellungen** (Phase 2+ placeholder)

### 11.4.2 Primary screen: Scanner as default landing
- After login, always land on Scanner.
- If the app is resumed, return to Scanner unless mid-flow confirmation is pending.

### 11.4.3 Operational flow containment
The scan → validate → confirm flow must remain in a single logical stack so the operator never gets lost.

---

## 11.5 Phase-Aware Navigation Behavior (Required)

### 11.5.1 Policy
- Phase >1 nav items exist from day one but are not implemented as real navigation yet.

Implementation constraint from project plan:
- "Only show the access button in navigation, without navigating."

### 11.5.2 How to represent future items
Choose one interaction model and apply consistently per app:

**Model A (recommended): Tap opens Coming Soon sheet**
- Pros: clear feedback; reduces confusion.
- Cons: adds a modal component (but reusable).

**Model B: Disabled nav items**
- Pros: simplest.
- Cons: users may not understand why the item exists.

Recommended:
- Consumer: Model A (better engagement and clarity)
- Business: Model B or A (depending on operator needs; B is acceptable in kiosk contexts)

---

## 11.6 Navigation Item Design Rules

### 11.6.1 Labels
- German labels only.
- Keep labels short (1 word preferred).
- Avoid abbreviations unless common (e.g., "QR").

### 11.6.2 Icons
- Use consistent icon set.
- Icons must support labels, not replace them.

### 11.6.3 Active state
- Active tab/item uses brand accent (gold) subtly:
  - underline indicator, small highlight, or icon tint
- Avoid large gold fills.

---

## 11.7 Deep-Link Readiness (Future-Proof)

Even if deep links are not implemented in Phase 1, the navigation structure should anticipate:
- `app://consumer/qr`
- `app://consumer/discover/{businessId}`
- `app://consumer/rewards`
- `app://business/scanner`
- `app://business/transactions/{id}`

Rule:
- All screens must have stable route names/keys from day one.
- Avoid renaming routes once published.

---

## 11.8 Back Navigation Rules (Non-Negotiable)

- If a screen is part of a stack, the back button must behave consistently.
- Modals (sheets/dialogs) close, not navigate.
- Never require multiple backs to exit a modal state.

---

## 11.9 Logout and Session Expiry Navigation

- Session expiry forces a route back to Login.
- Preserve minimal UI state only (do not show stale personal data).
- After re-login, return to the main default screen:
  - Consumer: QR tab
  - Business: Scanner

---

## 11.10 Future Navigation Additions (Phases 2+)
- Consumer: filters, favorites, business detail screens, promotions feed
- Business: analytics screens, reward management, subscription/stripe screens

These screens must exist as placeholders in the project structure now, but remain non-navigable per Phase 1 implementation constraint.

---

## 12. Screen Blueprints (All Phases)

### 12.1 Blueprint Format (How to read this section)
Each screen is defined by:
- **Route Key**: Stable internal route name (do not rename later).
- **Phase**: Which phase introduces real behavior.
- **Purpose**: Why the screen exists.
- **Primary UI**: The core components on the screen.
- **Data (Domain/API)**: Expected data sources (no fake data).
- **States**: Loading / empty / error / offline expectations.
- **Actions**: User actions and outcomes.
- **Phase 1 Rule**: If Phase > 1, show access in navigation but do not navigate yet.

---

# 12.2 Consumer App Screens (Darwin.Mobile.Consumer)

## 12.2.1 Auth & Entry

### Screen: Login
- **Route Key**: `consumer/auth/login`
- **Phase**: 1 (functional)
- **Purpose**: Authenticate the user to access QR, rewards, and profile.
- **Primary UI**
  - Email/username field
  - Password field (show/hide)
  - Primary: "Anmelden"
  - Secondary: "Passwort vergessen?" (Phase 2+ optional)
- **States**
  - Loading: button spinner
  - Error: inline message ("Anmeldung fehlgeschlagen. Bitte erneut versuchen.")
- **Actions**
  - Success: navigate to `consumer/main/qr`
  - Failure: show error state with retry

### Screen: Coming Soon (Reusable Sheet)
- **Route Key**: `shared/coming-soon`
- **Phase**: 1 (functional as a reusable UI pattern)
- **Purpose**: Standard response when user taps a future feature.
- **Primary UI**
  - Title: "Bald verfügbar"
  - 1–2 bullets describing the future value
  - Button: "Schließen"

---

## 12.2.2 Main Shell (Tabs)

### Shell: Consumer Main Tabs
- **Route Key**: `consumer/main`
- **Phase**: 1
- **Tabs (all phases visible)**
  1) QR -> `consumer/main/qr`
  2) Entdecken -> `consumer/main/discover`
  3) Belohnungen -> `consumer/main/rewards`
  4) Profil -> `consumer/main/profile`

Phase rule:
- Only Phase 1 tabs navigate. Future in-tab destinations exist as placeholders but are not navigable yet.

---

## 12.2.3 QR Tab (Phase 1 Core)

### Screen: QR Home (Scan Session QR)
- **Route Key**: `consumer/main/qr`
- **Phase**: 1 (functional)
- **Purpose**: Show an opaque scan token as QR to be scanned by the business device.
- **Primary UI**
  - Business selector (Phase 1 optional; if only one business is used in MVP, this can be fixed)
  - Mode selector (Accrual/Redemption) (Phase 1 minimal, Redemption can be shown as disabled if not ready)
  - QR panel (large)
  - Subtext: expiry countdown / validity hint
  - Secondary action: "Aktualisieren" (if manual refresh is supported)
- **Data (Domain/API)**
  - Token is opaque string (server-generated); QR payload contains **only the token**
  - Session properties conceptually include: mode + expiresAt
- **States**
  - Loading: skeleton QR panel
  - Error: "QR konnte nicht geladen werden" + retry
  - Expired: auto refresh if allowed, else show "Abgelaufen" + refresh
- **Actions**
  - Prepare session (server call) -> render QR
  - Refresh token (auto or manual)

### Screen: Select Rewards (Redemption) (Placeholder in Phase 1 if not implemented)
- **Route Key**: `consumer/qr/select-rewards`
- **Phase**: 2 (real behavior)
- **Purpose**: Choose reward tiers/quantities before generating a redemption token.
- **Primary UI**
  - List of reward tiers (chips or rows)
  - Points required per tier
  - Quantity stepper
  - Primary: "QR erstellen"
- **Phase 1 Rule**
  - Show entry point in QR screen as disabled or "Coming soon" sheet
- **States**
  - Empty: "Keine Belohnungen verfügbar"
  - Error: retry
- **Actions**
  - Confirm selection -> create redemption session -> return to QR Home

---

## 12.2.4 Discover Tab (Map/List)

### Screen: Discover Home (Map + List)
- **Route Key**: `consumer/main/discover`
- **Phase**: 1 (functional baseline)
- **Purpose**: Find nearby businesses and locations.
- **Primary UI**
  - Top bar: title "Entdecken"
  - Search field (Phase 2+ optional; can appear disabled in Phase 1)
  - Map preview or full map (Phase 1 baseline)
  - List of businesses/locations (cards)
- **Data (Domain/API)**
  - Business location details (name, address, city/region, country code, postal code, coordinate for map)
  - Media (logo/cover) if available
- **States**
  - Location permission denied: explain + "Einstellungen öffnen" (Phase 2+), show list-only fallback
  - Empty: "Keine Unternehmen gefunden"
  - Offline: cached list (Phase 2+), else offline message + retry
- **Actions**
  - Tap a list card -> Business Details (Phase 2+ placeholder now)

### Screen: Business Details (Placeholder)
- **Route Key**: `consumer/discover/business/{businessId}`
- **Phase**: 2
- **Purpose**: Show rich business profile: description, gallery, locations, offers, favorites, reviews.
- **Primary UI**
  - Header with logo/cover
  - Short description
  - Locations list + "Route" (directions)
  - Actions: Favorite, Like, Review (Phase 2+)
- **Phase 1 Rule**
  - Do not navigate; show "Coming soon" pattern instead.

### Screen: Location Details (Placeholder)
- **Route Key**: `consumer/discover/location/{locationId}`
- **Phase**: 2
- **Purpose**: Show branch-specific info: address, opening hours, map pin.
- **Primary UI**
  - Address block
  - Opening hours section
  - Map + directions button

### Screen: Favorites (Placeholder)
- **Route Key**: `consumer/discover/favorites`
- **Phase**: 2
- **Purpose**: Saved businesses/locations.
- **Primary UI**
  - Empty state first
  - List of favorited items

---

## 12.2.5 Rewards Tab

### Screen: Rewards Dashboard
- **Route Key**: `consumer/main/rewards`
- **Phase**: 1 (functional baseline)
- **Purpose**: Show user’s loyalty status at a glance.
- **Primary UI**
  - Points balance card (large number + caption)
  - Next reward hint (Phase 2+ if tiers are implemented)
  - Recent activity preview (Phase 2+ placeholder)
- **Data (Domain/API)**
  - Points balance and account status (Phase 1)
- **States**
  - Empty: "Noch keine Aktivitäten"
  - Error/offline: retry

### Screen: Rewards History (Placeholder)
- **Route Key**: `consumer/rewards/history`
- **Phase**: 2
- **Purpose**: Show transactions/redemptions.
- **Primary UI**
  - List grouped by date
  - Row shows: business, delta, reason/reference, timestamp

### Screen: Promotions / Feed (Placeholder)
- **Route Key**: `consumer/rewards/feed`
- **Phase**: 3
- **Purpose**: Campaign feed and push-driven content.
- **Primary UI**
  - Feed cards (title/body/media)
  - CTA actions (open business, redeem, etc.)

---

## 12.2.6 Profile Tab

### Screen: Profile Home
- **Route Key**: `consumer/main/profile`
- **Phase**: 1 (functional)
- **Purpose**: Show the current user profile and allow editing basics.
- **Primary UI**
  - Profile summary card
  - Buttons: "Profil bearbeiten"
  - Section placeholders: "Benachrichtigungen", "Sprache", "Design" (Phase 2+)
- **States**
  - Loading skeleton
  - Error + retry

### Screen: Edit Profile
- **Route Key**: `consumer/profile/edit`
- **Phase**: 1 (functional)
- **Purpose**: Update profile fields allowed by API.
- **Primary UI**
  - Form fields (minimal)
  - Primary: "Speichern"
  - Secondary: "Abbrechen"
- **States**
  - Validation errors inline
  - Save success toast

### Screen: Settings (Placeholder)
- **Route Key**: `consumer/profile/settings`
- **Phase**: 2
- **Purpose**: App settings: notifications, language, theme selection, privacy.
- **Phase 1 Rule**
  - Visible in UI as disabled or Coming Soon.

---

# 12.3 Business App Screens (Darwin.Mobile.Business)

## 12.3.1 Auth & Entry

### Screen: Login (Business)
- **Route Key**: `business/auth/login`
- **Phase**: 1 (functional)
- **Purpose**: Authenticate staff/operator and enter operational mode.
- **Primary UI**
  - Username/email
  - Password (show/hide)
  - Primary: "Anmelden"
  - Secondary (optional): "Hilfe" (Phase 2+)
- **States**
  - Loading (button spinner)
  - Error inline ("Anmeldung fehlgeschlagen. Bitte erneut versuchen.")
- **Actions**
  - Success → `business/main/scanner`
  - Failure → show error + retry

---

## 12.3.2 Main Shell (Tablet-First)

### Shell: Business Main
- **Route Key**: `business/main`
- **Phase**: 1 (shell + Scanner functional)
- **Default Landing**: `business/main/scanner`

**Navigation Items (all phases visible)**
1) **Scanner** → `business/main/scanner` (Phase 1 functional)
2) **Transaktionen** → `business/transactions/list` (Phase 2+ placeholder)
3) **Kunden** → `business/customers/list` (Phase 2+ placeholder)
4) **Rewards** → `business/rewards/home` (Phase 2+ placeholder)
5) **Berichte** → `business/reports/home` (Phase 3 placeholder)
6) **Einstellungen** → `business/settings/home` (Phase 2+ placeholder)

**Phase 1 Rule**
- Only Scanner navigates.
- Other items must be disabled or open the standard "Bald verfügbar" sheet.

---

## 12.3.3 Scanner Flow (Phase 1 Core)

### Screen: Scanner Home (Camera Scan)
- **Route Key**: `business/main/scanner`
- **Phase**: 1 (functional)
- **Purpose**: Scan customer QR (opaque ScanSessionToken) quickly and reliably.
- **Primary UI**
  - Full-screen camera preview (preferred)
  - Guidance text (German), e.g. "QR-Code des Kunden scannen"
  - Optional small actions:
    - Flash toggle (device dependent)
    - Camera switch (if available)
- **States**
  - Permission denied: explanation + "Erneut versuchen" + (Phase 2+) "Einstellungen öffnen"
  - Scanning: live preview with subtle overlay
  - Scan failure: "Ungültiger QR-Code" + retry
  - Offline: "Keine Verbindung" + retry (Phase 1 can block confirmation)
- **Actions**
  - On scan success → call `ProcessScanSessionForBusiness` (server) → go to `business/scanner/session`

Security rule:
- Do not display PII. Only show minimal session snapshot needed for confirmation.

### Screen: Scan Session (Validate & Decide)
- **Route Key**: `business/scanner/session`
- **Phase**: 1 (functional)
- **Purpose**: Show the scan session details and choose confirm action (accrual/redemption).
- **Primary UI**
  - Session summary card:
    - Mode badge: "Punkte sammeln" (Accrual) or "Einlösen" (Redemption)
    - Business/location context (if relevant)
    - Expiry indicator (subtle)
  - Customer snapshot (minimal, non-PII):
    - e.g., masked identifier or a friendly label provided by server policy
  - Primary action area:
    - If Accrual: "Bestätigen"
    - If Redemption: "Einlösung bestätigen" (or show redemption details first)
  - Secondary: "Abbrechen" (returns to scanner)
- **States**
  - Loading: skeleton session card
  - Expired: "Sitzung abgelaufen" + "Neu scannen"
  - Invalid/Rejected: reason + return to scanner
- **Actions**
  - Confirm Accrual → `business/scanner/confirm-accrual`
  - Confirm Redemption → `business/scanner/confirm-redemption`

### Screen: Confirm Accrual
- **Route Key**: `business/scanner/confirm-accrual`
- **Phase**: 1 (functional)
- **Purpose**: Final confirmation to award points.
- **Primary UI**
  - Recap card: what will happen
  - Primary: "Punkte gutschreiben"
  - Secondary: "Zurück"
- **States**
  - Loading during confirm request
  - Success: success sheet/card + auto return to scanner
  - Failure: error + retry
- **Actions**
  - Calls `ConfirmAccrual` (server)
  - On success → show confirmation → back to `business/main/scanner`

### Screen: Confirm Redemption
- **Route Key**: `business/scanner/confirm-redemption`
- **Phase**: 1 (functional if Redemption is included in Phase 1; otherwise placeholder)
- **Purpose**: Final confirmation to redeem rewards/points.
- **Primary UI**
  - Redemption summary (tier/quantity/value) as provided by server
  - Primary: "Einlösung abschließen"
  - Secondary: "Abbrechen"
- **States**
  - Loading
  - Success: confirmation + return to scanner
  - Failure: error + retry
- **Actions**
  - Calls `ConfirmRedemption` (server)
  - On success → back to scanner

### Screen: Scan Result (Reusable Outcome Sheet)
- **Route Key**: `business/scanner/result`
- **Phase**: 1 (functional, reusable)
- **Purpose**: Standardized success/failure feedback after confirm.
- **Primary UI**
  - Big status icon
  - Title ("Erfolgreich" / "Fehlgeschlagen")
  - Short description
  - Primary: "Weiter" (returns to scanner)
- **Notes**
  - Keep the operator flow fast; avoid extra steps.

---

## 12.3.4 Phase 2+ Screens (Placeholders Only in Phase 1)

### Screen: Transactions List (Placeholder)
- **Route Key**: `business/transactions/list`
- **Phase**: 2
- **Purpose**: View accrual/redemption history with filters.
- **Primary UI**
  - Date range filter (chips)
  - List grouped by date
  - Row: type badge + amount + timestamp + reference
- **Phase 1 Rule**
  - Navigation item exists but does not navigate (disabled or Coming Soon).

### Screen: Customers List (Placeholder)
- **Route Key**: `business/customers/list`
- **Phase**: 2
- **Purpose**: Find customers and view high-level stats (no PII by default).
- **Primary UI**
  - Search field
  - List rows with minimal identifiers and metrics
- **Phase 1 Rule**
  - Non-navigable entry only.

### Screen: Rewards Home (Placeholder)
- **Route Key**: `business/rewards/home`
- **Phase**: 2
- **Purpose**: Manage reward tiers and availability.
- **Primary UI**
  - Reward tier list
  - Add/edit actions
- **Phase 1 Rule**
  - Non-navigable entry only.

### Screen: Reports Home (Placeholder)
- **Route Key**: `business/reports/home`
- **Phase**: 3
- **Purpose**: Analytics dashboard + export (CSV/PDF).
- **Primary UI**
  - KPI cards
  - Charts (lightweight)
  - Export actions
- **Phase 1 Rule**
  - Non-navigable entry only.

### Screen: Settings Home (Placeholder)
- **Route Key**: `business/settings/home`
- **Phase**: 2
- **Purpose**: Device/operator settings, business context selection, session handling.
- **Primary UI**
  - Sections: Operator, Device, Security, About
  - Later: Language + Theme selection
- **Phase 1 Rule**
  - Non-navigable entry only.

---

## 13. Error, Loading, Offline, and Empty States (Unified Standard)

### 13.1 Why this matters
- Mobile UX breaks primarily due to inconsistent handling of:
  - network errors
  - offline behavior
  - permission failures
  - API validation errors
  - session expiry
- These standards must be identical across Consumer and Business to avoid UI debt.

---

## 13.2 Global State Components (Required)

### 13.2.1 LoadingState
Use when data is being fetched or an operation is in progress.
- **Variants**
  - Screen-level skeleton (preferred)
  - Inline spinner for small operations
- **Rules**
  - Avoid blocking full-screen spinners unless the whole screen truly depends on it.
  - Buttons that trigger requests must show loading + disable repeated taps.

### 13.2.2 EmptyState
Use when a valid request returns no data.
- Must include:
  - Title
  - Short explanation
  - Optional action (e.g., retry, adjust filters)
- Never show a blank screen.

### 13.2.3 ErrorState
Use when a request fails and user can retry.
- Must include:
  - Title (short)
  - Description (actionable)
  - Primary action: "Erneut versuchen"
  - Optional secondary action: "Hilfe" (Phase 2+) or "Support"
- Keep the copy in German and simple.

### 13.2.4 OfflineState
Use when connectivity is missing.
- Must include:
  - Clear offline message
  - Retry button
  - Optional explanation of what works offline (Phase 2+)
- Phase 1 rule:
  - If offline makes the flow impossible (e.g., confirm accrual), explain clearly.

---

## 13.3 Error Taxonomy (Map errors to UX responses)

### 13.3.1 Network / Connectivity
- **Symptoms**: timeout, DNS failure, no internet
- **UX**
  - Show OfflineState or ErrorState with retry
  - Keep user on the same screen; do not pop navigation unexpectedly

### 13.3.2 Server Errors (5xx)
- **UX**
  - Show ErrorState with:
    - "Serverfehler"
    - "Bitte später erneut versuchen."
  - Provide retry
- **Notes**
  - Do not show raw error codes to the user in Phase 1.
  - Log details internally (implementation later).

### 13.3.3 Client Errors (4xx)
- **401 Unauthorized**
  - Show message: "Sitzung abgelaufen. Bitte erneut anmelden."
  - Navigate to Login.
- **403 Forbidden**
  - Show: "Keine Berechtigung."
  - In Business: return to scanner (safe default).
- **404 Not Found**
  - If it is a detail screen: show "Nicht gefunden" + back.
- **409 Conflict**
  - Common for expired/consumed tokens.
  - Show: "Dieser Code ist nicht mehr gültig." + "Neu scannen."

### 13.3.4 Validation Errors
- UX pattern for forms:
  - Inline field errors under the field
  - Optional summary at top if multiple errors
- Keep messages actionable, avoid technical language.

---

## 13.4 Session Expiry (JWT/Refresh) UX Policy

- If token refresh fails:
  - Show login screen
  - After re-login:
    - Consumer → `consumer/main/qr`
    - Business → `business/main/scanner`
- Avoid loops:
  - Do not retry refresh endlessly.
- Never show stale personal data once unauthorized is detected.

---

## 13.5 Permissions UX (Camera & Location)

### 13.5.1 Camera Permission (Business Scanner)
- If denied:
  - Show a dedicated state:
    - Title: "Kamerazugriff erforderlich"
    - Description: "Bitte erlauben Sie den Zugriff, um QR-Codes zu scannen."
    - Primary: "Erneut versuchen"
    - Secondary (Phase 2+): "Einstellungen öffnen"
- Provide a fallback only if a secure manual token entry exists (Phase 2+).

### 13.5.2 Location Permission (Consumer Discover)
- If denied:
  - Show list-only mode (if server provides generic results)
  - Explain: "Standort ist deaktiviert. Ergebnisse können ungenauer sein."
  - Primary: "Erneut versuchen"
  - Secondary (Phase 2+): "Einstellungen öffnen"
- Never block the entire Discover tab solely due to missing permission.

---

## 13.6 Offline Behavior Rules (Phase 1 vs Future)

### 13.6.1 Phase 1 (strict)
- QR generation and scan confirmation typically require server calls.
- If offline:
  - Consumer QR: show error and retry; do not show an outdated QR.
  - Business confirm: block confirmation and return to scanner after message.

### 13.6.2 Phase 2+ (enhanced)
- Introduce caching for:
  - last known business list
  - basic profile data
  - last successful rewards dashboard
- UX must clearly label cached data:
  - "Zuletzt aktualisiert: …"

---

## 13.7 Standard Copy (German) for Common States

### 13.7.1 Generic
- Retry: "Erneut versuchen"
- Close: "Schließen"
- Cancel: "Abbrechen"
- Back: "Zurück"
- Save: "Speichern"
- Continue: "Weiter"

### 13.7.2 Network
- Title: "Keine Verbindung"
- Body: "Bitte prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut."

### 13.7.3 Server
- Title: "Serverfehler"
- Body: "Bitte versuchen Sie es später erneut."

### 13.7.4 Unauthorized
- Title: "Sitzung abgelaufen"
- Body: "Bitte melden Sie sich erneut an."

### 13.7.5 Invalid QR / Expired Session
- Title: "Code ungültig"
- Body: "Bitte erneut scannen."

---

## 13.8 Operational Safety Defaults (Business App)

- On any uncertain state (error/expired/unauthorized):
  - Prefer returning to `Scanner Home` after the user acknowledges the message.
- Avoid leaving the app stuck on a half-completed confirmation screen.

---

## 14. Accessibility Checklist (Light Theme v1, Dark Theme Ready)

### 14.1 Non-Negotiable Requirements
- Minimum touch target: **44x44 dp**
- All interactive elements must have:
  - Visible focus/pressed state
  - Sufficient spacing to avoid accidental taps
- Do not rely on color alone to convey meaning.

---

## 14.2 Color & Contrast
- Ensure readable contrast for:
  - Primary text on background/surfaces
  - Secondary text (must still be readable)
  - Disabled states (must remain legible, not “invisible”)
- Gold usage:
  - Avoid gold text on white.
  - If gold is a background, use near-black text (token: `Color.Text.OnBrand`).

---

## 14.3 Typography
- Avoid tiny text for critical information.
- Body text should remain readable at system font scaling.
- Allow text wrapping; do not clip labels.

---

## 14.4 Screen Reader / Semantics
- Every control must have an accessible label.
- Icons that perform actions must have text alternatives.
- Decorative icons/images must be marked as non-essential.

---

## 14.5 Focus Order & Navigation
- Focus order must follow visual order (top-to-bottom, left-to-right).
- Modals/sheets must trap focus and return focus correctly on close.

---

## 14.6 Forms & Validation
- Validation errors must be:
  - Announced
  - Visible near the field
  - Actionable (what to fix)
- Do not show multiple overlapping messages.

---

## 14.7 Motion & Feedback
- Avoid excessive animations.
- Loading indicators must not cause flicker.
- Provide clear completion feedback for critical actions (especially Business confirmations).

---

## 14.8 Camera & Location Permissions
- Permission screens must clearly explain:
  - Why permission is needed
  - What the app can and cannot do without it
- Provide "Retry" and (Phase 2+) "Open Settings" actions.

---

## 14.9 Offline and Error Accessibility
- Error and offline states must have:
  - Clear titles and descriptions
  - A single primary action (Retry)
  - Consistent placement and behavior

---

## 15. MAUI Implementation Notes (Tokens, Themes, Routing, Localization-Ready)

> Goal: Implement the UI foundation in .NET MAUI in a way that:
> - Light theme ships first
> - Dark theme can be added later by swapping token values
> - Localization (German now, English later) can be added without refactoring UI
> - Navigation skeleton for all phases exists without creating technical debt

---

## 15.1 Project Structure (Recommended)

Create a clear UI architecture that avoids mixed responsibilities:

- `Presentation/`
  - `Themes/`
    - `Tokens/`
      - `Colors.Base.xaml`
      - `Colors.Semantic.xaml`
      - `Typography.xaml`
      - `Spacing.xaml`
      - `Elevation.xaml`
      - `Radius.xaml`
    - `Themes/`
      - `Theme.Light.xaml`
      - `Theme.Dark.xaml` (later)
  - `Components/`
    - `Buttons/`
    - `Inputs/`
    - `Cards/`
    - `States/` (LoadingState, EmptyState, ErrorState, OfflineState)
    - `Sheets/` (ComingSoonSheet, ResultSheet)
  - `Navigation/`
    - `Routes.cs` (central route registry)
    - `Shell/` (ConsumerShell, BusinessShell)
  - `Screens/`
    - `Auth/`
    - `Main/`
    - `Discover/`
    - `Rewards/`
    - `Profile/`
    - `Scanner/` (Business)
    - `Placeholders/` (Phase 2+ screens)
  - `Localization/`
    - `Strings.de.resx`
    - `Strings.en.resx` (later)

Notes:
- Names can differ based on existing solution structure, but the separation must remain.
- Tokens and themes must be **shared** across both apps.

---

## 15.2 Token Strategy in MAUI (No Hard-Coded Colors)

### 15.2.1 Base Colors Dictionary
- `Colors.Base.xaml` contains raw colors (hex values).
- Only this file may contain hex values.

Example pattern:
- `Base.BrandGold.500`
- `Base.Neutral.50`
- `Base.Success.500`

### 15.2.2 Semantic Colors Dictionary
- `Colors.Semantic.xaml` maps semantic tokens to base colors.

Example:
- `Color.Brand.Primary` -> `{StaticResource Base.BrandGold.500}`
- `Color.Text.Primary` -> `{StaticResource Base.Neutral.900}`

Rule:
- UI components must only use semantic tokens like `Color.Text.Primary`.

### 15.2.3 Theme Dictionary
- `Theme.Light.xaml` imports:
  - Base colors
  - Semantic colors
  - Typography, spacing, radius, elevation
- Later `Theme.Dark.xaml` will redefine semantic mappings (or base values) without changing UI code.

---

## 15.3 Component Styling (Centralized)

### 15.3.1 Use shared styles
Define styles in dictionaries and apply them via keys:
- `PrimaryButtonStyle`
- `SecondaryButtonStyle`
- `TextFieldStyle`
- `CardStyle`
- `ListRowStyle`

Rule:
- No per-screen custom colors.
- If a screen needs a new variation, create a component/style token, not inline styling.

### 15.3.2 State completeness
Each component must cover:
- Default / Pressed / Disabled / Loading
- Error state (where applicable)

---

## 15.4 Routing and Navigation (Stable Route Keys)

### 15.4.1 Central route registry
Create a single source of truth:
- `Routes.cs` contains all route keys (strings) for all screens, including placeholders.

Rules:
- Route keys must match the blueprint section.
- Never rename a route key after release; only add new ones.

### 15.4.2 Phase-aware navigation logic
- Phase 1: only core screens actually navigate.
- Phase >1: tapping a nav item triggers:
  - either "ComingSoonSheet"
  - or a disabled state with tooltip/subtitle

Implementation rule:
- Do not leave buttons that do nothing silently.

---

## 15.5 Placeholder Screens (Phase 2+)

### 15.5.1 Placeholder pattern
Each future screen exists as:
- A minimal page with:
  - Title
  - Short description
  - "Bald verfügbar"
- No data calls.
- No fake data.

### 15.5.2 Navigation restriction (Phase 1)
Even if a placeholder screen exists, do not navigate to it yet if the plan requires “navigation buttons only”.
Use the Coming Soon sheet from Phase 1.

---

## 15.6 Localization (German now, English later)

### 15.6.1 Resource-based strings
- All user-facing strings must be loaded from resources:
  - `Strings.de.resx` now
  - `Strings.en.resx` later

Rules:
- No hard-coded German strings in XAML/pages where possible.
- Avoid concatenated sentences; use format strings with placeholders.

### 15.6.2 Layout resilience
- Allow label wrapping and dynamic widths.
- Avoid fixed-width buttons and headers.

---

## 15.7 Icon Strategy
- Use one icon set across both apps.
- Keep icons tokenized by size:
  - `Icon.16`, `Icon.20`, `Icon.24`
- Ensure accessibility labels for icon buttons.

---

## 15.8 Performance Notes (Practical)
- Prefer compiled XAML where applicable.
- Avoid heavy layout nesting; use simple containers.
- Lists:
  - Use virtualization-friendly controls
  - Avoid complex cell templates with deep nested stacks
- Images:
  - Use caching later (Phase 2+) but keep placeholders simple in Phase 1.

---

## 15.9 Security/Privacy Notes (UI-Level)
- Business app must not display PII by default.
- QR tokens are opaque; never decode/display sensitive values from QR.
- Do not show internal IDs, raw error codes, or stack traces to users.

---

## 15.10 Definition of Done for UI Foundation
- Semantic tokens implemented and used everywhere
- Light theme stable
- All navigation items exist for all phases
- Phase 1 navigation restrictions enforced
- Coming Soon pattern consistent
- Screens follow blueprint structure
- German resources applied for all strings
- Accessibility checklist passes baseline

---

## 16. UI Deliverables & Checklist per Phase (Operational Plan)

> Purpose: Keep UI implementation aligned with the roadmap while preventing UI rework.
> Rule: **All phase screens are defined and represented in navigation**, but only Phase 1 screens are functional.

---

## 16.1 Phase 1 (MVP) — Functional UI + Navigation Skeleton

### 16.1.1 Shared (Both Apps)
- [ ] Semantic token system applied everywhere (no hard-coded colors)
- [ ] Light theme stable (Theme.Light)
- [ ] All user-facing strings in German resources
- [ ] Global components available:
  - [ ] LoadingState
  - [ ] EmptyState
  - [ ] ErrorState
  - [ ] OfflineState
  - [ ] ComingSoonSheet
- [ ] Phase-aware navigation rule enforced:
  - [ ] Future-phase items visible but non-navigable
  - [ ] Interaction model consistent (disabled or ComingSoonSheet)

### 16.1.2 Consumer App — Phase 1 Functional Screens
- [ ] Login (`consumer/auth/login`)
- [ ] Main tabs shell (`consumer/main`)
- [ ] QR Home (`consumer/main/qr`) — scan token display + refresh behavior
- [ ] Discover Home (`consumer/main/discover`) — baseline list/map UI
- [ ] Rewards Dashboard (`consumer/main/rewards`) — baseline UI
- [ ] Profile Home (`consumer/main/profile`)
- [ ] Edit Profile (`consumer/profile/edit`)

### 16.1.3 Business App — Phase 1 Functional Screens
- [ ] Login (`business/auth/login`)
- [ ] Main shell with scanner as default (`business/main`)
- [ ] Scanner Home (`business/main/scanner`) — camera scan UI + permission handling
- [ ] Scan Session (`business/scanner/session`) — validate + recap
- [ ] Confirm Accrual (`business/scanner/confirm-accrual`)
- [ ] Confirm Redemption (`business/scanner/confirm-redemption`) — functional if included in Phase 1, otherwise Coming Soon only
- [ ] Result Sheet (`business/scanner/result`)

---

## 16.2 Phase 2 — Enable Navigation + Implement Secondary Features

### 16.2.1 Consumer App — Phase 2 Targets
- [ ] Business Details (`consumer/discover/business/{businessId}`)
- [ ] Location Details (`consumer/discover/location/{locationId}`)
- [ ] Favorites (`consumer/discover/favorites`)
- [ ] Rewards History (`consumer/rewards/history`)
- [ ] QR Redemption selection (`consumer/qr/select-rewards`) if applicable
- [ ] Profile Settings (`consumer/profile/settings`) with:
  - [ ] Notifications settings (if supported)
  - [ ] Language (German/English) — enable later if planned here
  - [ ] Theme toggle (Light/Dark) — may be Phase 3 depending on priority

### 16.2.2 Business App — Phase 2 Targets
- [ ] Transactions list (`business/transactions/list`)
- [ ] Customers list (`business/customers/list`)
- [ ] Rewards management (`business/rewards/home`)
- [ ] Settings home (`business/settings/home`)
- [ ] Stronger role-based UI restrictions (if roles are exposed to UI)

---

## 16.3 Phase 3 — Analytics, Push, Subscription, and Advanced UX

### 16.3.1 Consumer App — Phase 3 Targets
- [ ] Promotions / Feed (`consumer/rewards/feed`)
- [ ] Push notification UX integration
- [ ] Engagement reminders and advanced discovery

### 16.3.2 Business App — Phase 3 Targets
- [ ] Reports dashboard (`business/reports/home`)
- [ ] Export flows (CSV/PDF)
- [ ] Subscription management UX (Stripe)
- [ ] Advanced analytics visuals

---

## 16.4 Phase 1 Navigation Implementation Rule (Re-stated)
In Phase 1:
- Navigation UI must show all planned areas.
- **Only Phase 1 items navigate.**
- Phase 2/3 items:
  - Must be disabled or open ComingSoonSheet.
  - Must not silently do nothing.

---

## 16.5 QA Checklist (Phase 1 Release Gate)
- [ ] All screens render correctly on phone and tablet
- [ ] No text clipping at larger font sizes
- [ ] Primary flows require minimal taps:
  - Consumer: Login → QR
  - Business: Login → Scan → Confirm → Back to Scanner
- [ ] Permission denied states are clear (camera/location)
- [ ] Offline/network errors show consistent retry UX
- [ ] Gold accent never harms readability (contrast review)
- [ ] No fake data in placeholders

---

## 17. Route & Screen Registry (Stable Keys for Implementation)

> Purpose: Provide a single source of truth for navigation keys used across both apps.
> Rules:
> - Keys must remain stable after first release.
> - Phase >1 keys exist from day one, even if not navigable in Phase 1.
> - Use these keys in `Routes.cs` (or equivalent) and reference them everywhere.

---

## 17.1 Shared Routes

- `shared/coming-soon`  
  Reusable bottom sheet/page to explain non-available features.

---

## 17.2 Consumer Routes (Darwin.Mobile.Consumer)

### 17.2.1 Auth
- `consumer/auth/login`

### 17.2.2 Shell / Main Tabs
- `consumer/main`
- `consumer/main/qr`
- `consumer/main/discover`
- `consumer/main/rewards`
- `consumer/main/profile`

### 17.2.3 QR & Loyalty
- `consumer/qr/select-rewards`            (Phase 2+)
- `consumer/qr/history`                  (Phase 2+ optional)

### 17.2.4 Discover
- `consumer/discover/business/{businessId}`   (Phase 2+)
- `consumer/discover/location/{locationId}`   (Phase 2+)
- `consumer/discover/favorites`               (Phase 2+)

### 17.2.5 Rewards
- `consumer/rewards/history`              (Phase 2+)
- `consumer/rewards/feed`                 (Phase 3)

### 17.2.6 Profile
- `consumer/profile/edit`
- `consumer/profile/settings`             (Phase 2+)
- `consumer/profile/notifications`        (Phase 2+ optional)
- `consumer/profile/language`             (Phase 2+ optional)
- `consumer/profile/theme`                (Phase 2+ optional)
- `consumer/profile/privacy`              (Phase 2+ optional)

---

## 17.3 Business Routes (Darwin.Mobile.Business)

### 17.3.1 Auth
- `business/auth/login`

### 17.3.2 Shell / Main
- `business/main`
- `business/main/scanner`

### 17.3.3 Scanner Flow
- `business/scanner/session`
- `business/scanner/confirm-accrual`
- `business/scanner/confirm-redemption`
- `business/scanner/result`

### 17.3.4 Transactions (Phase 2+)
- `business/transactions/list`
- `business/transactions/detail/{transactionId}`   (Phase 2+ optional)

### 17.3.5 Customers (Phase 2+)
- `business/customers/list`
- `business/customers/detail/{customerId}`         (Phase 2+ optional, caution: PII rules)

### 17.3.6 Rewards Management (Phase 2+)
- `business/rewards/home`
- `business/rewards/edit/{rewardId}`              (Phase 2+ optional)
- `business/rewards/create`                       (Phase 2+ optional)

### 17.3.7 Reports (Phase 3)
- `business/reports/home`
- `business/reports/export`                       (Phase 3 optional)

### 17.3.8 Settings (Phase 2+)
- `business/settings/home`
- `business/settings/device`                      (Phase 2+ optional)
- `business/settings/security`                    (Phase 2+ optional)
- `business/settings/about`                       (Phase 2+ optional)
- `business/settings/language`                    (Phase 2+ optional)
- `business/settings/theme`                       (Phase 2+ optional)

---

## 17.4 Phase 1 Navigation Enablement Matrix (Quick Reference)

### Consumer
- Enabled in Phase 1:
  - `consumer/auth/login`
  - `consumer/main/*`
  - `consumer/profile/edit`
- Not navigable in Phase 1 (Coming Soon / Disabled):
  - all other consumer routes listed above

### Business
- Enabled in Phase 1:
  - `business/auth/login`
  - `business/main`
  - `business/main/scanner`
  - `business/scanner/*`
- Not navigable in Phase 1 (Coming Soon / Disabled):
  - all other business routes listed above

---

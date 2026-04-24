# WebAdmin Subscription Workspace Handoff

## Purpose

This document carries the durable handoff context for the `Subscription.cshtml` cleanup lane inside `Darwin.WebAdmin` so work can continue in a new chat without depending on thread history.

Primary surface:

- `E:\_Projects\Darwin\src\Darwin.WebAdmin\Views\Businesses\Subscription.cshtml`

Guard rails that must stay in sync:

- `E:\_Projects\Darwin\tests\Darwin.Tests.Unit\Security\SecurityAndPerformanceWebAdminSurfacesSourceTests.cs`
- `E:\_Projects\Darwin\tests\Darwin.Tests.Unit\Security\SecurityAndPerformanceContractsAndPackagingSourceTests.cs`
- `E:\_Projects\Darwin\BACKLOG.md`

## Current lane

The active lane is **`core WebAdmin`**, specifically the subscription workspace helper-contract cleanup in `Subscription.cshtml`.

This lane has been refactoring the page away from mixed-purpose helper naming toward:

- page-level identity and targeting contracts
- domain-specific summary, operational, and table contracts
- clearer interaction, display, state, and shell primitives
- tighter source-test guards so renames remain explicit and local

## Completed families

The following families are already implemented in the view and synced into both source-test guards plus `BACKLOG.md`.

### Page and navigation

- `...PageHref`, `...ExternalHref`, `...PostHref` were normalized to `...ActionHref`, `...ExternalActionHref`, and `...PostActionHref`
- page/header shell helpers were normalized into clearer `TitleBar...` and `WorkspaceFrame...`
- HTMX container targeting, `push-url`, and `swap` are already aligned on `SubscriptionWorkspaceFrame...`

### Summary and operational rails

- top overview sits on `OverviewSummary...`
- shared summary primitives sit on `MetricCard...`
- billing readiness sits on `BillingReadinessOverview...`
- active subscription sits on `ActiveSubscriptionOverview...`
- invoice signals sit on `InvoiceSignalsOverview...`

### Tables and list rails

- plan table sits on `AvailablePlansTable...`
- recent invoices rail sits on `RecentInvoicesTable...`
- playbooks rail sits on `BillingPlaybooksTable...`
- shared table shell sits on `PanelTable...`
- shared operational panel shell sits on `Panel...`

### Copy and display semantics

- page, section, and table copy was moved through clearer `...TitleText`, `...ColumnTitleText`, `...MetricText`, `...BadgeText`, and `...ActionText`
- display/value plumbing was moved through clearer `...DisplayText`
- state/gating semantics were normalized through clearer `...Can...`, `...Has...`, `...Shows...`, and `...StateClass`

### Shared interaction and chrome primitives

The latest completed batch moved the remaining shared interaction and surface primitives to:

- `SubscriptionUtilityActionClass`
- `SubscriptionRemediationActionClass`
- `SubscriptionInlineActionLinkClass`
- `SubscriptionProminentActionClass`
- `SubscriptionAlertMessageTextClass`
- `SubscriptionRemediationHintTextClass`
- `SubscriptionRecentInvoicesTableRowActionsClass`
- `SubscriptionOverviewSummarySurfaceClass`
- `SubscriptionOverviewSummaryContentClass`
- `SubscriptionRecentInvoicesTableCardClass`
- `SubscriptionRecentInvoicesTableCardHeaderClass`
- `SubscriptionEmptyStateActionRailClass`
- `SubscriptionBillingPlaybooksTableCardClass`
- `SubscriptionBillingPlaybooksTableRowActionsClass`
- `SubscriptionActiveSubscriptionTimelineTextClass`
- `SubscriptionInlineActionGroupClass`
- `SubscriptionStatusBadgeClass`
- `SubscriptionActionIconClass`
- `SubscriptionPanelSupportTextClass`

## Validation status

Latest validated status for this lane:

- `dotnet build E:\_Projects\Darwin\src\Darwin.WebAdmin\Darwin.WebAdmin.csproj --no-restore`
  - `0 warnings / 0 errors`
- `dotnet test E:\_Projects\Darwin\tests\Darwin.Tests.Unit\Darwin.Tests.Unit.csproj --no-restore`
  - `925/925`

Do not close a batch without re-running both commands unless the user explicitly says not to.

## Working pattern for future continuation

The user has a queue-delivery problem in the app and asked for larger, fewer turns. Continue with these assumptions:

- prefer **large coherent batches**, not tiny rename drifts
- do not stop at analysis if a concrete batch can be finished
- only stop for:
  - real technical blocker
  - conflicting user edits
  - ambiguous scope that cannot be resolved locally
- after each completed batch:
  - sync `Subscription.cshtml`
  - sync both source-test guard files
  - add a matching note to `BACKLOG.md`
  - run build and unit tests

## Recommended next work

The obvious remaining work is no longer broad generic cleanup. The next useful passes should be chosen as **one coherent family at a time**.

Recommended order:

1. Audit the remaining helper names in `Subscription.cshtml` for any last mixed families that still do not follow one of the current domain vocabularies:
   - `TitleBar`
   - `WorkspaceFrame`
   - `OverviewSummary`
   - `MetricCard`
   - `Panel`
   - `PanelTable`
   - `AvailablePlansTable`
   - `RecentInvoicesTable`
   - `BillingPlaybooksTable`
2. If any older mixed pockets remain, close them in one batch across:
   - view
   - both source tests
   - backlog
3. If the helper cleanup is effectively complete, move from naming cleanup to the next `core WebAdmin` lane rather than inventing more renames

## Open platform priorities

These are the broader headings still intentionally left open for the main WebAdmin program:

- `Settings UI and architecture`
- `Localization readiness`
- `Communication Core (email-first MVP)`
- `Business and tenant onboarding`
- `Access and support operations`

## Suggested starter checklist for a new chat

In a new chat, start by:

1. reading this handoff document
2. reading the current top section and recent notes in `BACKLOG.md`
3. scanning `Subscription.cshtml` for remaining mixed naming pockets
4. choosing one large coherent batch
5. syncing the two source-test files and backlog
6. running build and tests

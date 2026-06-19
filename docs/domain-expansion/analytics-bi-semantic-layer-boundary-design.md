# Analytics And BI Semantic Layer Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's Analytics/BI semantic layer boundary. It is documentation-only and adds no entity, migration, route, DTO, dashboard runtime, worker, export format, WebAdmin action, or mobile contract.

Decision: Analytics must define governed metrics and report semantics over authoritative module records. It must not mutate source modules, duplicate ledgers, or expose disabled/private module data outside audience rules.

## Current Darwin Analytics Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| Finance reporting queries | Finance reports and export over posted journal entries exist. | Keep finance reports as authoritative financial views. |
| `AnalyticsExportJob` and `AnalyticsExportFile` | Export job/file entities exist in Integration. | Use as export evidence, not the semantic metric catalog itself. |
| AI scoped context projection | Aggregated module context exists for internal AI. | Analytics can reuse aggregation discipline but needs user-facing metric governance. |
| Module data | CRM, sales, inventory, finance, HR, payroll, treasury data exists. | Analytics reads only permitted/enabled module data. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Metric catalog | Management can trust KPI definitions. | Requires metric definition, owner, grain, formula, source, freshness. |
| Dashboards | Operators can monitor performance. | Requires dashboard definition, widgets, filters, audience permission. |
| Scheduled materialization | Large reports can run reliably. | Requires worker policy, freshness, and disabled-mode skip. |
| Export/share | Reports can be handed to stakeholders. | Requires document/export evidence and audience scope. |
| Data quality notes | Avoid misleading reports. | Requires caveat/status fields and validation evidence. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `SemanticMetric` | analytics | code, name, owner capability, formula description, grain, source tables/queries, status. |
| `MetricDimension` | analytics | metric, dimension code, data type, allowed values/source. |
| `DashboardDefinition` | analytics | business/tenant, code, audience, status, layout metadata. |
| `DashboardWidget` | analytics | dashboard, metric/report source, visualization type, filters, sort order. |
| `MetricMaterializationRun` | analytics | metric/dashboard, period, status, row count, generated timestamp, evidence document. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Metric | `Draft -> Reviewed -> Active -> Deprecated -> Archived`; formula changes create a new version. |
| Dashboard | `Draft -> Active -> Archived`; audience changes are audited. |
| Materialization run | `Queued -> Running -> Completed` or `Failed`; failed runs do not publish stale success. |

## Application Surface

Future handlers:

- Manage metric definitions and versions.
- Manage dashboard definitions/widgets.
- Query governed metrics with audience/capability checks.
- Run materialization jobs.
- Export dashboard/report package with document evidence.

## WebAdmin Surface

Analytics WebAdmin should include metric catalog, dashboard catalog, report preview, materialization runs, data quality status, and read-only links to source modules where allowed.

Public/member/mobile dashboards require separate audience contract design.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | `analytics`. |
| Package role | Add-on to Enterprise/Operations/Finance packages. |
| Required dependencies | At least one source module; source module gates still apply. |
| Disabled behavior | Hide analytics nav; workers skip analytics materialization; existing exports remain evidence if allowed. |
| Permissions | Manage metrics, manage dashboards, view dashboard, export report. |
| Privacy | Payroll, HR, finance, and customer data require stricter audience scopes. |

## Compatibility Boundaries

- Analytics never mutates module records.
- Analytics must not read disabled capability data for a tenant unless historical legal/reporting policy explicitly allows a read-only exception.
- Finance export package format remains unchanged.
- No public/mobile exposure in this design.

## Implementation Slices

1. `Semantic Metric Catalog Core Slice`.
2. `Dashboard Definition Core Slice`.
3. `Metric Query Authorization Slice`.
4. `Materialization Worker Design/Slice`.
5. `Dashboard Export Evidence Slice`.

## Test Plan

Future tests must cover metric versioning, source capability checks, audience privacy, no source mutation, stale/failure handling, WebAdmin guards, worker skip, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement Analytics/BI.

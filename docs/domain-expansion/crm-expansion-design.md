# CRM Expansion Design

This document defines the next CRM expansion direction after the foundation primitives are in place. It is a design artifact only. It does not change entities, migrations, API routes, DTOs, mobile contracts, production flows, or existing CRM behavior.

CRM expansion must reuse Darwin-owned foundation primitives instead of adding scattered JSON fields, duplicated ledgers, parallel invoice models, or new sources of truth. Any mobile, loyalty, identity, customer bridge, order, or invoice gap discovered during CRM work must be handled before mobile release.

## Current Darwin CRM Findings

- `Customer` is the CRM lifecycle, segmentation, tax/commercial, and fallback profile aggregate.
- Linked customers use `Customer.UserId`; linked display/contact data is resolved from `User` instead of being duplicated in CRM.
- Unlinked customers keep fallback CRM profile and contact fields on `Customer`.
- `Customer` must not own auth secrets, device tokens, provider tokens, refresh tokens, phone verification tokens, push tokens, or loyalty balances.
- `CustomerAddress` exists for CRM-owned addresses and must stay compatible with canonical address mapping.
- `Lead`, `Opportunity`, `Interaction`, `Consent`, `CustomerSegment`, `CustomerSegmentMembership`, `Invoice`, and `InvoiceLine` already exist in the current CRM model.
- `Invoice` is shared by order and CRM scenarios; CRM expansion must evolve this shared invoice model and must not create a parallel CRM invoice, sales invoice, or finance invoice for the same issued-document surface.
- Existing mobile/member profile, loyalty, order, and invoice contracts remain compatibility-sensitive and are not redesigned by CRM expansion.

## CRM Decision Matrix

| CRM surface | Current Darwin model | Foundation primitive to reuse | Decision | Schema/API impact | Mobile/loyalty impact | Priority | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer source of truth | `Customer` has `UserId` plus fallback name, email, phone, tax, company, notes, segments, addresses, interactions, consents, opportunities, and invoices. | `ExternalReference`, `CustomFieldDefinition`, `CustomFieldValue`, `Activity`, `Note`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`. | Keep `Customer` as CRM lifecycle and fallback aggregate; linked profile/contact comes from `User`; no loyalty balance or auth state on `Customer`. | Future schema may add real columns only for common lifecycle, owner, classification, reporting, and compliance fields. | High if linked profile/contact behavior changes; default is no mobile contract change. | P0 for CRM design. | Define exact CRM core fields in the next implementation slice before adding columns. |
| Customer addresses | `CustomerAddress` has CRM-owned line/city/state/postal/country and optional `AddressId`. | Canonical address mapper. | Preserve current DTO/wire shapes; use canonical mapping internally for new CRM address flows. | No immediate schema change; future address changes must remain compatible with snapshots and existing contracts. | Medium if exposed through profile or checkout; default is no mobile contract change. | P0 for CRM design. | Require all new CRM address handling to pass through canonical mapping. |
| Linked customer profile | Member/customer profile context already bridges `Customer` and `User`. | `ExternalReference` only if imported identity/customer links are needed later. | Linked customer display/contact is resolved from `User`; CRM fallback fields remain for unlinked customers. | No immediate API change. | High if changed; do not change in this design slice. | P0. | Keep source-of-truth behavior explicit in CRM handlers and future tests. |
| Lead | `Lead` has name, company, email, phone, source, notes, status, assigned user, converted customer, and interactions. | `ExternalReference`, `CustomFieldValue`, `Activity`, `Note`, `DocumentRecord`, `BusinessEvent`. | Keep `Lead` as pre-customer lifecycle record; use real columns for status, owner, source, conversion, and reportable qualification data. | Future schema may add qualification and lifecycle columns; no API change in this step. | Low unless lead appears in mobile; default is no mobile exposure. | P1. | Design lead qualification fields and conversion rules in CRM core model expansion. |
| Opportunity | `Opportunity` has customer, title, estimated value, stage, expected close date, assigned user, items, and interactions. | `ExternalReference`, `CustomFieldValue`, `Activity`, `Note`, `DocumentRecord`, `NumberSequence`, `BusinessEvent`, `AuditTrail`. | Keep opportunity as CRM sales pipeline record; avoid creating a separate sales document model inside CRM. | Future schema may add probability, close reason, source, forecast category, and sequence-backed opportunity number if needed. | Low for mobile release. | P1. | Define pipeline fields and event/audit transitions before schema changes. |
| Opportunity items | `OpportunityItem` links opportunity to product variant, quantity, and unit price. | `CustomFieldValue`, `DocumentRecord`. | Keep as lightweight proposed line items; do not turn this into order/invoice lines. | Future schema may add currency, discount, tax intent, and pricing snapshot only if sales design requires it. | Low. | P2. | Defer detailed quoting behavior to sales/order document expansion. |
| Interaction | `Interaction` exists as CRM-specific email/call/note/timeline record tied to customer, lead, or opportunity. | `Activity`, `Note`, `BusinessEvent`. | Keep existing `Interaction` for compatibility; new shared timeline behavior should use `Activity` and `Note` where cross-module timeline is needed. | No immediate migration; future coexistence policy must prevent duplicate timeline rows. | Low unless surfaced in mobile. | P1. | Define coexistence rules before migrating or projecting interactions into shared timeline. |
| Consent | `Consent` stores customer consent type, granted state, and grant/revoke timestamps. | `AuditTrail`, `BusinessEvent`. | Keep consent as structured CRM/compliance data; do not model consent as custom fields. | Future schema may add source, evidence, policy version, channel, and actor columns if needed. | Medium if member-facing preferences use the same consent. | P1. | Design consent evidence and profile-preference interaction before implementation. |
| Customer segments | `CustomerSegment` and membership links exist. | `CustomFieldValue`, `BusinessEvent`, `FeatureArea`. | Keep manual segments structured; use custom fields only for deployment-specific segmentation attributes. | Future schema may add segment code, status, scope, and rule metadata. | Medium if loyalty campaigns consume segments. | P1. | Define segment ownership and campaign eligibility boundaries. |
| Customer notes | `Customer.Notes` and CRM `Interaction.Content` can hold operator text. | `Note`, `Activity`. | New human notes should use shared `Note`; existing notes remain until a deliberate migration is designed. | No immediate schema or API change. | Low. | P2. | Stop adding new large free-text note columns unless a specific compliance reason exists. |
| CRM documents and attachments | CRM currently relies on specialized invoice archive and media/document-related flows elsewhere. | `DocumentRecord`. | CRM attachments should register metadata through `DocumentRecord`; upload/download behavior is not changed here. | Future API may expose CRM document metadata separately; no route now. | Low unless documents are mobile-visible. | P2. | Define allowed document kinds and storage references in CRM implementation planning. |
| CRM external identity | Some current areas have provider-specific or source payloads, but CRM needs structured coexistence. | `ExternalSystem`, `ExternalReference`, `SourceOfTruth`. | Use `ExternalReference` for imported or synchronized `Customer`, `Lead`, `Opportunity`, and `Invoice` identity. | No direct column on CRM records unless source-of-truth must be queried frequently. | Low to medium if mobile-visible records are externally sourced. | P0. | Add CRM reference usage in service layer when imports/integrations are implemented. |
| CRM custom data | Current model has real columns plus several notes/source fields. | `CustomFieldDefinition`, `CustomFieldValue`. | Common/reportable/cross-module fields become columns; uncertain, customer-specific, or low-frequency fields use custom fields. | Future schema decisions must justify every new column versus custom field. | Low unless custom fields are exposed in mobile forms. | P0. | Create a field classification list before CRM schema changes. |
| CRM document numbers | Current opportunity/lead/customer records do not have a shared sequence policy. | `NumberSequence`. | Use `NumberSequence` only for business-facing document or case numbers that users need to reference. | No existing numbers are rewritten. | Low. | P2. | Decide whether leads/opportunities need visible numbers during CRM implementation planning. |
| CRM lifecycle events | Lifecycle changes are handled by current commands without shared event instrumentation. | `BusinessEvent`, `AuditTrail`. | Instrument important CRM transitions later; do not add event writes in this design step. | Future handler changes should be additive and internal. | Low unless events drive mobile-visible state. | P1. | Identify lead conversion, opportunity stage changes, consent changes, and customer merge/link events as event candidates. |
| CRM module visibility | Current permissions/navigation remain the access source. | `FeatureArea`, `BusinessFeatureOverride`. | Use feature areas for CRM visibility and packaging only; do not replace authorization policies. | No UI/API gating in this design step. | Low unless CRM surfaces become mobile-visible. | P2. | Define stable CRM feature codes before wiring UI navigation. |
| Shared invoice boundary | `Invoice` supports order and CRM scenarios and has archive/source-model fields. | `NumberSequence`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`. | Evolve the existing shared `Invoice`; do not create a parallel CRM invoice or finance invoice for the same issued document. | Future sales/finance work must preserve archive and source-model behavior. | High for member invoice history/download; default is no contract change. | P0. | Keep invoice expansion under sales/finance design, not CRM-only implementation. |

## Field Storage Rules

| Field type | Storage decision | Examples |
| --- | --- | --- |
| Frequent, reportable, filterable, compliance-relevant, integration-key, or cross-module data | Real column on the owning entity or a structured foundation entity. | Customer lifecycle status, owner, type, tax profile, consent policy version, opportunity stage, source-of-truth reference. |
| Customer-specific, uncertain, low-frequency, deployment-specific, or experimental data | `CustomFieldDefinition` and `CustomFieldValue`. | Local scoring attributes, temporary qualification details, deployment-only tags. |
| Human collaboration text | `Note` unless an existing compatibility field already owns the data. | Operator note, follow-up note, internal handoff note. |
| Timeline facts and operational events | `Activity` for collaboration timeline; `BusinessEvent` or `AuditTrail` for evidence and automation context. | Lead converted, opportunity won, consent revoked, customer linked to user. |
| External identity | `ExternalReference` with `SourceOfTruth`; never ad hoc JSON-only identity. | External customer id, external lead id, external opportunity id, external invoice id. |
| Documents and attachments | `DocumentRecord` metadata only; payload storage stays with existing storage services. | Contract file, customer document, opportunity attachment. |

## Implementation Boundaries

- This design does not redesign loyalty, mobile auth/profile, business access, order snapshots, invoice archive/download, or member-facing contracts.
- CRM must not create duplicate loyalty balances, duplicate linked profile/contact state, duplicate business access state, duplicate issued invoice models, or duplicate external identity fields.
- CRM schema changes must be introduced in a later slice with focused migrations and tests.
- CRM APIs or UI changes must be additive unless a deliberate pre-release migration is explicitly approved and tested.
- Feature visibility and permissions remain separate concerns: `FeatureArea` can hide or package CRM surfaces, while authorization still comes from existing roles and policies.

## Next Implementation Slice

`CRM Core Model Expansion Slice`

Goal: add only the first confirmed CRM domain/schema changes after this design. The first implementation should focus on source-of-truth-safe CRM fields and foundation primitive usage, not broad UI expansion.

Default first scope:

- Classify proposed `Customer`, `Lead`, `Opportunity`, `Consent`, and `CustomerSegment` fields into real columns versus custom fields.
- Add only the highest-confidence CRM columns needed for lifecycle, ownership, reporting, integration identity, and compliance.
- Add internal service usage for `ExternalReference` where CRM import/link identity is required.
- Keep existing mobile, loyalty, order, and invoice contracts unchanged.
- Add tests for source-of-truth behavior, field validation, migration shape, and no contract drift.

## Implementation Outcome

`CRM Core Model Expansion Slice` adds the first confirmed CRM schema expansion without changing public/mobile contracts.

Implemented foundation-aligned CRM fields:

- `Customer`: lifecycle status, owner, acquisition source, preferred contact channel, last contacted timestamp, and next follow-up timestamp.
- `Lead`: priority, qualification timestamp, disqualification timestamp, conversion timestamp, and closed reason.
- `Opportunity`: currency, probability, forecast category, closed timestamp, close reason, and source.
- `Consent`: source, policy version, and evidence JSON.
- `CustomerSegment`: normalized code, active flag, and rule JSON.

Compatibility decisions kept:

- `Customer` still does not own auth secrets, device tokens, provider tokens, phone verification tokens, push tokens, or loyalty balance.
- `Interaction`, `Invoice`, `OpportunityItem`, and `CustomerAddress` are not redesigned by this slice.
- No public/mobile route, DTO, loyalty contract, order snapshot, invoice archive, or member-facing download behavior changes.
- External identity, shared timeline, document metadata, business events, audit trails, and feature visibility remain available foundation primitives for a later CRM integration slice.

The next planned slice is `CRM Foundation Primitive Integration Slice`, focused on controlled use of `ExternalReference`, `Activity`, `Note`, `DocumentRecord`, and event/audit records only where CRM flows need them.

## Foundation Primitive Integration Outcome

`CRM Foundation Primitive Integration Slice` connects CRM to the existing foundation primitives without adding schema or changing public/mobile contracts.

Implemented internal integration points:

- `CrmFoundationPrimitiveService` wraps external references, CRM notes, CRM activities, document metadata, and lifecycle event/audit recording.
- CRM external identity uses `ExternalReference`; no CRM entity receives scattered external-id columns or JSON-only identity fields.
- Lead conversion records idempotent lifecycle evidence and timeline activities for the lead and linked customer.
- Lead and opportunity lifecycle actions record internal event/audit evidence and timeline activity after successful domain changes.
- Consent creation records compliance event/audit evidence and a customer timeline activity.
- Existing `Customer.Notes`, `Lead.Notes`, `Interaction`, invoice archive, and upload/download behavior are not migrated or replaced.

## CRM UI/Admin Exposure Outcome

`CRM UI/Admin Exposure Slice` exposes CRM foundation evidence in WebAdmin without adding schema or changing public/mobile contracts.

Implemented WebAdmin scope:

- Existing `Customer`, `Lead`, and `Opportunity` editor pages can show external references, activities, notes, document metadata, business events, and audit trail evidence.
- WebAdmin can add an internal foundation note for those CRM records through the shared `Note` primitive.
- External references, document metadata, events, and audit records are read-only in this slice.
- `Interaction`, `Customer.Notes`, `Lead.Notes`, invoice archive/download behavior, upload/download flows, and existing CRM fields are not migrated or replaced.
- No public route, mobile route, mobile DTO, loyalty contract, order snapshot, invoice archive, or member-facing contract changes are introduced.

The next domain step should move to `Sales And Order Document Model Design` if this CRM admin exposure is sufficient. If operators need import/link management before sales design, use a focused `CRM External Reference Management Slice` first.

# Darwin Domain Capability Catalog

This catalog records candidate domain capabilities before Darwin entity, migration, API, mobile-contract, or UI work starts. It is a planning and decision document, not an implementation claim.

This document records Darwin-owned product and domain decisions. Do not copy external schemas, legacy framework concepts, customer-specific customizations, vendor-specific names, or non-English names into Darwin. Accepted concepts must become Darwin-owned English concepts that fit the existing architecture.

## Decision Rules

- `Canonical`: broadly useful for Darwin's core ERP and likely to become a structured entity or column.
- `Optional`: useful for a supported area but not required in every deployment.
- `Extension`: keep as custom-field metadata, JSON, or provider payload until repeated usage justifies structure.
- `Industry-specific`: defer to a vertical extension unless needed by the current product scope.
- `Customer-specific`: do not add to the canonical model.
- `Legacy technical`: do not import; use only to understand historical workflows.
- `Deferred`: keep the concept visible but postpone design.

Storage shape decisions:

- Use real columns for common, reportable, filterable, compliance-relevant, accounting-relevant, inventory-relevant, integration-key, or cross-module fields.
- Use custom fields or JSON for customer-specific, uncertain, low-frequency, provider-specific payload, industry-specific, or unstructured metadata.
- Keep external integration fields explicit when source-of-truth, sync state, or conflict handling matters.

Priority rules:

- `P0`: must be resolved before mobile/loyalty launch if it affects `User`, `Business`, `BusinessMember`, `Customer`, `Address`, order/invoice snapshots, WebApi contracts, or mobile contracts.
- `P1`: foundation or ERP-core design needed before implementation begins.
- `P2`: important capability for a later implementation slice.
- `P3`: later-phase or deployment-specific capability.

## Mobile And Loyalty-Safe Foundation Review

This review identifies foundation records and contracts that can affect the current loyalty and mobile release path. Any accepted change in these areas must be handled before mobile release if delaying it would force a breaking mobile contract, a disruptive migration, duplicated domain state, or mobile downtime later.

The loyalty ledger and mobile-facing loyalty contracts are not part of the general ERP redesign track, but they are not protected from necessary change. If the review finds that a loyalty or mobile-facing change is required for the target foundation, it must be designed and implemented before mobile release. If no change is required, that decision must be explicit and backed by current contract compatibility.

| Surface | Current dependency | Affected records/contracts | Risk if changed later | Decision | Storage shape | Required before mobile release | Recommended next action | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Identity profile | Mobile auth, profile, password, external-login, token refresh, account deletion, and member profile flows depend on identity records and member profile contracts. | `User`, `UserToken`, `UserLogin`, `UserDevice`, `UserWebAuthnCredential`, identity auth contracts, profile contracts, shared mobile auth services. | Breaking auth/profile contracts after mobile release would require app updates, migration support, and compatibility fallbacks. | Needs follow-up design | Real columns for identity, contact, verification, security, and integration identifiers; JSON/custom fields only for optional profile metadata. | Yes | Audit identity/profile contract shape before any ERP foundation change. Avoid changing issued mobile auth routes without versioning. | Keep provider tokens and secrets out of domain records and logs. |
| Address foundation | Mobile profile, member addresses, business discovery, business locations, orders, invoices, and shipping snapshots all use address-like data. | `Address`, `CustomerAddress`, `BusinessLocation`, order billing/shipping snapshots, invoice snapshots, business location contracts, profile address contracts. | Keeping parallel address shapes too long can force duplicate mapping, inconsistent validation, and mobile contract churn. | Change before release | Real columns for common address fields, country, postal code, coordinates, defaults, and validation state; JSON/custom fields for delivery instructions and customer-specific metadata. | Yes | Decide the canonical structured address shape and compatibility strategy before new ERP sales, purchasing, shipping, or warehouse records depend on it. | Preserve immutable order/invoice snapshots even if canonical address records change later. |
| Business master data | Business app access state, invitations, staff access, loyalty program ownership, public discovery, media, subscription status, and WebAdmin onboarding depend on business records. | `Business`, `BusinessLocation`, `BusinessMember`, `BusinessInvitation`, `BusinessMedia`, business access-state contracts, onboarding contracts, public business contracts. | Late business master-data changes can break business app gates, staff access, onboarding, public discovery, and loyalty ownership. | Needs follow-up design | Real columns for legal/display identity, status, ownership, localization, feature visibility, integration identity, and public discovery fields; JSON/custom fields for branding or vertical-specific settings. | Yes | Review business master fields needed by ERP foundation, mobile access gates, and loyalty ownership before adding ERP module visibility or external references. | Module separation remains logical through UI, permissions, and feature visibility, not project/database splits. |
| Business membership and staff access | Business mobile operations, invitations, staff QR, scan processing, role gates, and support workspaces depend on staff/member state. | `BusinessMember`, `BusinessInvitation`, `BusinessStaffQrCode`, business auth contracts, access-state response, staff badge contracts. | Delayed role/staff redesign can create incompatible mobile access gates and duplicated employee/staff concepts when HR is added. | Needs follow-up design | Real columns for role, status, invitation, ownership, staff access, and business linkage; custom fields only for HR-specific optional metadata until HR design is approved. | Yes | Decide how future `Employee` links to `BusinessMember` without replacing current business access contracts. | Do not make HR employee records the source of truth for mobile staff access in this step. |
| CRM customer bridge | Loyalty member views, member commerce, invoices, tax profile, customer context, and account/profile linkage rely on customer records. | `Customer`, `CustomerAddress`, customer profile context, member commerce contracts, invoice/customer links. | Late customer/account redesign could duplicate identity profile data, break invoice ownership, or create a second loyalty/customer ledger. | Needs follow-up design | Real columns for identity link, customer type, tax profile, VAT status, lifecycle, owner/business link, and integration identity; custom fields for segmentation or deployment-specific metadata. | Yes | Define the customer/account bridge before CRM expansion and before adding external references to customer-facing records. | CRM must not own loyalty balances; loyalty totals remain projections from loyalty records. |
| Loyalty ledger and scan contracts | Consumer and business mobile apps depend on account summaries, rewards, campaigns, scan preparation, scan processing, accrual, redemption, and QR/session contracts. | `LoyaltyProgram`, `LoyaltyAccount`, `LoyaltyPointsTransaction`, `LoyaltyRewardTier`, `LoyaltyRewardRedemption`, `ScanSession`, `QrCodeToken`, loyalty contracts and shared mobile loyalty services. | Discovering required ledger or contract changes after mobile release could force app downtime, emergency compatibility code, or a disruptive data migration. | Needs follow-up design | Keep current structured loyalty entities and contracts unless the foundation review proves a required change; use projections for mobile/member views and real columns for any cross-module loyalty identity or reporting fields that become required. | Yes, for any required contract, ledger, identity-link, or reporting change; otherwise document explicit no-change decision | Validate loyalty ledger, QR/session, reward, campaign, accrual, redemption, and mobile contract compatibility before release. Implement necessary changes now, or record why the current shape is stable enough for release. | Explicitly no CRM-owned loyalty ledger and no second points balance source. |
| Member commerce records | Mobile member commerce pages and invoice artifact downloads depend on order, invoice, payment, shipment, and archive contract stability. | `Order`, `OrderLine`, `Invoice`, `InvoiceLine`, `Payment`, `Shipment`, member order/invoice contracts, invoice artifact routes, source-model/archive downloads. | Changing snapshots after release can invalidate mobile invoice/order displays, archive downloads, and compliance evidence. | Needs follow-up design | Real columns for totals, currency, status, business/customer/order/payment links, tax/compliance state, provider references, and archive metadata; JSON only for immutable snapshots and provider/source payloads. | Yes | Review order/invoice snapshot compatibility before sales-document and finance expansion. Preserve existing archive/source-model contract behavior. | Do not replace current invoice model with a parallel sales invoice model. |
| Mobile API boundary | Shared mobile route catalog, mobile services, launch guards, and serialization tests define the current mobile contract boundary. | `ApiRoutes`, shared mobile API services, launch readiness guard tests, contract serialization tests, route alias/source-contract tests. | Route or contract drift after release forces app updates and weakens launch guard value. | Change before release | Structured route and DTO contracts; JSON only inside explicit payload fields already designed for snapshots or metadata. | Yes | Treat mobile-used routes and DTOs as compatibility-sensitive. Update source-contract and serialization tests before any breaking change is accepted. | New ERP routes should be additive and audience-scoped; do not overload existing mobile routes for back-office ERP workflows. |

## Foundation

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target ERP scope | Party master data | Party / Organization / Person | Foundation | Deferred | Structured entities after design approval | Possible if linked to `Customer`, `User`, or `Business` | High | Resolve without disrupting existing mobile identity and loyalty flows. |
| Darwin current model, target ERP scope | External system identity | ExternalSystem / ExternalReference | Foundation | Canonical | Structured entities or owned references | Possible if added to mobile-facing records | High | Required for Darwin as primary ERP and for coexistence with external ERP/CRM/accounting systems. |
| Darwin current model | Custom metadata | CustomFieldDefinition / CustomFieldValue | Foundation | Canonical | Structured definitions plus typed/JSON values | Low unless exposed in mobile forms | Medium | Use for customer-specific and uncertain fields instead of adding columns. |
| Darwin current model | Activity and notes | Activity / Note | Foundation | Canonical | Structured | Medium if surfaced in business/mobile support flows | Medium | Shared by CRM, support, sales, purchasing, and HR. |
| Darwin current model | Numbering policy | NumberSequence | Foundation | Canonical | Structured | Low | Medium | Needed for sales, purchasing, finance, inventory, and integrations. |

## CRM

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target CRM scope | Account and contact hierarchy | Account / Contact | CRM | Canonical | Structured | Possible through customer/member profile projections | High | Complements existing `Customer`, `Lead`, and `Opportunity`; does not own loyalty balances. |
| Darwin current model | Customer profile | Customer | CRM | Canonical | Structured | High | High | Changes must be reviewed before mobile release if contracts or projections change. |
| Darwin current model | Lead lifecycle | Lead | CRM | Canonical | Structured | Low | Medium | Strengthen owner assignment, conversion, provenance, and status rules. |
| Darwin current model | Opportunity pipeline | Opportunity / OpportunityLine | CRM | Canonical | Structured | Low | Medium | Keep separate from sales orders until conversion. |

## Sales

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target sales scope | Sales document lifecycle | SalesQuote / SalesOrder / SalesInvoice | Sales | Canonical | Structured with snapshots | Medium if member orders or invoices change | High | Build on existing order and invoice models, not as a parallel invoice ledger. |
| Target sales scope | Delivery document | DeliveryNote | Sales | Optional | Structured | Low | High | Connects sales, shipping, and inventory fulfillment. |
| Target sales scope | Credit and return flow | CreditNote / ReturnOrder | Sales | Canonical | Structured | Medium if member return visibility is added | High | Must align with refunds, shipments, and stock consequences. |

## Purchasing

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target purchasing scope | Supplier master data | Supplier / SupplierContact | Purchasing | Canonical | Structured | Low | High | Extend existing supplier model with contacts, external references, and terms. |
| Darwin current model, target purchasing scope | Purchase order lifecycle | PurchaseRequest / PurchaseOrder / PurchaseOrderLine | Purchasing | Canonical | Structured | Low | High | Existing purchase order model should evolve rather than be replaced blindly. |
| Target purchasing scope | Receiving lifecycle | GoodsReceipt | Purchasing | Canonical | Structured | Low | High | Drives inventory ledger entries and supplier invoice matching. |

## Inventory

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target inventory scope | Warehouse structure | Warehouse / WarehouseLocation / Bin | Inventory | Canonical | Structured | Possible for business app only | High | Extend existing warehouse model beyond a location string. |
| Darwin current model, target inventory scope | Stock accounting | StockBalance / StockLedgerEntry | Inventory | Canonical | Structured | Low | High | Ledger should become source of audit truth; balances are derived or maintained projections. |
| Target inventory scope | Warehouse work execution | WarehouseTask / PickingTask / ReceivingTask | Inventory | Canonical | Structured | Medium for future warehouse mobile/PWA | Medium | Plan mobile-first PWA by default, with native app only if hardware/offline requirements require it. |
| Target inventory scope | Traceability | Lot / SerialNumber / HandlingUnit | Inventory | Optional | Structured when enabled | Low | High | Required for regulated, batch, serialized, or advanced warehouse flows. |

## Finance

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target finance scope | General ledger | ChartOfAccount / FinancialAccount / JournalEntry | Finance | Canonical | Structured | Low | High | Extend existing lightweight accounting without breaking subscription and invoice flows. |
| Darwin current model, target finance scope | Settlement and obligations | Receivable / Payable / PaymentTerm | Finance | Canonical | Structured | Medium if member invoices change | High | Needed for independent ERP operation and external accounting coexistence. |
| Target finance scope | Reconciliation | BankTransaction / Reconciliation | Finance | Optional | Structured | Low | High | Important after payment/provider flows stabilize. |

## HR And Time

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Target HR scope | Employee master data | Employee / Department / Position | HR | Canonical | Structured | Possible if linked to `BusinessMember` | Medium | Keep employee records distinct from identity users while allowing links. |
| Target HR scope | Employment records | EmploymentContract / PersonnelFile | HR | Optional | Structured plus attachments | Low | Medium | Sensitive data requires strict permission and retention rules. |
| Target time-tracking scope | Attendance and work time | WorkSchedule / AttendanceEvent / TimeEntry / Timesheet | Time Tracking | Canonical | Structured | Low | Medium | Payroll can remain deferred while time tracking is designed. |

## Documents

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target document scope | Business document attachment | Attachment / DocumentRecord | Documents | Canonical | Structured metadata plus object storage | Medium for invoice/mobile downloads | High | Reuse object-storage boundaries and avoid duplicating media/archive concepts. |
| Target document scope | Document classification | DocumentType / DocumentFolder | Documents | Optional | Structured | Low | Medium | Useful for CRM, HR, finance, and procurement documents. |

## Integrations

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin current model, target integration scope | Integration endpoint | ExternalSystem | Integrations | Canonical | Structured | Possible if mobile-facing records sync externally | High | Track system type, trust boundary, and source-of-truth policy. |
| Target integration scope | Sync execution | ImportBatch / ExportBatch / IntegrationJob | Integrations | Canonical | Structured with payload snapshots where needed | Low | High | Keep raw provider payloads out of logs and secrets out of docs. |
| Target integration scope | Conflict handling | SyncState / SyncConflict | Integrations | Canonical | Structured | Possible if mobile edits conflict | High | Required for coexistence with external ERP/CRM/accounting systems. |

## AI-Readiness

| Source | Candidate concept | Darwin English name | Area | Decision | Storage shape | Mobile/Loyalty impact | Integration impact | Priority | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Darwin target architecture | Recommendation workflow | Recommendation / AutomationSuggestion | AI-readiness | Canonical | Structured | Possible if surfaced in mobile later | Medium | AI should recommend or draft; normal application commands execute approved actions. |
| Darwin target architecture | Approval-controlled action | AiActionDraft / AiActionApproval | AI-readiness | Canonical | Structured | Possible if mobile approval is added | Medium | Required to avoid AI mutating stock, invoices, orders, or customer records without audit. |
| Darwin target architecture | Audit context | BusinessEvent / AuditTrail | AI-readiness | Canonical | Structured | Medium | High | Enables scoped AI assistance, reporting, anomaly detection, and explainability. |

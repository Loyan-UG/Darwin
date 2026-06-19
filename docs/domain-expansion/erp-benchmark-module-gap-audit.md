# ERP Benchmark And Module Gap Audit

## Summary

This document is a documentation-only benchmark and decision-preparation audit. It does not add entities, migrations, routes, DTOs, WebAdmin mutations, mobile contracts, production flows, or integration adapters.

Darwin now has a broad internal ERP baseline, but it must not be called complete against large ERP products until the remaining capability gaps are explicitly accepted, rejected, or designed. The current product is strong in CRM, sales, purchasing, inventory/warehouse, finance/payables/treasury, HR/time/payroll, documents, audit, integration primitives, and AI governance. It is not yet a complete replacement for every enterprise ERP scenario because manufacturing, project/service operations, advanced controlling, fixed assets, enterprise planning, target-specific ERP/accounting adapters, and several industry functions are still not implemented as Darwin-owned modules.

The purpose of this audit is to create the top-level module and feature list first. Detailed entity design should start only after the module decisions below are reviewed.

## Related Darwin Documents

This audit is connected directly to these documents:

- [domain-capability-catalog.md](domain-capability-catalog.md)
- [erp-expansion-master-status.md](erp-expansion-master-status.md)
- [external-system-readiness-primitives.md](external-system-readiness-primitives.md)
- [sync-state-conflict-foundation-design.md](sync-state-conflict-foundation-design.md)
- [finance-export-accounting-api-target-selection-design.md](finance-export-accounting-api-target-selection-design.md)
- [purchasing-supplier-lifecycle-design.md](purchasing-supplier-lifecycle-design.md)
- [inventory-warehouse-task-pwa-design.md](inventory-warehouse-task-pwa-design.md)
- [hr-time-tracking-boundary-design.md](hr-time-tracking-boundary-design.md)
- [payroll-legal-calculation-boundary-design.md](payroll-legal-calculation-boundary-design.md)
- [ai-integration-decision-checkpoint.md](ai-integration-decision-checkpoint.md)
- [../module-audit.md](../module-audit.md)
- [../../BACKLOG.md](../../BACKLOG.md)
- [../../DarwinWebAdmin.md](../../DarwinWebAdmin.md)
- [../../DarwinWebApi.md](../../DarwinWebApi.md)

## Benchmark Scope

The benchmark uses public product documentation from large global ERP suites and German-market business systems as capability references. Darwin must not copy external schemas or product-specific names. Accepted capabilities must become Darwin-owned English concepts that fit the existing architecture.

Reference products and why they matter:

| Reference | Why it matters for this audit | Capability signal |
| --- | --- | --- |
| SAP S/4HANA and SAP Business One | Global and German ERP reference points across finance, supply chain, HR, sales, purchasing, inventory, and analytics. | Broad ERP breadth and German market relevance. |
| Microsoft Dynamics 365 Finance, Supply Chain Management, Business Central, and Project Operations | Common enterprise and SMB ERP reference with finance, supply chain, warehouse, project, and service capabilities. | Strong comparison for modular ERP and customer coexistence. |
| Oracle Fusion Cloud ERP, Oracle SCM, and NetSuite | Global ERP/SCM reference for finance, procurement, project, inventory, manufacturing, and enterprise planning. | Enterprise and mid-market cloud ERP breadth. |
| Odoo | Modular ERP reference for CRM, sales, accounting, inventory, purchase, manufacturing, project, HR, ecommerce, and POS. | Useful benchmark for integrated modular product breadth. |
| DATEV | German accounting, payroll, document, bank, and tax-adviser collaboration reference. | Critical for German finance, payroll, tax, and accountant handoff. |
| Lexware and Sage | German SMB/mid-market commercial process references for purchasing, sales, warehouse, accounting, production, project, service, and HR-adjacent workflows. | Useful for practical German SMB expectations. |

## Current Darwin Module Inventory

| Darwin module group | Current status | Important foundations already present | Strict assessment |
| --- | --- | --- | --- |
| Foundation primitives | Strong baseline | External systems, external references, source-of-truth markers, custom fields, documents, notes, activities, number sequences, business events, audit trails, feature areas. | Adequate for current ERP expansion and integration-ready design. |
| Identity, business, member access | Strong baseline | Users, devices, external login boundary, business members, invitations, staff access, launch guards, mobile contract guards. | Strong for current product. Enterprise identity governance and SSO directory administration remain future scope. |
| CRM and customer bridge | Strong baseline | Customer, lead, opportunity, segments/consent/context decisions, foundation primitive integration. | Adequate for current CRM; advanced marketing automation and support case management are not full ERP-grade yet. |
| Sales and order-to-cash | Strong baseline | Quotes, quote-to-order, orders, invoices, delivery notes, return orders, credit notes, payments, refunds, shipping linkage, source/archive decisions. | Strong for commerce and sales documents; advanced pricing/rebates/contracts need separate decision if required. |
| Purchasing and supplier lifecycle | Strong baseline | Supplier, supplier contacts, purchase orders, goods receipt, supplier documents, supplier invoice/payables, supplier payments, supplier advances. | Strong for purchase-to-pay; strategic sourcing, RFQ/tender, supplier scoring, and contracts are not complete modules. |
| Inventory and warehouse | Strong baseline | Warehouses, locations/bins, labels, inventory transactions, stock levels, stock transfers, goods receipt posting, warehouse tasks, picking, stock counts, lot/serial/handling unit evidence, bin-stock derivation, warehouse PWA. | Strong for warehouse execution; manufacturing material planning, transportation planning, and advanced quality are not complete modules. |
| Finance and accounting | Strong baseline | Financial accounts, journal entries, finance posting service, receivables, payables, account mappings, supplier invoice/payment postings, credit-note posting, reporting, export batch/package/storage/file delivery. | Strong operational accounting foundation. Advanced budgeting, cost accounting depth, fixed assets, consolidation, tax filing, and target API adapters remain gaps. |
| Bank and treasury | Strong baseline | Bank accounts, statement imports, statement lines, reconciliation matches, supplier and payroll bank settlement, returned-transfer correction evidence. | Strong evidence-backed treasury baseline. Bank API integration and automated reconciliation remain future scope. |
| HR, time, payroll | Strong baseline | Employees, departments, positions, contracts, personnel documents, schedules, attendance, time entries, timesheets, leave/absence, payroll periods, rules, runs, payslips, payroll posting/payment/bank correction. | Strong Germany-first baseline. Statutory provider submission and country expansion remain target-specific future work. |
| Documents and compliance evidence | Strong baseline | DocumentRecord, object storage, invoice archive/source model, e-invoice readiness, evidence package, retention/legal-hold readiness docs. | Strong foundation. Production legal acceptance and selected-provider evidence remain go-live blockers. |
| Notifications and communication | Strong baseline | Email/channel audits, push notification inbox, dispatch workers, provider callback boundaries, mobile notification surfaces. | Strong current product surface. Omnichannel campaign automation beyond current scope is future. |
| Integration and sync | Foundation complete | ExternalSystem, ExternalReference, SyncState, SyncConflict, finance file-delivery, provider-neutral boundaries. | Foundation is ready; target-specific inbound/two-way adapters are not implemented. |
| AI governance | Foundation complete | Sensitive-field policy, recommendations, action drafts, approvals, scoped context, internal task/evidence executors. | Correctly conservative. Real model provider and direct operational executors are intentionally blocked. |
| Production readiness | Strong evidence framework | Go-live evidence package, staging plan, readiness exporters/checkers, owner handoff, provider smokes, MinIO/Azure/e-invoice/Android/provider preflights. | Framework is strong; real production evidence and approvals are still required. |

## Benchmark Coverage Matrix

Legend:

- `Covered`: Darwin has a current module foundation and operational/admin workflows.
- `Partial`: Darwin has related foundations but not the complete reference-ERP capability.
- `Gap`: Darwin does not yet have a real module.
- `Conditional`: do only when a real deployment/customer/target proves the need.

| Capability family | Large ERP expectation | Darwin status | Priority recommendation | Decision required |
| --- | --- | --- | --- | --- |
| Core master data | Parties, organizations, customers, vendors, items, locations, employees, chart of accounts, banks. | Covered | Keep hardening through go-live. | Decide later whether a formal `Party` abstraction is worth the migration cost. |
| CRM and sales pipeline | Leads, opportunities, accounts, contacts, quotations, orders, returns, customer documents. | Covered | Current scope is enough for go-live. | Decide advanced marketing automation and support case scope later. |
| Order-to-cash | Sales order, fulfillment, invoicing, payments, refunds, credit notes, receivables. | Covered | Current scope is enough for go-live. | Decide advanced contracts, rebates, and subscription-style enterprise pricing later. |
| Purchase-to-pay | Supplier master, purchase orders, receiving, invoice matching, payables, payments, advances. | Covered | Current scope is enough for go-live. | Decide RFQ/tender/supplier scoring if procurement-heavy customers require it. |
| Inventory and warehouse execution | Stock ledger, bins, transfers, receiving, putaway, picking, counts, lot/serial/handling unit, barcode workflows. | Covered | Current scope is strong; keep operational tests green. | Decide offline/native warehouse device requirements after production evidence. |
| Manufacturing, BOM, MRP, production control | Bill of materials, routings, work centers, production orders, capacity, material planning, WIP, costing. | Gap | P1 for product-producing customers; not required for service or commerce-only go-live. | Choose whether Darwin should own manufacturing or integrate with external manufacturing ERP first. |
| Quality and nonconformance | Inspection plans, quality orders, nonconformance, quarantine, corrective action, supplier quality. | Partial | P1 if regulated goods, food, pharma, serialized goods, or supplier QA matter. | Decide if quality is a separate module or a warehouse/inventory extension. |
| Transportation and logistics planning | Carrier planning, freight rating, route planning, load/shipment consolidation, transport execution. | Partial | P2 after current shipping/provider go-live evidence. | Decide whether DHL/carrier integration is enough or a broader TMS module is needed. |
| Project operations and project accounting | Projects, tasks, resources, time/expense, project costing, billing, WIP, revenue recognition. | Gap | P1 for service/project businesses; P2 otherwise. | Decide if project operations is a core module or vertical extension. |
| Service management and field service | Service contracts, work orders, technicians, spare parts, service billing, maintenance visits. | Gap | P1 for service-heavy customers; P2 otherwise. | Decide service order ownership relative to Sales, Inventory, HR time, and mobile/PWA. |
| Fixed assets and asset maintenance | Fixed asset register, depreciation, leases, asset maintenance, service history. | Gap | P1 for finance completeness; maintenance can be P2. | Decide finance fixed assets before enterprise finance claims. |
| Budgeting, controlling, consolidation, FP&A | Budgets, cost centers, dimensions, cost accounting, consolidation, forecasts, management reporting. | Partial | P1 for enterprise finance; P2 for SMB go-live. | Decide financial dimensions and cost-center policy before broad reporting. |
| Cash management and bank automation | Bank import, reconciliation, payment settlement, bank API, returned payments, cash forecasts. | Partial | Current evidence-backed treasury is enough for internal go-live; API is conditional. | Select target banks only after credential owner and smoke strategy exist. |
| Payroll statutory filing and provider submission | Payroll run, payslip, payment, statutory reports, provider filing, employee self-service. | Partial | Current payroll is strong internally; statutory provider adapter is conditional. | Select real German payroll filing target before adapter design. |
| Document management and legal archive | Attachments, archive, retention, e-invoice artifacts, source models, legal hold. | Covered | Keep production evidence strict. | Decide broader DMS search/classification only after user need is clear. |
| E-commerce, POS, retail | Storefront, checkout, POS, pricing, promotions, returns, customer loyalty. | Partial | Storefront/loyalty are strong; POS is not implemented. | Decide POS only if physical retail customers require it. |
| Customer support/case management | Cases, SLAs, queues, service history, communication timeline. | Partial | P2 unless support-heavy customers need it. | Decide if support is CRM extension or standalone module. |
| Analytics and BI | Dashboards, KPIs, reporting, operational analytics, finance reports. | Partial | P1 after go-live for management visibility. | Decide semantic metric catalog and dashboard governance. |
| Integration and coexistence | External systems, references, sync states, conflicts, import/export, target adapters. | Partial | P1 for customers running Darwin beside SAP, Microsoft, DATEV, or accounting tools. | Choose first real target adapters; do not build fake adapters. |
| AI and automation | Recommendations, action drafts, approval workflows, scoped data, provider adapters. | Partial | Lower priority by current decision. | Select real provider and command families before more automation. |
| Governance, security, compliance | Audit, permissions, evidence, approval workflows, safe metadata, no-secret policy. | Covered for current phase | Keep strict. | Add separation-of-duties matrix if enterprise finance customers require it. |

## German-Market Comparison Matrix

| German-market expectation | Reference pressure | Darwin status | Practical impact for customers | Recommended priority |
| --- | --- | --- | --- | --- |
| DATEV-style accounting and tax-adviser handoff | German companies often need accountant collaboration, document exchange, bank/accounting handoff, payroll outputs, and finance evidence. | Partial through finance export file-delivery, DocumentRecord, e-invoice, payroll artifacts, and external references. | Darwin can prepare evidence and export packages, but not a native DATEV API workflow yet. | P1 after go-live evidence if German accountant handoff is a sales requirement. |
| ZUGFeRD/Factur-X and XRechnung readiness | German e-invoice rollout requires legally accepted artifacts and validation evidence. | Partial: tooling and readiness gates exist; production evidence is blocked until sign-off. | Cannot claim compliant production rollout until validation reports, storage/download smoke, and sign-off exist. | P0 for German production rollout. |
| Warenwirtschaft breadth | German SMB systems emphasize purchasing, sales, stock, warehouse, documents, and accounting linkage. | Covered for core Warenwirtschaft. | Strong fit for commerce/trade customers. | Keep as current core. |
| Production/manufacturing | German mid-market ERP often includes production and light manufacturing. | Gap. | Product-producing customers may need external ERP integration or a Darwin manufacturing module. | P1 if target customers manufacture. |
| Project/service workflows | Common in German SMB/mid-market software. | Gap. | Service companies may find CRM/sales/warehouse insufficient without service orders and project accounting. | P1/P2 based on target customer profile. |
| Payroll and personnel documents | German payroll needs legal rule versioning, payslips, payment evidence, and provider filing. | Partial to strong internally; statutory provider submission not implemented. | Internal payroll evidence is strong, but external statutory submission needs a target provider. | Conditional P1 after target provider selection. |
| Bank/accounting integration | German finance operations often rely on bank reconciliation and accounting exports. | Partial: treasury, reconciliation, file export are present; bank/accounting APIs are blocked. | Good internal evidence, but limited automation with banks/accounting tools. | P1 only after selecting target banks/accounting products. |

## Missing Or Incomplete Module Decisions

These are the main decision items before detailed design begins. The strict recommendation is to decide at module level first, then write boundary design documents for accepted modules.

| Decision item | Options | Business impact | Technical impact | Recommendation |
| --- | --- | --- | --- | --- |
| Manufacturing and MRP | Build Darwin manufacturing; integrate external manufacturing ERP; defer until customer demand. | Determines whether Darwin can sell to producers or only trade/service customers. | Requires BOM, routing, production orders, capacity, WIP, costing, inventory reservations, and finance posting boundaries. | Decide explicitly. If product-producing customers are in scope, make this the next major design track after go-live evidence. |
| Project operations | Build core project module; keep projects as CRM/custom fields; integrate external project tools. | Required for consulting, installation, construction, agency, and project-based billing. | Requires project master, tasks, resource/time linkage, expense, billing, project accounting, and reporting. | Treat as P1 if service businesses are target customers. |
| Service management | Build service orders/field service; keep support as CRM notes; integrate external helpdesk/field-service tools. | Required for after-sales service, maintenance visits, repair, and spare-parts workflows. | Requires work orders, scheduling, technicians, parts consumption, service billing, and mobile/PWA decisions. | Design separately from generic CRM to avoid weak support workflows. |
| Quality management | Build inspection/nonconformance module; keep inspection in warehouse/goods receipt only. | Required for regulated or high-value inventory businesses. | Requires quality plans, inspection results, holds, nonconformance, supplier quality, and inventory disposition. | Build if target customers handle regulated, expiring, serialized, or supplier-sensitive goods. |
| Advanced finance | Build budgeting/cost centers/fixed assets/consolidation; integrate accounting suite; defer. | Determines whether Darwin is a full finance system or operational ERP with accounting export. | Requires financial dimensions, cost accounting, fixed asset lifecycle, depreciation, consolidation, and reporting. | P1 for enterprise finance claims; otherwise keep file-delivery/accounting integration first. |
| DATEV/German accounting adapters | Build DATEV-first adapter; choose another accounting API target; keep file-delivery only. | Directly affects German accountant workflow and customer adoption. | Requires target credential policy, package mapping, external references, sync/conflict, safe errors, and smoke strategy. | Start with target selection design, not implementation. |
| Bank API adapters | Build German bank adapters; keep manual statement import; use third-party bank aggregation. | Reduces manual bank import and improves settlement automation. | Requires credentials, PSD2/open-banking policy, error handling, statement identity, reconciliation safety, and no-secret logging. | Do not implement until target banks/provider are selected. |
| POS and physical retail | Build POS; integrate external POS; defer. | Required for stores, counter sales, cash drawer, receipts, and offline checkout. | Requires device UX, payments, tax receipts, stock deduction, offline handling, and closing reports. | P2 unless physical retail is a near-term customer segment. |
| Master data import and coexistence | Build generic import/sync framework; build target-specific imports only; manual import. | Critical for migration and coexistence with SAP/Microsoft/accounting systems. | SyncState/SyncConflict exists, but import batches, mapping UI, validation reports, and conflict resolution workflow need design. | P1 for enterprise migrations and coexistence. |
| Separation of duties and approval governance | Build SoD matrix and approval policies; keep current permission model; integrate identity governance. | Needed for finance-heavy enterprise customers and audit. | Requires role conflict model, approval delegation, audit reports, and enforcement in WebAdmin actions. | P1 before enterprise finance go-live. |

## Strict Findings

- Darwin is no longer a small panel-based system. It has a broad ERP baseline and many current modules are mature enough to be treated as current-phase complete.
- Darwin is not yet a full benchmark-equivalent to SAP, Microsoft Dynamics, Oracle, or mature German mid-market ERP for every customer type.
- The largest functional gap is manufacturing/MRP. If Darwin must support product-producing customers directly, this cannot stay implicit.
- The second major gap is project/service operations. If customers sell services, installations, repairs, or project work, CRM plus Sales is not enough.
- Advanced finance is still not equivalent to enterprise finance suites. The current accounting foundation is strong, but budgeting, cost accounting depth, fixed assets, consolidation, and statutory integrations need decisions.
- Coexistence with SAP, Microsoft, DATEV, and other systems is architecturally prepared but not complete. `ExternalSystem`, `ExternalReference`, `SyncState`, and `SyncConflict` are necessary foundations, not target adapters.
- Vendor-neutral documentation was correct for architecture discipline, but a separate benchmark audit is now necessary so Darwin's roadmap is not blind to industry-standard ERP capabilities.

## Recommended Next Documentation Steps

1. Review and decide the module-level items in `Missing Or Incomplete Module Decisions`.
2. For every accepted P1 module, create a boundary design document before implementation.
3. Update `domain-capability-catalog.md` only after module decisions are accepted.
4. Keep external ERP product names out of entity names, code, and core model language. Use them only in benchmark and adapter selection documents.
5. Keep production go-live evidence execution as the immediate release gate. New module design must not weaken go-live evidence requirements.

## References

- SAP ERP overview: https://www.sap.com/products/erp.html
- SAP S/4HANA Cloud Public Edition: https://www.sap.com/products/erp/s4hana.html
- SAP Business One: https://www.sap.com/products/erp/business-one.html
- Microsoft Dynamics 365 Business Central documentation: https://learn.microsoft.com/en-us/dynamics365/business-central/
- Microsoft Dynamics 365 Finance documentation: https://learn.microsoft.com/en-us/dynamics365/finance/
- Microsoft Dynamics 365 Supply Chain Management documentation: https://learn.microsoft.com/en-us/dynamics365/supply-chain/
- Microsoft Dynamics 365 Project Operations documentation: https://learn.microsoft.com/en-us/dynamics365/project-operations/
- Oracle ERP: https://www.oracle.com/erp/
- Oracle SCM: https://www.oracle.com/scm/
- Oracle NetSuite ERP: https://www.netsuite.com/portal/products/erp.shtml
- Odoo apps: https://www.odoo.com/page/all-apps
- DATEV Unternehmen online: https://www.datev.de/web/de/unternehmen/loesungen/rechnungswesen/buchfuehrung/buchfuehrung-vom-steuerberater/digital-stark-zusammenarbeiten
- DATEV professional accounting: https://www.datev.de/web/de/unternehmen/loesungen/rechnungswesen
- Lexware Warenwirtschaft overview: https://www.lexware.de/wissen/faktura-warenwirtschaft/warenwirtschaft/
- Lexware product overview: https://shop.lexware.de/produktuebersicht

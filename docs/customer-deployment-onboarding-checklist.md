# Customer Deployment Onboarding Checklist

Reviewed: 2026-05-27

This checklist defines the repeatable steps for preparing a Darwin deployment for a customer. It is deployment-neutral and must not contain customer names, domains, credentials, API keys, webhook secrets, signing keys, or private infrastructure details.

Use this document together with:

- [docs/production-setup.md](production-setup.md)
- [docs/external-smoke-inputs.md](external-smoke-inputs.md)
- [docs/go-live-status.md](go-live-status.md)
- [docs/module-audit.md](module-audit.md)
- [docs/e-invoice-acceptance-checklist.md](e-invoice-acceptance-checklist.md)

## Approval Roles

| Role | Typical owner | Approves |
| --- | --- | --- |
| Darwin technical owner | Darwin implementation/support team | Build, migration, provider smoke, technical runbooks. |
| Customer system admin | Customer IT/admin team | Domains, DNS, TLS, users, roles, device access, operational settings. |
| Customer business owner | Customer management | Go-live scope, user onboarding, support process, payment/subscription policy. |
| Customer accounting/tax owner | Accounting lead or tax advisor | VAT/VIES policy, invoice content, e-invoice fixtures, retention expectations. |
| Customer operations owner | Operations/support lead | Order, inventory, shipping, returns, communication, and alert handling. |
| Legal/compliance reviewer | Customer legal/compliance or external advisor | Contractual, privacy, retention, and legal compliance sign-off where required. |

## Phase 1: Scope And Decisions

- Confirm deployment scope: WebAdmin, WebApi, Worker, Web, Consumer mobile, Business mobile.
- Confirm persistence provider: PostgreSQL preferred/default; SQL Server supported when required.
- Confirm object-storage provider: MinIO recommended for self-hosted deployments; AWS S3 or Azure Blob are alternatives.
- Confirm payment model:
  - Customer checkout stays web/hosted payment handoff unless explicitly changed.
  - Business subscription stays status plus web/back-office management handoff unless explicitly changed.
- Confirm shipping provider scope and whether DHL live validation is in go-live scope.
- Confirm communication provider scope and sender roles.
- Confirm VAT/VIES policy: `Manual Review + Scheduled Retry` is the default for provider failures.
- Confirm e-invoice scope: receive-only readiness, ZUGFeRD/Factur-X generation, XRechnung export, or deferred.
- Confirm mobile store scope: Android only, Android+iOS, or broader.

Manual approvals:

- Customer business owner signs off go-live scope.
- Customer accounting/tax owner signs off VAT and e-invoice scope.
- Darwin technical owner records unresolved provider blockers.

## Phase 2: Infrastructure And Secrets

- Provision database and roles.
- Configure backups and restore test plan.
- Configure public HTTPS endpoints and TLS.
- Configure secure secret storage.
- Configure appsettings only for non-secret defaults.
- Configure User Secrets only for local development.
- Configure deployment vault or equivalent for production secrets.
- Configure object storage endpoint, bucket/container, profiles, retention, and monitoring.
- Configure Worker deployment and background job ownership.

Manual approvals:

- Customer system admin confirms DNS/TLS.
- Customer system admin or DevOps owner confirms backup/restore ownership.
- Darwin technical owner confirms no secrets are committed.

## Phase 3: Provider Setup

### Stripe

- Configure publishable key, server-side key, webhook signing secret, and enabled flag through Site Settings or secure deployment configuration.
- Configure webhook endpoint and subscribed events.
- Run test-mode smoke before live mode.
- Do not run live-mode smoke without explicit approval.

Approval:

- Customer business owner approves live-mode execution.
- Darwin technical owner confirms webhook-authoritative finalization.

### Brevo

- Verify sender domain and sender addresses.
- Configure API key through Site Settings or secure deployment configuration.
- Configure outbound webhook to the public notification endpoint.
- Configure Basic Auth or token authentication for webhook security.
- Subscribe transactional events required by the runbook.
- Run sandbox smoke, controlled inbox smoke, and callback visibility checks.

Approval:

- Customer system admin confirms DNS/DKIM/DMARC.
- Operations owner confirms failed-send queue ownership.

### DHL

- Configure account credentials, product code, billing/account number, shipper data, receiver smoke data, callback settings, and label storage.
- Run preflight first.
- Run live validation only when complete account data is available.
- Do not generate fake labels, references, or tracking values.

Approval:

- Operations owner confirms shipping/returns flow.
- Darwin technical owner confirms label storage and provider-operation recovery.

### VIES

- Configure controlled valid and invalid VAT IDs for smoke.
- Keep provider failures as `Unknown` and manual review.
- Enable scheduled retry only when queue ownership is assigned.
- Show format hints only as operator guidance, never official validation.

Approval:

- Accounting/tax owner confirms manual-review process.
- Operations owner confirms queue monitoring.

### Object Storage

- Run `scripts\check-minio-production-readiness.ps1` or equivalent selected-provider readiness check.
- Confirm `InvoiceArchive`, `ShipmentLabels`, `MediaAssets`, `FinanceExports`, and `FinanceExportOutbound` profile decisions.
- Confirm `FinanceExports` is the generated finance export package source and `FinanceExportOutbound` is the outbound delivery destination when accounting export is in scope.
- Confirm finance export profile configuration comes from secure deployment configuration, not from batch metadata, document metadata, external-reference metadata, or package content.
- Confirm disposable smoke prefix and retention/delete behavior.
- Run selected-provider smoke only after explicit production-like smoke confirmation.
- Run WebAdmin smoke against a disposable object.

Approval:

- Customer system admin or DevOps owner confirms retention, backup, restore, monitoring, and alerting.
- Accounting/tax owner confirms archive retention expectations.

## Phase 4: Business Setup

- Create tenant/business profile.
- Set legal name, contact/provisioning email, support email, sender identity, default culture, time zone, and primary location.
- Assign owner/admin/member roles.
- Send and verify invitations.
- Approve, suspend, reactivate, and support lifecycle paths as needed.
- Configure loyalty program, rewards, campaigns, QR/session policy if in scope.
- Configure billing/subscription plan visibility and web/back-office handoff.

Manual approvals:

- Customer business owner approves profile and plan setup.
- Operations owner approves loyalty/QR/session workflow.

## Phase 5: WebAdmin And Support Readiness

- Verify business onboarding wizard/support queue.
- Verify user activation/reset/lock/unlock support.
- Verify billing/subscription/payment/refund/dispute visibility.
- Verify communication audits, provider callbacks, and retry surfaces.
- Verify mobile operations workspace.
- Verify inventory, receiving, stock transfer, returns, and refund coordination if in scope.
- Verify operational alerts:
  - Critical system-failure alerts go to admin email.
  - Non-critical warnings remain visible in WebAdmin queues/workspaces.
  - Future monitoring integration can consume logs, provider callback queues, and audit states.

Manual approvals:

- Operations owner confirms daily support workflow.
- Customer system admin confirms admin roles and access.

## Phase 6: Mobile Readiness

- Configure Google Maps keys with package/bundle and signing restrictions.
- Configure Firebase/APNS production push settings.
- Validate Android/iOS/MacCatalyst signing profiles as required.
- Run Consumer smoke: login, discovery, map, QR generation, profile/account flows.
- Run Business smoke: login, access-state gate, home, scan tab, QR session handling.
- Validate real camera QR scan with two devices or approved camera feed.
- Confirm mobile apps do not use broad cleartext HTTP or unsafe Release certificate trust.

Manual approvals:

- Customer system admin confirms mobile configuration.
- Operations owner confirms QR/session workflow.
- Darwin technical owner confirms signed artifact validation.

## Phase 7: Compliance And E-Invoice

- Follow [docs/e-invoice-acceptance-checklist.md](e-invoice-acceptance-checklist.md).
- Confirm receive-only readiness where generation is deferred.
- Approve deterministic fixture scenarios.
- Generate and validate artifacts.
- Store and download artifacts through the archive storage profile.
- Record accounting/tax/legal sign-off outside source control.

Manual approvals:

- Accounting/tax owner signs off invoice fixture correctness.
- Legal/compliance reviewer signs off where required.
- Darwin technical owner confirms artifact storage and validation evidence.

## Phase 8: Final Verification

- Run focused build/test lanes for touched components.
- Run `scripts\check-secrets.ps1`.
- Run `git diff --check`.
- Run `scripts\check-go-live-readiness.ps1`.
- Run provider smoke scripts only for providers in scope and only with approved credentials/configuration.
- Record all blocked items and owner assignments.
- Record rollback plan and support contact path.

Go-live is not complete until:

- Critical provider smokes are passed or explicitly deferred.
- Production secrets are in secure storage.
- Backups and restore tests are complete.
- Monitoring/alerting ownership is assigned.
- Customer approvals are recorded outside source control.

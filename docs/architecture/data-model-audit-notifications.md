# Data Model Audit: Notifications Foundation

Date: 2026-06-15

## Scope

This audit covered the data model changes needed for the internal notification inbox, plus the surrounding persistence patterns that affect security, storage, and performance.

## Current Model Findings

- Primary keys are consistently `Guid` in domain entities and are mapped to PostgreSQL `uuid`. This is compatible with the existing architecture and mobile/API contracts. For very high-write tables, especially delivery/recipient tables, index bloat should be monitored before considering a separate numeric surrogate key.
- `BaseEntity` provides audit fields, soft delete, and row version data. PostgreSQL row versions are managed by the application in `DarwinDbContext.Auditing`, while SQL Server uses native row version semantics where configured. This is acceptable, but comments and migration review should keep the provider difference explicit.
- `citext` is used for case-insensitive identifiers such as email-like lookup fields. This is the right PostgreSQL feature for uniqueness and lookup stability.
- `jsonb` is used for evolvable payloads such as campaign targeting and plan features. This is appropriate when the app treats JSON as opaque configuration. If a JSON property becomes part of frequent filtering/reporting, it should be promoted to a typed column or backed by a targeted GIN/expression index.
- Existing soft-delete filters depend on explicit query predicates in handlers. The new inbox queries follow this convention. A global query filter would reduce accidental exposure but is a broader architecture change.
- Push device tokens are stored as application data. They are not user passwords, but they are sensitive delivery identifiers. Long term, token storage should use provider-level encryption or application encryption-at-rest, and tokens should never be returned from public API responses.
- Deep links in notifications are stored as bounded strings. They should remain app-internal route/deep-link values, not arbitrary executable commands or unvalidated external URLs.

## Notification Inbox Design Decisions

- `NotificationMessage` stores the shared message content, category, target app, source, optional deep link, publish time, and expiry.
- `NotificationRecipient` stores per-user delivery/read/archive state. This avoids duplicating message text for every recipient and keeps unread count queries focused.
- Both new entities use `Guid` keys to match the rest of the solution.
- The unique index on `(NotificationMessageId, UserId)` prevents duplicate recipients for the same message and user during retries.
- Recipient list and unread-count paths are supported by composite indexes on `UserId`, `ReadAtUtc`, `ArchivedAtUtc`, and creation time.
- Message filtering is supported by indexes on `TargetApp`, `Category`, and `PublishedAtUtc`.

## Security Notes

- API access control filters notifications by the authenticated user id before returning list/read/count results.
- Target app is accepted as a query filter so the same endpoint can serve both mobile apps. A stricter future version should derive allowed target app(s) from authenticated role/claims and reject mismatched client filters.
- Notification body/title must be treated as user-visible content. Campaign-derived content should continue to pass through existing campaign validation and moderation controls.
- Source ids and source types are returned for client context. They should not be used to authorize access to the source object; any follow-up deep link target must run its own authorization.

## PostgreSQL-Specific Notes

- `uuid`, `citext`, `jsonb`, and `timestamp with time zone` are already aligned with PostgreSQL best practices for this project.
- The new inbox model does not require a GIN index because v1 list/count queries do not filter JSON payloads.
- Partial indexes are useful for active/unread rows. EF provider differences make fully portable filtered indexes harder; the current composite indexes are the safer cross-provider baseline.
- If unread counts become hot, add a PostgreSQL partial index for active unread recipients, for example on `(UserId, CreatedAtUtc DESC)` where `ReadAtUtc is null and ArchivedAtUtc is null and IsDeleted = false`.

## Follow-Up Recommendations

- Add role-derived target-app enforcement in the notification handlers instead of trusting the query parameter alone.
- Encrypt or otherwise protect push tokens at rest before production push dispatch scales up.
- Add retention/cleanup jobs for expired notification messages and archived/read recipient rows.
- Add integration tests for inbox access control, campaign activation dedupe, unread count, and read-all.
- Promote any frequently queried JSON field to a typed column rather than relying on broad JSON scans.

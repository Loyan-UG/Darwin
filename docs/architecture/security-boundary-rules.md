# Security Boundary Rules

Reviewed: 2026-06-19

## Summary

This document records security boundary rules for tenant, package, domain, and deployment architecture. It is documentation-only and changes no authorization policy, middleware, route, token, cookie, provider, or storage behavior.

Tenant enablement, capability enablement, and authorization are separate concerns. A tenant may be entitled to a capability, but each user and surface must still pass normal authentication and authorization checks.

## Boundary Rules

| Boundary | Rule |
| --- | --- |
| Tenant resolution | Resolve tenant before loading tenant-sensitive data. If tenant cannot be resolved safely, fail closed. |
| Business selection | A selected business must belong to the resolved tenant. |
| Capability enablement | Capability gates control availability; they do not grant permissions. |
| Authorization | Roles, permissions, policies, token claims, and audience rules remain authoritative for user access. |
| Provider readiness | Provider add-ons require secure configuration and readiness checks before success paths are enabled. |
| Secrets | Store secret references, not raw secrets, in tenant, package, domain, external reference, audit, or metadata records. |
| Domain routing | Only verified active domains can route tenant traffic. |
| Database routing | Tenant data-store resolution must not trust user-supplied host or route values without catalog verification. |
| On-premise | Local tenant configuration must not require hosted secrets or hosted key-ring access. |

## Fail-Closed Responses

| Condition | Required response policy |
| --- | --- |
| Unmapped host | No tenant data is loaded; return safe not-found or structured tenant resolution failure. |
| Inactive tenant | Block tenant operations and use `TenantDisabled` or maintenance response depending on status. |
| Unverified domain | Do not route traffic to tenant surfaces. |
| Business outside tenant | Return authorization failure or not-found according to the audience and enumeration risk. |
| Disabled capability | Return `FeatureDisabled` for API and authenticated direct admin access; hide navigation where possible. |
| Provider not ready | Disable provider action and never record fake success. |

## Token And Cookie Rules

- Existing JWTs may carry `business_id`; future tenant-aware tokens can add tenant context only after compatibility planning.
- Admin cookies and storefront/member cookies must remain separated by purpose and host.
- Customer custom admin domains should use exact-host cookie scope by default.
- Shared Darwin-hosted admin subdomains may use parent-domain cookie scope only if every child host is trusted and the risk is explicitly accepted.
- Data Protection keys must be durable and deployment-appropriate; on-premise customers own their local key-ring and certificate policy.

## Metadata Safety

The following values must not be stored in tenant/domain/package/feature/audit metadata:

- Passwords, API keys, access tokens, refresh tokens, private keys, and connection strings.
- Raw provider callback payloads beyond the dedicated bounded inbox/storage policy.
- Full bank, card, or private account identifiers.
- Private storage paths intended only for infrastructure.

## Source-Of-Truth Rules

- `PlatformTenant` owns tenant lifecycle and deployment isolation.
- `Business` owns merchant operations inside a tenant.
- `FeatureArea` owns technical capability code.
- `TenantPackageAssignment` owns commercial entitlement.
- `BusinessFeatureOverride` owns scoped business exception only under tenant entitlement.
- Authorization owns user permission.
- Provider readiness owns whether configured provider operations can execute.

## Approval And SoD Boundary

High-risk actions need explicit approval governance before enterprise-grade enforcement is claimed. The detailed design lives in [security-sod-approval-governance-design.md](security-sod-approval-governance-design.md). Approval is separate from entitlement and permission:

- entitlement decides whether the tenant can use a capability;
- permission decides whether a user can request or perform an action;
- approval decides whether a sensitive requested action is allowed to execute;
- SoD decides whether the requester and approver can be the same actor.

No implementation may use package enablement, role membership, or AI approval evidence as a substitute for the others.

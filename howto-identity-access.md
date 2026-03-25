# Identity Access How-To

> Scope: admin identity, users, roles, permissions, addresses, and the current HTMX-backed interaction model inside `Darwin.WebAdmin`.

## Purpose

The identity module is responsible for:

- user administration
- role administration
- permission administration
- user-to-role assignments
- role-to-permission assignments
- admin-managed user addresses

It is implemented in `Darwin.WebAdmin` over application handlers in `Darwin.Application`.

## Core Rules

- the web layer must never manipulate domain entities directly
- all mutations must go through application handlers and DTOs
- optimistic concurrency is enforced with `RowVersion`
- sensitive actions must be protected by permission checks
- admin identity DTOs must not be reused as public/member API contracts

## Conceptual Schema

### User

Identity record with audit fields and `RowVersion`.

Typical fields:

- `Email`
- `FirstName`
- `LastName`
- `IsActive`
- `Locale`
- `Currency`
- `Timezone`
- `PhoneE164`

### Role

Named permission bundle with audit fields and `RowVersion`.

### Permission

Atomic capability such as:

- `FullAdminAccess`
- `ManageUsers`
- `ManageRoles`
- `ManagePermissions`
- `AccessMemberArea`
- `AccessLoyaltyBusiness`

### Join Tables

- `UserRoles`
- `RolePermissions`

## Admin UI Model

### Users

Current back-office user flows:

- paged list
- create
- edit
- change email
- change password
- assign roles
- soft delete
- manage addresses

### Roles and Permissions

Current back-office role flows:

- paged list
- edit
- assign permissions
- delete where allowed

## HTMX Usage in Identity Screens

The Users screen now uses HTMX as the preferred interaction model for address management.

### Address create/edit

- the modal form posts with `hx-post`
- the target is `#addresses-section`
- the returned server partial replaces only the address section
- alerts are refreshed through a server-rendered alerts partial

Example pattern:

```html
<form hx-post="/Users/CreateAddress"
      hx-target="#addresses-section"
      hx-swap="innerHTML">
    ...
</form>
```

### Default billing/shipping

The “Set Billing” and “Set Shipping” buttons use `hx-post` and replace the address section without reloading the page.

### Delete

Delete still uses the shared confirmation modal and the existing AJAX flow. It can be moved to full HTMX in a later cleanup pass.

## Application Handlers Used by WebAdmin

### Users

- `GetUsersPageHandler`
- `RegisterUserHandler`
- `GetUserWithAddressesForEditHandler`
- `UpdateUserHandler`
- `ChangeUserEmailHandler`
- `SetUserPasswordByAdminHandler`
- `SoftDeleteUserHandler`

### Addresses

- `CreateUserAddressHandler`
- `UpdateUserAddressHandler`
- `SoftDeleteUserAddressHandler`
- `SetDefaultUserAddressHandler`

### Roles and Permissions

- `GetRolesPageHandler`
- `GetRoleWithPermissionsForEditHandler`
- `UpdateRolePermissionsHandler`
- `GetUserWithRolesForEditHandler`
- `UpdateUserRolesHandler`

## Authorization Guidance

### Controller protection

Admin controllers should enforce permission-based access, not just authenticated access.

### Razor protection

Use `PermissionRazorHelper` to hide links and actions that should not render for the current operator.

Example:

```cshtml
@if (await Perms.HasAsync("FullAdminAccess"))
{
    <a asp-controller="Users" asp-action="Index">Users</a>
}
```

## Concurrency Guidance

Any edit or delete path that depends on the current stored state must include `RowVersion`.

Behavior:

- stale `RowVersion` should result in a deterministic failure
- the UI should show a clear conflict message
- the operator should be able to reload and retry

## Relationship to WebApi

Identity administration belongs to `Darwin.WebAdmin`, but the user-facing identity and profile experience must be exposed through `Darwin.WebApi`.

Rule:

- admin screens use WebAdmin + Application handlers
- front-office and mobile use `Darwin.WebApi` contracts
- do not share admin operational DTOs with public/member consumers

## Developer Notes

- keep nullable reference types enabled
- initialize non-nullable strings
- keep code comments and XML docs in English
- avoid embedding front-office assumptions into back-office identity models

# identity-access-howto.md

> **Scope:** Admin Identity module (Users, Roles, Permissions) — how data is modeled, how the Admin UI flows work, which Application-layer APIs (Handlers + DTOs) the Web layer calls, what security policies we enforce, and how we handle optimistic concurrency + common error patterns.

## 1) Data Schema (conceptual)

**Entities**
- **User**: identity record (email, name fields, flags like `IsActive`), audit + `RowVersion`.
- **Role**: named bundle of permissions; not soft-deleted; audit + `RowVersion`.
- **Permission**: atomic capability with `Key`, `DisplayName`, `Description`, flags like `IsSystem`; audit + `RowVersion`.

**Join tables (many-to-many)**
- **UserRoles**: (UserId, RoleId, RowVersion).
- **RolePermissions**: (RoleId, PermissionId, RowVersion).

> The Web layer never manipulates Domain entities directly. It only speaks to the Application layer through commands/queries (Handlers) and DTOs. Optimistic concurrency is enforced by `RowVersion` where applicable.

## 2) Admin UI Flows

### 2.1 Users

**Index**
- Grid with paging/filtering (query by email/name).
- Row actions: **Edit**, **Change Password**, **Roles**, **Delete** (soft delete via modal).
- Use `TempData["Success"|"Error"|"Warning"|"Info"]` to show alerts via the shared `_Alerts.cshtml`.

**Edit**
- Profile fields only (email and roles are edited elsewhere).
- User addresses section embedded (`_AddressesSection.cshtml`), with Add/Edit/Delete in modal, and “Set as Default Billing/Shipping”.
- Concurrency conflicts surface as user-friendly messages; user can reload and retry.

**Change Password (by Admin)**
- Admin sets a new password without knowing the current one. Uses the Application handler that rotates the security stamp and invalidates prior sessions.

**Assign Roles to a User**
- Page `/Admin/Identity/Users/Roles/{userId}` shows:
  - Header with user identity (e.g., email).
  - Checklist of all roles, with current selections.
  - Save posts back the selection.
  - On success, redirect to Users/Index with success alert.

### 2.2 Roles

**Index**
- Grid with paging/filtering.
- Row actions: **Edit**, **Permissions**, **Delete** (if allowed), etc.

**Assign Permissions to a Role**
- Page `/Admin/Identity/Roles/Permissions/{roleId}` shows:
  - Header with role display name.
  - Checklist of all permissions (Key, DisplayName, Description).
  - Save posts back the selected permissions.
  - On success, redirect to Roles/Index with success alert.

> **Deep-linking & Active Nav**
> - All screens accept query-string parameters for state (page, pageSize, query). Pages should preserve and round-trip these parameters in links and forms so that when the admin navigates back, they land where they were (deep-linkable list state).
> - The left navigation highlights the active route using `ActiveNavLinkTagHelper` (area/controller/action) to keep user orientation consistent.

## 3) Application API (Handlers & DTOs)

> The Web layer must call only Handlers with concrete DTOs; never hydrate entities itself. For new features, add new Handlers/DTOs in Application and wire them through DI in Web.

### Users
- **Get paged users**: `GetUsersPageHandler.HandleAsync(page, pageSize, query, ct)` → `(Items, Total)`.
- **Get user + addresses for edit**: `GetUserWithAddressesForEditHandler.HandleAsync(userId, ct)` → combined edit DTO (user profile + addresses).
- **Update user**: `UpdateUserHandler.HandleAsync(UserEditDto, ct)`.
- **Soft-delete user**: `SoftDeleteUserHandler.HandleAsync(UserDeleteDto, ct)`.
- **Change password by Admin**: `SetUserPasswordByAdminHandler.HandleAsync(UserAdminSetPasswordDto, ct)` (Id, NewPassword).

### Roles & Permissions
- **Get role + permissions for edit**: `GetRoleWithPermissionsForEditHandler.HandleAsync(roleId, ct)` → `{ RoleId, RowVersion, PermissionIds, AllPermissions[...] }`.
- **Update role-permissions**: `UpdateRolePermissionsHandler.HandleAsync(RolePermissionsUpdateDto, ct)`.
- **Get user + roles for edit**: `GetUserWithRolesForEditHandler.HandleAsync(userId, ct)` → `{ UserId, RowVersion, RoleIds, AllRoles[...] }`.
- **Update user-roles**: `UpdateUserRolesHandler.HandleAsync(UserRolesUpdateDto, ct)`.

> **Naming & results**
> - Handlers return `Result` or raw values depending on operation type. Web must check `result.Succeeded` and read `result.Value` (not `Data`), and use `result.Error` for alerts when `Succeeded == false`.

## 4) Security Policies

- **Authorization in Controllers**
  - Gate admin-only actions with policy/permission checks (e.g., `ManageUsers`, `ManageRoles`, `ManagePermissions`).
  - For Razor, use `PermissionRazorHelper` to conditionally render sensitive links and action buttons.

- **Razor Helper**
  - Example usage:
    ```cshtml
    @if (await Perms.HasAsync("FullAdminAccess")) {
        <!-- secure link(s) -->
    }
    ```
  - Keep menu items and settings links inside permission checks to prevent accidental exposure.

- **Password Change by Admin**
  - No need for current password.
  - Security stamp rotates; previous sessions are invalidated.

## 5) Concurrency Pattern & Common Errors

- **RowVersion** (byte[]) is included in edit/delete DTOs:
  - On edit: controller posts back hidden `RowVersion`. If mismatch occurs, Application throws/returns a concurrency error; Web displays `TempData["Error"] = "Concurrency conflict..."` and suggests reload.
  - On delete from list: modal posts `Id` + `RowVersion`. If mismatch, show concurrency error and keep user on the list.

- **Result pattern**
  - Always check `result.Succeeded`. For failure, surface `result.Error` in `_Alerts.cshtml`.
  - Avoid assuming message strings; the Application already produces short, user-facing messages.

## 6) UI Conventions

- **Alerts**: shared partial `_Alerts.cshtml` reads:
  - `TempData["Success"]`, `TempData["Error"]`, `TempData["Warning"]`, `TempData["Info"]`.
- **Delete buttons**: Always use the shared confirmation modal `_ConfirmDeleteModal.cshtml`, with data attributes:
  - `data-action`, `data-id`, `data-name`, `data-rowversion` (Base64).
- **Paging**: Use the `PagerTagHelper` with `Page`, `PageSize`, `Total`, `Query`, `PageSizeItems` on VMs.
- **Deep-linking**: always pass through `page`, `pageSize`, `query` in action links to preserve state.
- **Active navigation**: keep menu highlighting via `ActiveNavLinkTagHelper`.

---

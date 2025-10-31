identity-access-howto.md

Audience: Darwin developers (Web + Application)
Scope: Identity & Access in Darwin — data model, UI flows, Application APIs (Handlers/DTOs), security policies, concurrency & common errors.
Stack: ASP.NET Core (.NET 9, C# 13), EF Core, FluentValidation

1) Data Model (Conceptual)

User: local identity (email/first/last/locale/currency/timezone/phone, IsActive, RowVersion).
Users relate to Roles (many-to-many) and own addresses, webauthn creds, etc.

Role: named collection of permissions (Key, DisplayName, Description, IsSystem, RowVersion).

Permission: atomic capability (Key, DisplayName, Description, IsSystem, RowVersion).

RolePermission: join(User-independent) mapping Role ↔ Permission.

UserRole: join mapping User ↔ Role.

Seed/permission keys are enforced consistently via PermissionKeys in Web and IPermissionService in Infrastructure/Application.

2) Application Layer — Public API (Handlers & DTOs)

These are the only surfaces Web should call.

Roles ↔ Permissions

GetRoleWithPermissionsForEditHandler.HandleAsync(Guid roleId, ct)
Returns Role info + all permissions + selected permission ids for the role. (Web maps to RolePermissionsEditVm.)

UpdateRolePermissionsHandler.HandleAsync(RolePermissionsUpdateDto, ct)
Persists the edited selection; expects RowVersion for concurrency.
(Located in Application; see the added files referenced in the backlog and used by Web controllers.)

Users ↔ Roles

GetUserWithRolesForEditHandler.HandleAsync(Guid userId, ct)
Returns User (id/email/rowVersion), all roles, and current selected role ids.

UpdateUserRolesHandler.HandleAsync(UserRolesUpdateDto, ct)
Saves the selection; expects RowVersion.
(Consumed by UsersController -> Roles actions.)

Users — Password (Admin)

SetUserPasswordByAdminHandler.HandleAsync(UserAdminSetPasswordDto, ct)
UserAdminSetPasswordDto { Guid Id; string NewPassword; }
Admin sets a new password without knowing the current one. SecurityStamp is rotated; prior sessions invalidated.
(Integrated in Web’s UsersController.ChangePassword view/action.)

Infrastructure: Permission checks rely on IPermissionService registered in Identity infrastructure. Web’s authorization attribute/helper call into this service.

3) Web Layer — Security & Policies
Razor Permission Helper (for conditional UI)

PermissionRazorHelper.HasAsync("PermissionKey")
In views, to hide/show buttons/sections based on effective permissions. Honors FullAdminAccess bypass.

Attribute-based Authorization

PermissionAuthorizeAttribute("PermissionKey") on controllers/actions.
Execution: checks authenticated principal; bypasses with FullAdminAccess; then queries IPermissionService.HasAsync.

Global Alerts UX

Use ~/Areas/Admin/Views/Shared/_Alerts.cshtml for consistent Success/Error/Warning/Info messages. Controller actions set TempData["Success"|"Error"|...].

Active Navigation

ActiveNavLinkTagHelper renders <a> with generated href (no raw asp-* left in output) and auto-adds active based on current route.
Usage in Admin layout menus, deep-link friendly.

Pager (Deep-link friendly)

PagerTagHelper builds Bootstrap pagination preserving area/controller/action + arbitrary asp-route-* (e.g. query, pageSize). Works well with browser back/forward.

4) UI Flows (Web)
A) Roles → Edit Permissions

From Roles/Index click Permissions.

GET /Admin/Identity/Roles/Permissions/{roleId} → calls GetRoleWithPermissionsForEditHandler.

View renders: role header, checklist of permissions, rowversion hidden, alerts at top.

POST with selected ids → UpdateRolePermissionsHandler.

On success: TempData["Success"]="Permissions updated." → redirect to Roles/Index.

B) Users → Edit Roles

From Users/Index click Roles.

GET /Admin/Identity/Users/Roles/{userId} → calls GetUserWithRolesForEditHandler.

View renders: user header (email/id), checklist of roles, rowversion hidden, alerts.

POST → UpdateUserRolesHandler.

Success: TempData["Success"]="User roles updated." → redirect Users/Index.

C) Users → Change Password (by Admin)

From Users/Index click Password.

GET /Admin/Users/ChangePassword?id=... shows email/id and fields NewPassword + ConfirmNewPassword.

POST → SetUserPasswordByAdminHandler.

Success: TempData Success → redirect Users/Index. (UI template present in ChangePassword.cshtml.)

D) Users → Edit Profile & Addresses

Edit page shows user form + Addresses grid partial. CRUD via modal; after POST, grid refreshes with partial return; alerts refreshed via AlertsFragment endpoint.

Set default billing/shipping posts, stays on same page, refreshes grid + alerts.

5) Concurrency & Common Errors

RowVersion is required on update/delete DTOs. Missing/old tokens → Application throws concurrency error; Web maps to either TempData["Error"] or ModelState error with a friendly message and suggests reload. Example handling exists in Product controller for reference.

Result pattern: Handlers return Result with Succeeded, Value, Error. Always check Succeeded; don’t assume Value on failure. (Shared Result lives in Shared project and is used uniformly.)

Validation: FluentValidation in Application. Web displays per-field and summary errors. Validators are auto-registered via DI in Web composition.

6) Composition Root (DI)

Web layer calls Infrastructure registration extensions; Identity includes IPermissionService.

All Application validators are added; maintainers should add new validators in Application and they will flow here.

7) View Conventions

Top of each page: _Alerts.cshtml.

Use ActiveNavLinkTagHelper for menus; PagerTagHelper for lists.

Use confirmation modal _ConfirmDeleteModal for deletes (post RowVersion and Id).
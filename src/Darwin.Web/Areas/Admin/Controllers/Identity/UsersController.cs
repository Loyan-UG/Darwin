using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Identity.Commands;
using Darwin.Web.Security;
using Darwin.Web.Areas.Admin.ViewModels.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers.Identity
{
    /// <summary>
    /// Admin list + edit for users. 
    /// </summary>
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    [PermissionAuthorize("AccessAdminPanel")]
    public sealed class UsersController : Controller
    {
        private readonly GetUsersPageHandler _getPage;
        private readonly GetUserForEditHandler _getForEdit;
        private readonly UpdateUserHandler _update;
        private readonly CreateUserHandler _create;

        public UsersController(
            GetUsersPageHandler getPage,
            GetUserForEditHandler getForEdit,
            UpdateUserHandler update,
            CreateUserHandler create)
        {
            _getPage = getPage;
            _getForEdit = getForEdit;
            _update = update;
            _create = create;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _getPage.HandleAsync(q, active, page, pageSize, ct);

            var vm = new UserIndexVm
            {
                Q = q,
                Active = active,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(u => new UserRowVm
                {
                    Id = u.Id,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    HasPassword = u.HasPasswordHash,
                    RolesCsv = u.RolesCsv ?? "",
                    CreatedAtUtc = u.CreatedAtUtc,
                    ModifiedAtUtc = u.ModifiedAtUtc
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct);
            if (dto == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new UserEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Email = dto.Email ?? string.Empty,
                IsActive = dto.IsActive,
                Locale = dto.Locale ?? "de-DE",
                Timezone = dto.Timezone ?? "Europe/Berlin",
                Currency = dto.Currency ?? "EUR",
                RoleIds = dto.RoleIds?.ToList() ?? new()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new UserEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                IsActive = vm.IsActive,
                Locale = vm.Locale,
                Timezone = vm.Timezone,
                Currency = vm.Currency,
                RoleIds = vm.RoleIds.ToArray()
            };

            var res = await _update.HandleAsync(dto, ct);
            if (!res.Succeeded)
            {
                TempData["Error"] = res.Error ?? "Failed to update user.";
                return View(vm);
            }

            TempData["Success"] = "User updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Default values
            return View(new UserEditVm
            {
                RowVersion = Array.Empty<byte>(),
                IsActive = true,
                Locale = "de-DE",
                Timezone = "Europe/Berlin",
                Currency = "EUR"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserEditVm vm, [FromForm] string password, CancellationToken ct = default)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(password))
            {
                if (string.IsNullOrWhiteSpace(password))
                    TempData["Warning"] = "Password is required.";
                return View(vm);
            }

            var dto = new UserCreateDto
            {
                Email = vm.Email.Trim(),
                Password = password,
                Locale = vm.Locale,
                Timezone = vm.Timezone,
                Currency = vm.Currency,
                RoleIds = vm.RoleIds.ToArray()
            };

            var res = await _create.HandleAsync(dto, ct);
            if (!res.Succeeded)
            {
                TempData["Error"] = res.Error ?? "Failed to create user.";
                return View(vm);
            }

            TempData["Success"] = "User created.";
            return RedirectToAction(nameof(Index));
        }
    }
}

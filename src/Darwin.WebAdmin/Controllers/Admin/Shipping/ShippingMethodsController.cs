using Darwin.Application.Shipping.Commands;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Queries;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.ViewModels.Shipping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebAdmin.Controllers.Admin.Shipping;

[PermissionAuthorize(PermissionKeys.FullAdminAccess)]
public sealed class ShippingMethodsController : AdminBaseController
{
    private readonly GetShippingMethodsPageHandler _getPage;
    private readonly GetShippingMethodOpsSummaryHandler _getSummary;
    private readonly GetShippingMethodForEditHandler _getForEdit;
    private readonly CreateShippingMethodHandler _create;
    private readonly UpdateShippingMethodHandler _update;

    public ShippingMethodsController(
        GetShippingMethodsPageHandler getPage,
        GetShippingMethodOpsSummaryHandler getSummary,
        GetShippingMethodForEditHandler getForEdit,
        CreateShippingMethodHandler create,
        UpdateShippingMethodHandler update)
    {
        _getPage = getPage;
        _getSummary = getSummary;
        _getForEdit = getForEdit;
        _create = create;
        _update = update;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? query = null,
        ShippingMethodQueueFilter filter = ShippingMethodQueueFilter.All,
        CancellationToken ct = default)
    {
        var (items, total) = await _getPage.HandleAsync(page, pageSize, query, filter, ct);
        var summary = await _getSummary.HandleAsync(ct);
        var vm = new ShippingMethodsListVm
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Query = query ?? string.Empty,
            Filter = filter,
            Summary = new ShippingMethodOpsSummaryVm
            {
                TotalCount = summary.TotalCount,
                ActiveCount = summary.ActiveCount,
                InactiveCount = summary.InactiveCount,
                MissingRatesCount = summary.MissingRatesCount,
                DhlCount = summary.DhlCount,
                GlobalCoverageCount = summary.GlobalCoverageCount,
                MultiRateCount = summary.MultiRateCount
            },
            Playbooks = BuildPlaybooks(),
            FilterItems = BuildFilterItems(filter),
            PageSizeItems = BuildPageSizeItems(pageSize),
            Items = items.Select(x => new ShippingMethodListItemVm
            {
                Id = x.Id,
                Name = x.Name,
                Carrier = x.Carrier,
                Service = x.Service,
                DisplayName = x.DisplayName,
                CountriesCsv = x.CountriesCsv,
                Currency = x.Currency,
                IsActive = x.IsActive,
                RatesCount = x.RatesCount,
                IsDhl = x.IsDhl,
                HasGlobalCoverage = x.HasGlobalCoverage,
                HasMultipleRates = x.HasMultipleRates,
                ModifiedAtUtc = x.ModifiedAtUtc
            }).ToList()
        };

        return RenderIndexWorkspace(vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var vm = new ShippingMethodEditVm
        {
            Currency = "EUR",
            Rates = new List<ShippingRateEditVm> { new() { SortOrder = 0 } }
        };
        return RenderEditor(vm, true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShippingMethodEditVm vm, CancellationToken ct = default)
    {
        EnsureRates(vm);
        if (!ModelState.IsValid)
        {
            return RenderEditor(vm, true);
        }

        var dto = new ShippingMethodCreateDto
        {
            Name = vm.Name,
            Carrier = vm.Carrier,
            Service = vm.Service,
            CountriesCsv = vm.CountriesCsv,
            IsActive = vm.IsActive,
            Currency = vm.Currency,
            Rates = vm.Rates.Select(MapRateDto).ToList()
        };

        try
        {
            await _create.HandleAsync(dto, ct);
            SetSuccessMessage("ShippingMethodCreatedMessage");
            return RedirectOrHtmx(nameof(Index), new { });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RenderEditor(vm, true);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        var dto = await _getForEdit.HandleAsync(id, ct);
        if (dto is null)
        {
            SetErrorMessage("ShippingMethodNotFoundMessage");
            return RedirectOrHtmx(nameof(Index), new { });
        }

        var vm = new ShippingMethodEditVm
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,
            Name = dto.Name,
            Carrier = dto.Carrier,
            Service = dto.Service,
            CountriesCsv = dto.CountriesCsv,
            IsActive = dto.IsActive,
            Currency = dto.Currency,
            Rates = dto.Rates.Select(x => new ShippingRateEditVm
            {
                Id = x.Id,
                MaxShipmentMass = x.MaxShipmentMass,
                MaxSubtotalNetMinor = x.MaxSubtotalNetMinor,
                PriceMinor = x.PriceMinor,
                SortOrder = x.SortOrder
            }).ToList()
        };

        EnsureRates(vm);
        return RenderEditor(vm, false);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ShippingMethodEditVm vm, CancellationToken ct = default)
    {
        EnsureRates(vm);
        if (!ModelState.IsValid)
        {
            return RenderEditor(vm, false);
        }

        var dto = new ShippingMethodEditDto
        {
            Id = vm.Id,
            RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
            Name = vm.Name,
            Carrier = vm.Carrier,
            Service = vm.Service,
            CountriesCsv = vm.CountriesCsv,
            IsActive = vm.IsActive,
            Currency = vm.Currency,
            Rates = vm.Rates.Select(MapRateDto).ToList()
        };

        try
        {
            await _update.HandleAsync(dto, ct);
            SetSuccessMessage("ShippingMethodUpdatedMessage");
            return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            SetErrorMessage("ShippingMethodConcurrencyMessage");
            return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RenderEditor(vm, false);
        }
    }

    private static ShippingRateDto MapRateDto(ShippingRateEditVm vm)
    {
        return new ShippingRateDto
        {
            Id = vm.Id,
            MaxShipmentMass = vm.MaxShipmentMass,
            MaxSubtotalNetMinor = vm.MaxSubtotalNetMinor,
            PriceMinor = vm.PriceMinor,
            SortOrder = vm.SortOrder
        };
    }

    private static void EnsureRates(ShippingMethodEditVm vm)
    {
        vm.Rates ??= new List<ShippingRateEditVm>();
        vm.Rates = vm.Rates
            .Where(x => x.MaxShipmentMass.HasValue || x.MaxSubtotalNetMinor.HasValue || x.PriceMinor > 0 || x.Id.HasValue)
            .OrderBy(x => x.SortOrder)
            .ToList();

        if (vm.Rates.Count == 0)
        {
            vm.Rates.Add(new ShippingRateEditVm { SortOrder = 0 });
        }

        for (var i = 0; i < vm.Rates.Count; i++)
        {
            vm.Rates[i].SortOrder = i;
        }
    }

    private IActionResult RenderEditor(ShippingMethodEditVm vm, bool isCreate)
    {
        ViewData["IsCreate"] = isCreate;
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/ShippingMethods/_ShippingMethodEditorShell.cshtml", vm);
        }
        return isCreate ? View("Create", vm) : View("Edit", vm);
    }

    private IActionResult RenderIndexWorkspace(ShippingMethodsListVm vm)
    {
        if (IsHtmxRequest())
        {
            return PartialView("~/Views/ShippingMethods/Index.cshtml", vm);
        }

        return View("Index", vm);
    }

    private IActionResult RedirectOrHtmx(string actionName, object routeValues)
    {
        if (IsHtmxRequest())
        {
            Response.Headers["HX-Redirect"] = Url.Action(actionName, routeValues) ?? string.Empty;
            return new EmptyResult();
        }

        return RedirectToAction(actionName, routeValues);
    }

    private bool IsHtmxRequest()
    {
        return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<SelectListItem> BuildFilterItems(ShippingMethodQueueFilter selected)
    {
        yield return new SelectListItem(T("AllMethods"), ShippingMethodQueueFilter.All.ToString(), selected == ShippingMethodQueueFilter.All);
        yield return new SelectListItem(T("Active"), ShippingMethodQueueFilter.Active.ToString(), selected == ShippingMethodQueueFilter.Active);
        yield return new SelectListItem(T("Inactive"), ShippingMethodQueueFilter.Inactive.ToString(), selected == ShippingMethodQueueFilter.Inactive);
        yield return new SelectListItem(T("MissingRates"), ShippingMethodQueueFilter.MissingRates.ToString(), selected == ShippingMethodQueueFilter.MissingRates);
        yield return new SelectListItem(T("DhlMethods"), ShippingMethodQueueFilter.Dhl.ToString(), selected == ShippingMethodQueueFilter.Dhl);
        yield return new SelectListItem(T("GlobalCoverage"), ShippingMethodQueueFilter.GlobalCoverage.ToString(), selected == ShippingMethodQueueFilter.GlobalCoverage);
        yield return new SelectListItem(T("MultiRate"), ShippingMethodQueueFilter.MultiRate.ToString(), selected == ShippingMethodQueueFilter.MultiRate);
    }

    private List<ShippingMethodPlaybookVm> BuildPlaybooks()
    {
        return new List<ShippingMethodPlaybookVm>
        {
            new()
            {
                Title = T("MissingRates"),
                ScopeNote = T("ShippingPlaybookMissingRatesScope"),
                OperatorAction = T("ShippingPlaybookMissingRatesAction")
            },
            new()
            {
                Title = T("GlobalCoverage"),
                ScopeNote = T("ShippingPlaybookGlobalCoverageScope"),
                OperatorAction = T("ShippingPlaybookGlobalCoverageAction")
            },
            new()
            {
                Title = T("DhlMethods"),
                ScopeNote = T("ShippingPlaybookDhlScope"),
                OperatorAction = T("ShippingPlaybookDhlAction")
            }
        };
    }

    private static IEnumerable<SelectListItem> BuildPageSizeItems(int selected)
    {
        var sizes = new[] { 10, 20, 50, 100 };
        return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selected)).ToList();
    }
}


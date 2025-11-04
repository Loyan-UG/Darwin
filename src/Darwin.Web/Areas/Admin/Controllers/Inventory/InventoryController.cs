using Darwin.Application.Inventory.Queries;
using Darwin.Web.Areas.Admin.Controllers;
using Darwin.Web.Areas.Admin.ViewModels.Inventory;
using Darwin.Web.TagHelpers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Inventory
{
    [Area("Admin")]
    public sealed class InventoryController : AdminBaseController
    {
        private readonly GetInventoryLedgerHandler _getLedger;

        public InventoryController(GetInventoryLedgerHandler getLedger)
        {
            _getLedger = getLedger;
        }

        /// <summary>
        /// Paged ledger for a single variant.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VariantLedger(Guid variantId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            // Application handler returns a paged projection; map to VM 
            var dto = await _getLedger.HandleAsync(variantId, page, pageSize, ct);

            var items = dto.Items.Select(x => new InventoryLedgerItemVm
            {
                VariantId = x.VariantId,
                QuantityDelta = x.QuantityDelta,
                Reason = x.Reason,
                ReferenceId = x.ReferenceId,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList();

            var vm = new InventoryLedgerListVm
            {
                VariantId = variantId,
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = dto.Total
            };

            return View(vm);
        }
    }
}

using Darwin.Application.Common.Queries;
using Darwin.Application.Inventory.Queries;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.Services.Admin
{
    /// <summary>
    /// Composes lightweight reference data from Application handlers into MVC-friendly select lists.
    /// </summary>
    public sealed class AdminReferenceDataService
    {
        private readonly GetBusinessLookupHandler _getBusinesses;
        private readonly GetWarehouseLookupHandler _getWarehouses;
        private readonly GetUserLookupHandler _getUsers;
        private readonly GetCustomerLookupHandler _getCustomers;
        private readonly GetCustomerSegmentLookupHandler _getCustomerSegments;
        private readonly GetProductVariantLookupHandler _getVariants;
        private readonly GetSupplierLookupHandler _getSuppliers;
        private readonly GetFinancialAccountLookupHandler _getAccounts;

        public AdminReferenceDataService(
            GetBusinessLookupHandler getBusinesses,
            GetWarehouseLookupHandler getWarehouses,
            GetUserLookupHandler getUsers,
            GetCustomerLookupHandler getCustomers,
            GetCustomerSegmentLookupHandler getCustomerSegments,
            GetProductVariantLookupHandler getVariants,
            GetSupplierLookupHandler getSuppliers,
            GetFinancialAccountLookupHandler getAccounts)
        {
            _getBusinesses = getBusinesses ?? throw new ArgumentNullException(nameof(getBusinesses));
            _getWarehouses = getWarehouses ?? throw new ArgumentNullException(nameof(getWarehouses));
            _getUsers = getUsers ?? throw new ArgumentNullException(nameof(getUsers));
            _getCustomers = getCustomers ?? throw new ArgumentNullException(nameof(getCustomers));
            _getCustomerSegments = getCustomerSegments ?? throw new ArgumentNullException(nameof(getCustomerSegments));
            _getVariants = getVariants ?? throw new ArgumentNullException(nameof(getVariants));
            _getSuppliers = getSuppliers ?? throw new ArgumentNullException(nameof(getSuppliers));
            _getAccounts = getAccounts ?? throw new ArgumentNullException(nameof(getAccounts));
        }

        public async Task<Guid?> ResolveBusinessIdAsync(Guid? requestedBusinessId, CancellationToken ct = default)
        {
            var items = await _getBusinesses.HandleAsync(ct).ConfigureAwait(false);
            return ResolveSelectedId(items.Select(x => x.Id).ToList(), requestedBusinessId);
        }

        public async Task<Guid?> ResolveWarehouseIdAsync(Guid? requestedWarehouseId, Guid? businessId = null, CancellationToken ct = default)
        {
            var items = await _getWarehouses.HandleAsync(ct).ConfigureAwait(false);
            if (businessId.HasValue)
            {
                items = items.Where(x => x.BusinessId == businessId.Value).ToList();
            }

            return ResolveSelectedId(items.Select(x => x.Id).ToList(), requestedWarehouseId);
        }

        public async Task<List<SelectListItem>> GetBusinessOptionsAsync(Guid? selectedBusinessId, CancellationToken ct = default)
        {
            var items = await _getBusinesses.HandleAsync(ct).ConfigureAwait(false);
            return items
                .Select(x => new SelectListItem(
                    x.SecondaryLabel is null ? x.Label : $"{x.Label} ({x.SecondaryLabel})",
                    x.Id.ToString(),
                    selectedBusinessId == x.Id))
                .ToList();
        }

        public async Task<List<SelectListItem>> GetWarehouseOptionsAsync(Guid? selectedWarehouseId, Guid? businessId = null, CancellationToken ct = default)
        {
            var items = await _getWarehouses.HandleAsync(ct).ConfigureAwait(false);
            if (businessId.HasValue)
            {
                items = items.Where(x => x.BusinessId == businessId.Value).ToList();
            }

            return items
                .Select(x => new SelectListItem(
                    string.IsNullOrWhiteSpace(x.Location) ? x.Name : $"{x.Name} ({x.Location})",
                    x.Id.ToString(),
                    selectedWarehouseId == x.Id))
                .ToList();
        }

        public async Task<List<SelectListItem>> GetUserOptionsAsync(Guid? selectedUserId, bool includeEmpty = true, CancellationToken ct = default)
        {
            var items = await _getUsers.HandleAsync(ct).ConfigureAwait(false);
            return BuildOptions(items, selectedUserId, includeEmpty, "Unassigned");
        }

        public async Task<List<SelectListItem>> GetCustomerOptionsAsync(Guid? selectedCustomerId, bool includeEmpty = false, CancellationToken ct = default)
        {
            var items = await _getCustomers.HandleAsync(ct).ConfigureAwait(false);
            return BuildOptions(items, selectedCustomerId, includeEmpty, "Select customer");
        }

        public async Task<List<SelectListItem>> GetCustomerSegmentOptionsAsync(Guid? selectedSegmentId, bool includeEmpty = false, CancellationToken ct = default)
        {
            var items = await _getCustomerSegments.HandleAsync(ct).ConfigureAwait(false);
            return BuildOptions(items, selectedSegmentId, includeEmpty, "Select segment");
        }

        public async Task<List<SelectListItem>> GetVariantOptionsAsync(Guid? selectedVariantId, CancellationToken ct = default)
        {
            var items = await _getVariants.HandleAsync(ct).ConfigureAwait(false);
            return BuildOptions(items, selectedVariantId, false, "Select variant");
        }

        public async Task<List<SelectListItem>> GetSupplierOptionsAsync(Guid businessId, Guid? selectedSupplierId, bool includeEmpty = false, CancellationToken ct = default)
        {
            var items = await _getSuppliers.HandleAsync(businessId, ct).ConfigureAwait(false);
            return BuildOptions(items, selectedSupplierId, includeEmpty, "Select supplier");
        }

        public async Task<List<SelectListItem>> GetFinancialAccountOptionsAsync(Guid businessId, Guid? selectedAccountId, bool includeEmpty = false, CancellationToken ct = default)
        {
            var items = await _getAccounts.HandleAsync(businessId, ct).ConfigureAwait(false);
            return BuildOptions(items, selectedAccountId, includeEmpty, "Select account");
        }

        private static Guid? ResolveSelectedId(IReadOnlyCollection<Guid> availableIds, Guid? requestedId)
        {
            if (availableIds.Count == 0)
            {
                return null;
            }

            if (requestedId.HasValue && availableIds.Contains(requestedId.Value))
            {
                return requestedId;
            }

            return availableIds.First();
        }

        private static List<SelectListItem> BuildOptions(
            IEnumerable<Darwin.Application.Common.DTOs.LookupItemDto> items,
            Guid? selectedId,
            bool includeEmpty,
            string emptyLabel)
        {
            var options = new List<SelectListItem>();
            if (includeEmpty)
            {
                options.Add(new SelectListItem(emptyLabel, string.Empty, !selectedId.HasValue));
            }

            options.AddRange(items.Select(x => new SelectListItem(
                x.SecondaryLabel is null ? x.Label : $"{x.Label} ({x.SecondaryLabel})",
                x.Id.ToString(),
                selectedId == x.Id)));

            return options;
        }
    }
}

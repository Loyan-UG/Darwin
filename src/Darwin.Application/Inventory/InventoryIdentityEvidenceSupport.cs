using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory;

internal static class InventoryIdentityEvidenceSupport
{
    public static void Normalize(IEnumerable<InventoryIdentityEvidenceDto>? identities)
    {
        if (identities is null) return;
        foreach (var identity in identities)
        {
            identity.LotCodeSnapshot = NormalizeOptional(identity.LotCodeSnapshot);
            identity.SupplierLotCodeSnapshot = NormalizeOptional(identity.SupplierLotCodeSnapshot);
            identity.SerialNumberSnapshot = NormalizeOptional(identity.SerialNumberSnapshot);
            identity.HandlingUnitCodeSnapshot = NormalizeOptional(identity.HandlingUnitCodeSnapshot);
            identity.MetadataJson = NormalizeMetadataJson(identity.MetadataJson);
        }
    }

    public static bool LooksSensitive(IEnumerable<InventoryIdentityEvidenceDto>? identities)
        => identities?.Any(identity =>
            FoundationInputNormalizer.LooksSensitive(identity.LotCodeSnapshot) ||
            FoundationInputNormalizer.LooksSensitive(identity.SupplierLotCodeSnapshot) ||
            FoundationInputNormalizer.LooksSensitive(identity.SerialNumberSnapshot) ||
            FoundationInputNormalizer.LooksSensitive(identity.HandlingUnitCodeSnapshot) ||
            FoundationInputNormalizer.LooksSensitive(identity.MetadataJson)) == true;

    public static bool IsBlank(InventoryIdentityEvidenceDto identity)
        => identity.InventoryLotId is null &&
           identity.InventorySerialUnitId is null &&
           identity.HandlingUnitId is null &&
           identity.Quantity <= 0 &&
           string.IsNullOrWhiteSpace(identity.LotCodeSnapshot) &&
           string.IsNullOrWhiteSpace(identity.SerialNumberSnapshot) &&
           string.IsNullOrWhiteSpace(identity.HandlingUnitCodeSnapshot);

    public static async Task<Result<IdentitySnapshots>> PopulateSnapshotsAsync(
        IAppDbContext db,
        Guid businessId,
        Guid productVariantId,
        Guid? inventoryLotId,
        Guid? inventorySerialUnitId,
        Guid? handlingUnitId,
        IStringLocalizer<ValidationResource> localizer,
        CancellationToken ct)
    {
        var snapshots = new IdentitySnapshots();

        if (inventoryLotId.HasValue)
        {
            var lot = await db.Set<InventoryLot>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == inventoryLotId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (lot is null || lot.BusinessId != businessId || lot.ProductVariantId != productVariantId)
            {
                return Result<IdentitySnapshots>.Fail(localizer["InventoryLotNotFound"]);
            }

            snapshots.LotCodeSnapshot = lot.LotCode;
            snapshots.SupplierLotCodeSnapshot = lot.SupplierLotCode;
            snapshots.ExpiryDateUtc = lot.ExpiryDateUtc;
        }

        if (inventorySerialUnitId.HasValue)
        {
            var serial = await db.Set<InventorySerialUnit>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == inventorySerialUnitId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (serial is null || serial.BusinessId != businessId || serial.ProductVariantId != productVariantId)
            {
                return Result<IdentitySnapshots>.Fail(localizer["InventorySerialUnitNotFound"]);
            }

            if (inventoryLotId.HasValue && serial.InventoryLotId != inventoryLotId)
            {
                return Result<IdentitySnapshots>.Fail(localizer["InventorySerialUnitNotFound"]);
            }

            snapshots.SerialNumberSnapshot = serial.SerialNumber;
            snapshots.ExpiryDateUtc ??= serial.ExpiryDateUtc;
        }

        if (handlingUnitId.HasValue)
        {
            var handlingUnit = await db.Set<HandlingUnit>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == handlingUnitId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (handlingUnit is null || handlingUnit.BusinessId != businessId)
            {
                return Result<IdentitySnapshots>.Fail(localizer["HandlingUnitNotFound"]);
            }

            snapshots.HandlingUnitCodeSnapshot = handlingUnit.Code;
        }

        return Result<IdentitySnapshots>.Ok(snapshots);
    }

    public static async Task<Result> ValidateRequiredEvidenceAsync<TLine, TIdentity>(
        IAppDbContext db,
        Guid businessId,
        IEnumerable<TLine> lines,
        Func<TLine, bool> lineIncluded,
        Func<TLine, Guid?> productVariantId,
        Func<TLine, int> requiredQuantity,
        Func<TLine, IEnumerable<TIdentity>> identities,
        Func<TIdentity, Guid?> lotId,
        Func<TIdentity, Guid?> serialId,
        Func<TIdentity, Guid?> handlingUnitId,
        Func<TIdentity, int> identityQuantity,
        Func<TIdentity, DateTime?> expiryDate,
        Func<TIdentity, string?> supplierLotCode,
        IStringLocalizer<ValidationResource> localizer,
        string genericRequiredKey,
        string invalidQuantityKey,
        string lotRequiredKey,
        string serialRequiredKey,
        string expiryRequiredKey,
        string supplierLotRequiredKey,
        string handlingUnitRequiredKey,
        CancellationToken ct)
    {
        var policies = await db.Set<ProductTrackingPolicy>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && x.Status == ProductTrackingPolicyStatus.Active && !x.IsDeleted)
            .ToDictionaryAsync(x => x.ProductVariantId, ct)
            .ConfigureAwait(false);

        foreach (var line in lines.Where(lineIncluded))
        {
            var variantId = productVariantId(line);
            var quantity = requiredQuantity(line);
            if (!variantId.HasValue || quantity <= 0) continue;
            if (!policies.TryGetValue(variantId.Value, out var policy) || policy.TrackingMode == ProductTrackingMode.Untracked) continue;

            var activeIdentities = identities(line).ToList();
            if (activeIdentities.Count == 0) return Result.Fail(localizer[genericRequiredKey]);
            if (activeIdentities.Any(x => identityQuantity(x) <= 0)) return Result.Fail(localizer[invalidQuantityKey]);

            if (policy.TrackingMode is ProductTrackingMode.LotTracked or ProductTrackingMode.LotAndExpiryTracked &&
                activeIdentities.Where(x => lotId(x).HasValue).Sum(identityQuantity) != quantity)
            {
                return Result.Fail(localizer[lotRequiredKey]);
            }

            if (policy.TrackingMode is ProductTrackingMode.SerialTracked or ProductTrackingMode.SerialAndExpiryTracked)
            {
                var serialIdentities = activeIdentities.Where(x => serialId(x).HasValue).ToList();
                if (serialIdentities.Count != quantity || serialIdentities.Any(x => identityQuantity(x) != 1))
                {
                    return Result.Fail(localizer[serialRequiredKey]);
                }
            }

            if (policy.RequiresExpiryDate && activeIdentities.Any(x => !expiryDate(x).HasValue))
            {
                return Result.Fail(localizer[expiryRequiredKey]);
            }

            if (policy.RequiresSupplierLot && activeIdentities.Any(x => string.IsNullOrWhiteSpace(supplierLotCode(x))))
            {
                return Result.Fail(localizer[supplierLotRequiredKey]);
            }

            if (policy.RequiresHandlingUnit &&
                activeIdentities.Where(x => handlingUnitId(x).HasValue).Sum(identityQuantity) != quantity)
            {
                return Result.Fail(localizer[handlingUnitRequiredKey]);
            }
        }

        return Result.Ok();
    }

    public sealed class IdentitySnapshots
    {
        public string? LotCodeSnapshot { get; set; }
        public string? SupplierLotCodeSnapshot { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public string? SerialNumberSnapshot { get; set; }
        public string? HandlingUnitCodeSnapshot { get; set; }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeMetadataJson(string? value)
    {
        var normalized = NormalizeOptional(value);
        return string.Equals(normalized, "{}", StringComparison.Ordinal) ? null : normalized;
    }
}

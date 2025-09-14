using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Shipping.DTOs;
using Darwin.Domain.Entities.Shipping;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Shipping.Validators
{
    /// <summary>
    /// Ensures that the combination of Name, Carrier and Service is unique across all shipping methods when creating a new one.
    /// </summary>
    public sealed class ShippingMethodCreateUniqueNameValidator : AbstractValidator<ShippingMethodCreateDto>
    {
        private readonly IAppDbContext _db;

        public ShippingMethodCreateUniqueNameValidator(IAppDbContext db)
        {
            _db = db;
            RuleFor(x => x)
                .MustAsync(BeUniqueName).WithMessage("A shipping method with the same Name, Carrier, and Service already exists");
        }

        private async Task<bool> BeUniqueName(ShippingMethodCreateDto dto, CancellationToken ct)
        {
            return !await _db.Set<ShippingMethod>()
                .AsNoTracking()
                .AnyAsync(m =>
                    m.Name == dto.Name &&
                    (m.Carrier ?? string.Empty) == (dto.Carrier ?? string.Empty) &&
                    (m.Service ?? string.Empty) == (dto.Service ?? string.Empty),
                    ct);
        }
    }

    /// <summary>
    /// Ensures that the Name+Carrier+Service combination remains unique when editing a shipping method.
    /// </summary>
    public sealed class ShippingMethodEditUniqueNameValidator : AbstractValidator<ShippingMethodEditDto>
    {
        private readonly IAppDbContext _db;

        public ShippingMethodEditUniqueNameValidator(IAppDbContext db)
        {
            _db = db;
            RuleFor(x => x)
                .MustAsync(BeUniqueName).WithMessage("A different shipping method already uses this Name, Carrier, and Service");
        }

        private async Task<bool> BeUniqueName(ShippingMethodEditDto dto, CancellationToken ct)
        {
            return !await _db.Set<ShippingMethod>()
                .AsNoTracking()
                .AnyAsync(m =>
                    m.Id != dto.Id &&
                    m.Name == dto.Name &&
                    (m.Carrier ?? string.Empty) == (dto.Carrier ?? string.Empty) &&
                    (m.Service ?? string.Empty) == (dto.Service ?? string.Empty),
                    ct);
        }
    }
}

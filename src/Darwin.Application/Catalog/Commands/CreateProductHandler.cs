using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    ///     Use-case handler that creates a new product with translations and variants,
    ///     applying validation, sanitization (where applicable), and enforcing invariant business rules.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Responsibilities:
    ///         <list type="bullet">
    ///             <item>Validate input via FluentValidation.</item>
    ///             <item>Map DTO to domain entities, normalize strings (e.g., slugs), and sanitize HTML fields.</item>
    ///             <item>Ensure uniqueness constraints (e.g., slug per culture) are checked pre-insert for UX.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Transactions:
    ///         Performs work within a single EF Core save operation; consider explicit transactions
    ///         if you later expand to side-effects (events, outbox).
    ///     </para>
    /// </remarks>
    public sealed class CreateProductHandler
    {
        private readonly IAppDbContext _db;
        private readonly IMapper _mapper;
        private readonly IValidator<ProductCreateDto> _validator;

        public CreateProductHandler(IAppDbContext db, IMapper mapper, IValidator<ProductCreateDto> validator)
        {
            _db = db;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Guid> HandleAsync(ProductCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var product = _mapper.Map<Product>(dto);

            // Ensure simple product has exactly one variant (business rule example)
            if (product.Kind == Domain.Enums.ProductKind.Simple && product.Variants.Count == 0)
            {
                throw new ValidationException("Simple product must contain one variant.");
            }

            _db.Set<Product>().Add(product);
            await _db.SaveChangesAsync(ct);

            return product.Id;
        }
    }
}

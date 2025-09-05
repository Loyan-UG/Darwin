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
    /// Creates a product aggregate with translations and variants.
    /// No MediatR: direct application service callable from Web controllers.
    /// </summary>
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

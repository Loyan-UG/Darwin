using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Commands
{
    public sealed class CreateCustomerHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CustomerCreateDto> _validator;

        public CreateCustomerHandler(IAppDbContext db, IValidator<CustomerCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(CustomerCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            if (dto.UserId.HasValue)
            {
                var userExists = await _db.Set<User>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.UserId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (!userExists)
                {
                    throw new InvalidOperationException("Linked user not found.");
                }
            }

            var customer = new Customer
            {
                UserId = dto.UserId,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone?.Trim() ?? string.Empty,
                CompanyName = NormalizeOptional(dto.CompanyName),
                Notes = NormalizeOptional(dto.Notes),
                Addresses = dto.Addresses.Select(MapAddress).ToList()
            };

            _db.Set<Customer>().Add(customer);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return customer.Id;
        }

        private static CustomerAddress MapAddress(CustomerAddressDto dto)
        {
            return new CustomerAddress
            {
                AddressId = dto.AddressId,
                Line1 = dto.Line1.Trim(),
                Line2 = NormalizeOptional(dto.Line2),
                City = dto.City.Trim(),
                State = NormalizeOptional(dto.State),
                PostalCode = dto.PostalCode.Trim(),
                Country = dto.Country.Trim(),
                IsDefaultBilling = dto.IsDefaultBilling,
                IsDefaultShipping = dto.IsDefaultShipping
            };
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpdateCustomerHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CustomerEditDto> _validator;

        public UpdateCustomerHandler(IAppDbContext db, IValidator<CustomerEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(CustomerEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var customer = await _db.Set<Customer>()
                .Include(x => x.Addresses)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (customer is null)
            {
                throw new InvalidOperationException("Customer not found.");
            }

            if (!customer.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            if (dto.UserId.HasValue)
            {
                var userExists = await _db.Set<User>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.UserId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (!userExists)
                {
                    throw new InvalidOperationException("Linked user not found.");
                }
            }

            customer.UserId = dto.UserId;
            customer.FirstName = dto.FirstName.Trim();
            customer.LastName = dto.LastName.Trim();
            customer.Email = dto.Email.Trim();
            customer.Phone = dto.Phone?.Trim() ?? string.Empty;
            customer.CompanyName = NormalizeOptional(dto.CompanyName);
            customer.Notes = NormalizeOptional(dto.Notes);

            var requestedAddressIds = dto.Addresses
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            var removed = customer.Addresses
                .Where(x => !requestedAddressIds.Contains(x.Id))
                .ToList();

            foreach (var address in removed)
            {
                _db.Set<CustomerAddress>().Remove(address);
            }

            foreach (var addressDto in dto.Addresses)
            {
                var existingAddress = addressDto.Id.HasValue
                    ? customer.Addresses.FirstOrDefault(x => x.Id == addressDto.Id.Value)
                    : null;

                if (existingAddress is null)
                {
                    customer.Addresses.Add(MapAddress(addressDto));
                    continue;
                }

                existingAddress.AddressId = addressDto.AddressId;
                existingAddress.Line1 = addressDto.Line1.Trim();
                existingAddress.Line2 = NormalizeOptional(addressDto.Line2);
                existingAddress.City = addressDto.City.Trim();
                existingAddress.State = NormalizeOptional(addressDto.State);
                existingAddress.PostalCode = addressDto.PostalCode.Trim();
                existingAddress.Country = addressDto.Country.Trim();
                existingAddress.IsDefaultBilling = addressDto.IsDefaultBilling;
                existingAddress.IsDefaultShipping = addressDto.IsDefaultShipping;
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private static CustomerAddress MapAddress(CustomerAddressDto dto)
        {
            return new CustomerAddress
            {
                AddressId = dto.AddressId,
                Line1 = dto.Line1.Trim(),
                Line2 = NormalizeOptional(dto.Line2),
                City = dto.City.Trim(),
                State = NormalizeOptional(dto.State),
                PostalCode = dto.PostalCode.Trim(),
                Country = dto.Country.Trim(),
                IsDefaultBilling = dto.IsDefaultBilling,
                IsDefaultShipping = dto.IsDefaultShipping
            };
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class CreateLeadHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<LeadCreateDto> _validator;

        public CreateLeadHandler(IAppDbContext db, IValidator<LeadCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(LeadCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var lead = new Lead
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                CompanyName = NormalizeOptional(dto.CompanyName),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Source = NormalizeOptional(dto.Source),
                Notes = NormalizeOptional(dto.Notes),
                Status = dto.Status,
                AssignedToUserId = dto.AssignedToUserId,
                CustomerId = dto.CustomerId
            };

            _db.Set<Lead>().Add(lead);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return lead.Id;
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class UpdateLeadHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<LeadEditDto> _validator;

        public UpdateLeadHandler(IAppDbContext db, IValidator<LeadEditDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task HandleAsync(LeadEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var lead = await _db.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (lead is null)
            {
                throw new InvalidOperationException("Lead not found.");
            }

            if (!lead.RowVersion.SequenceEqual(dto.RowVersion))
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");
            }

            lead.FirstName = dto.FirstName.Trim();
            lead.LastName = dto.LastName.Trim();
            lead.CompanyName = NormalizeOptional(dto.CompanyName);
            lead.Email = dto.Email.Trim();
            lead.Phone = dto.Phone.Trim();
            lead.Source = NormalizeOptional(dto.Source);
            lead.Notes = NormalizeOptional(dto.Notes);
            lead.Status = dto.Status;
            lead.AssignedToUserId = dto.AssignedToUserId;
            lead.CustomerId = dto.CustomerId;

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

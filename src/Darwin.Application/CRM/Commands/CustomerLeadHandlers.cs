using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Compliance;
using Darwin.Application;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CRM.Commands
{
    public sealed class CreateCustomerHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CustomerCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateCustomerHandler(IAppDbContext db, IValidator<CustomerCreateDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
                    throw new InvalidOperationException(_localizer["LinkedUserNotFound"]);
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
                TaxProfileType = dto.TaxProfileType,
                VatId = NormalizeOptional(dto.VatId),
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateCustomerHandler(IAppDbContext db, IValidator<CustomerEditDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = customer.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (dto.UserId.HasValue)
            {
                var userExists = await _db.Set<User>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.UserId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (!userExists)
                {
                    throw new InvalidOperationException(_localizer["LinkedUserNotFound"]);
                }
            }

            customer.UserId = dto.UserId;
            customer.FirstName = dto.FirstName.Trim();
            customer.LastName = dto.LastName.Trim();
            customer.Email = dto.Email.Trim();
            customer.Phone = dto.Phone?.Trim() ?? string.Empty;
            customer.CompanyName = NormalizeOptional(dto.CompanyName);
            customer.TaxProfileType = dto.TaxProfileType;
            var normalizedVatId = NormalizeOptional(dto.VatId);
            var vatChanged = !string.Equals(customer.VatId, normalizedVatId, StringComparison.OrdinalIgnoreCase);
            customer.VatId = normalizedVatId;
            if (vatChanged)
            {
                customer.VatValidationStatus = CustomerVatValidationStatus.Unknown;
                customer.VatValidationCheckedAtUtc = null;
                customer.VatValidationSource = null;
                customer.VatValidationMessage = null;
            }
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

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
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

    public sealed class UpdateCustomerTaxProfileHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<CustomerTaxProfileUpdateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateCustomerTaxProfileHandler(
            IAppDbContext db,
            IValidator<CustomerTaxProfileUpdateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(CustomerTaxProfileUpdateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var customer = await _db.Set<Customer>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (customer is null)
            {
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = customer.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            var normalizedVatId = NormalizeOptional(dto.VatId);
            var vatChanged = !string.Equals(customer.VatId, normalizedVatId, StringComparison.OrdinalIgnoreCase);

            customer.TaxProfileType = dto.TaxProfileType;
            customer.CompanyName = NormalizeOptional(dto.CompanyName);
            customer.VatId = normalizedVatId;
            if (vatChanged)
            {
                ResetVatValidation(customer);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static void ResetVatValidation(Customer customer)
        {
            customer.VatValidationStatus = CustomerVatValidationStatus.Unknown;
            customer.VatValidationCheckedAtUtc = null;
            customer.VatValidationSource = null;
            customer.VatValidationMessage = null;
        }
    }

    public sealed class UpdateCustomerVatValidationDecisionHandler
    {
        private const int MaxSourceLength = 80;
        private const int MaxMessageLength = 512;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateCustomerVatValidationDecisionHandler(
            IAppDbContext db,
            IClock clock,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(CustomerVatValidationDecisionDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            if (!Enum.IsDefined(dto.Status) || dto.Status == CustomerVatValidationStatus.Unknown)
            {
                throw new ValidationException(_localizer["CustomerVatValidationStatusRequired"]);
            }

            var source = NormalizeOptional(dto.Source);
            var message = NormalizeOptional(dto.Message);
            if (source?.Length > MaxSourceLength)
            {
                throw new ValidationException(_localizer["CustomerVatValidationSourceTooLong"]);
            }

            if (message?.Length > MaxMessageLength)
            {
                throw new ValidationException(_localizer["CustomerVatValidationMessageTooLong"]);
            }

            var customer = await _db.Set<Customer>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (customer is null)
            {
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = customer.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (customer.TaxProfileType != CustomerTaxProfileType.Business || string.IsNullOrWhiteSpace(customer.VatId))
            {
                throw new ValidationException(_localizer["CustomerBusinessRequiresVatIdForTaxCompliance"]);
            }

            customer.VatValidationStatus = dto.Status;
            customer.VatValidationCheckedAtUtc = _clock.UtcNow;
            customer.VatValidationSource = source ?? "operator";
            customer.VatValidationMessage = message;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class ValidateCustomerVatIdHandler
    {
        private const int MaxSourceLength = 80;
        private const int MaxMessageLength = 512;

        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IVatValidationProvider _provider;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ValidateCustomerVatIdHandler(
            IAppDbContext db,
            IClock clock,
            IVatValidationProvider provider,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<CustomerVatValidationStatus> HandleAsync(CustomerVatValidationLookupDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            var customer = await _db.Set<Customer>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (customer is null)
            {
                throw new InvalidOperationException(_localizer["CustomerNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = customer.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (customer.TaxProfileType != CustomerTaxProfileType.Business || string.IsNullOrWhiteSpace(customer.VatId))
            {
                throw new ValidationException(_localizer["CustomerBusinessRequiresVatIdForTaxCompliance"]);
            }

            var result = await ValidateWithProviderAsync(customer.VatId, ct).ConfigureAwait(false);
            customer.VatValidationStatus = result.Status;
            customer.VatValidationCheckedAtUtc = _clock.UtcNow;
            customer.VatValidationSource = Truncate(NormalizeOptional(result.Source) ?? "provider", MaxSourceLength);
            customer.VatValidationMessage = Truncate(NormalizeOptional(result.Message), MaxMessageLength);

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            return customer.VatValidationStatus;
        }

        private async Task<VatValidationProviderResult> ValidateWithProviderAsync(string vatId, CancellationToken ct)
        {
            try
            {
                return await _provider.ValidateAsync(vatId, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return new VatValidationProviderResult
                {
                    Status = CustomerVatValidationStatus.Unknown,
                    Source = "provider.unavailable",
                    Message = "VAT validation provider failed; manual review is required."
                };
            }
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? Truncate(string? value, int maxLength) =>
            value is null || value.Length <= maxLength ? value : value[..maxLength];
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateLeadHandler(IAppDbContext db, IValidator<LeadEditDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(LeadEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var lead = await _db.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (lead is null)
            {
                throw new InvalidOperationException(_localizer["LeadNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = lead.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
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

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed class ConvertLeadToCustomerHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ConvertLeadToCustomerDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ConvertLeadToCustomerHandler(IAppDbContext db, IValidator<ConvertLeadToCustomerDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(ConvertLeadToCustomerDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

            var lead = await _db.Set<Lead>()
                .FirstOrDefaultAsync(x => x.Id == dto.LeadId, ct)
                .ConfigureAwait(false);

            if (lead is null)
            {
                throw new InvalidOperationException(_localizer["LeadNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = lead.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (lead.CustomerId.HasValue)
            {
                lead.Status = LeadStatus.Converted;
                try
                {
                    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
                }

                return lead.CustomerId.Value;
            }

            if (dto.UserId.HasValue)
            {
                var userExists = await _db.Set<User>()
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.UserId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (!userExists)
                {
                    throw new InvalidOperationException(_localizer["LinkedUserNotFound"]);
                }
            }

            var existingCustomer = await _db.Set<Customer>()
                .FirstOrDefaultAsync(x =>
                    !x.IsDeleted &&
                    ((dto.UserId.HasValue && x.UserId == dto.UserId.Value) ||
                     (!dto.UserId.HasValue && x.Email == lead.Email)), ct)
                .ConfigureAwait(false);

            if (existingCustomer is null)
            {
                existingCustomer = new Customer
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    FirstName = lead.FirstName.Trim(),
                    LastName = lead.LastName.Trim(),
                    Email = lead.Email.Trim(),
                    Phone = lead.Phone.Trim(),
                    CompanyName = NormalizeOptional(lead.CompanyName),
                    TaxProfileType = string.IsNullOrWhiteSpace(lead.CompanyName)
                        ? Darwin.Domain.Enums.CustomerTaxProfileType.Consumer
                        : Darwin.Domain.Enums.CustomerTaxProfileType.Business,
                    Notes = dto.CopyNotesToCustomer ? NormalizeOptional(lead.Notes) : null
                };

                _db.Set<Customer>().Add(existingCustomer);
            }
            else if (dto.CopyNotesToCustomer && string.IsNullOrWhiteSpace(existingCustomer.Notes) && !string.IsNullOrWhiteSpace(lead.Notes))
            {
                existingCustomer.Notes = NormalizeOptional(lead.Notes);
            }

            if (dto.UserId.HasValue && !existingCustomer.UserId.HasValue)
            {
                existingCustomer.UserId = dto.UserId;
            }

            lead.CustomerId = existingCustomer.Id;
            lead.Status = LeadStatus.Converted;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            return existingCustomer.Id;
        }

        private static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

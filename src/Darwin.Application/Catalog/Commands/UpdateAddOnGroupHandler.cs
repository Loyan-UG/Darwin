using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Updates an add-on group while preserving nested option/value identities for existing selections.
    /// </summary>
    public sealed class UpdateAddOnGroupHandler
    {
        private readonly IAppDbContext _db;
        private readonly AddOnGroupEditValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateAddOnGroupHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(AddOnGroupEditDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) throw new ValidationException(vr.Errors);

            var g = await _db.Set<AddOnGroup>()
                .Include(x => x.Translations)
                .Include(x => x.Options).ThenInclude(o => o.Values)
                .Include(x => x.Options).ThenInclude(o => o.Translations)
                .Include(x => x.Options).ThenInclude(o => o.Values).ThenInclude(v => v.Translations)
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct);

            if (g == null) throw new InvalidOperationException(_localizer["AddOnGroupNotFound"]);

            if (!g.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            g.Name = dto.Name.Trim();
            g.Currency = dto.Currency.Trim();
            g.IsGlobal = dto.IsGlobal;
            g.SelectionMode = dto.SelectionMode;
            g.MinSelections = dto.MinSelections;
            g.MaxSelections = dto.MaxSelections;
            g.IsActive = dto.IsActive;
            SyncGroupTranslations(g, dto);

            SyncOptions(g, dto);

            await _db.SaveChangesAsync(ct);
        }

        private void SyncOptions(AddOnGroup group, AddOnGroupEditDto dto)
        {
            var existingById = group.Options
                .Where(o => !o.IsDeleted)
                .ToDictionary(o => o.Id);
            var requestedIds = new HashSet<Guid>();
            var requestedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in dto.Options.OrderBy(x => x.SortOrder))
            {
                var label = input.Label.Trim();
                if (!requestedLabels.Add(label))
                {
                    throw new ValidationException(_localizer["DuplicateVariantLinesNotAllowed"]);
                }

                AddOnOption option;
                if (input.Id.HasValue && existingById.TryGetValue(input.Id.Value, out var existingOption))
                {
                    option = existingOption;
                }
                else
                {
                    option = new AddOnOption
                    {
                        Id = Guid.NewGuid(),
                        AddOnGroupId = group.Id
                    };
                    group.Options.Add(option);
                }

                option.Label = label;
                option.SortOrder = input.SortOrder;
                option.IsDeleted = false;
                SyncOptionTranslations(option, input);
                requestedIds.Add(option.Id);

                SyncValues(option, input);
            }

            foreach (var existing in group.Options.Where(o => !o.IsDeleted).ToList())
            {
                if (!requestedIds.Contains(existing.Id))
                {
                    SoftDeleteOption(existing);
                }
            }
        }

        private void SyncValues(AddOnOption option, AddOnOptionDto dto)
        {
            var existingById = option.Values
                .Where(v => !v.IsDeleted)
                .ToDictionary(v => v.Id);
            var requestedIds = new HashSet<Guid>();
            var requestedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in dto.Values.OrderBy(x => x.SortOrder))
            {
                var label = input.Label.Trim();
                if (!requestedLabels.Add(label))
                {
                    throw new ValidationException(_localizer["DuplicateVariantLinesNotAllowed"]);
                }

                AddOnOptionValue value;
                if (input.Id.HasValue && existingById.TryGetValue(input.Id.Value, out var existingValue))
                {
                    value = existingValue;
                }
                else
                {
                    value = new AddOnOptionValue
                    {
                        Id = Guid.NewGuid(),
                        AddOnOptionId = option.Id
                    };
                    option.Values.Add(value);
                }

                value.Label = label;
                value.PriceDeltaMinor = input.PriceDeltaMinor;
                value.Hint = string.IsNullOrWhiteSpace(input.Hint) ? null : input.Hint.Trim();
                value.SortOrder = input.SortOrder;
                value.IsActive = input.IsActive;
                value.IsDeleted = false;
                SyncValueTranslations(value, input);
                requestedIds.Add(value.Id);
            }

            foreach (var existing in option.Values.Where(v => !v.IsDeleted).ToList())
            {
                if (!requestedIds.Contains(existing.Id))
                {
                    existing.IsDeleted = true;
                    existing.IsActive = false;
                }
            }
        }

        private static void SoftDeleteOption(AddOnOption option)
        {
            option.IsDeleted = true;
            foreach (var value in option.Values.Where(v => !v.IsDeleted))
            {
                value.IsDeleted = true;
                value.IsActive = false;
            }
        }

        private static void SyncGroupTranslations(AddOnGroup group, AddOnGroupEditDto dto)
        {
            foreach (var input in dto.Translations)
            {
                var culture = input.Culture.Trim();
                var translation = group.Translations.FirstOrDefault(t => string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase));
                if (translation is null)
                {
                    translation = new AddOnGroupTranslation { Culture = culture };
                    group.Translations.Add(translation);
                }

                translation.Name = input.Name.Trim();
                translation.IsDeleted = false;
            }
        }

        private static void SyncOptionTranslations(AddOnOption option, AddOnOptionDto dto)
        {
            foreach (var input in dto.Translations)
            {
                var culture = input.Culture.Trim();
                var translation = option.Translations.FirstOrDefault(t => string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase));
                if (translation is null)
                {
                    translation = new AddOnOptionTranslation { Culture = culture };
                    option.Translations.Add(translation);
                }

                translation.Label = input.Label.Trim();
                translation.IsDeleted = false;
            }
        }

        private static void SyncValueTranslations(AddOnOptionValue value, AddOnOptionValueDto dto)
        {
            foreach (var input in dto.Translations)
            {
                var culture = input.Culture.Trim();
                var translation = value.Translations.FirstOrDefault(t => string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase));
                if (translation is null)
                {
                    translation = new AddOnOptionValueTranslation { Culture = culture };
                    value.Translations.Add(translation);
                }

                translation.Label = input.Label.Trim();
                translation.IsDeleted = false;
            }
        }
    }
}

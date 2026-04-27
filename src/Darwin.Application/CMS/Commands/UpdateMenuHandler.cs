using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Validators;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CMS.Commands
{
    /// <summary>
    /// Updates a menu while preserving item and translation identities where possible.
    /// </summary>
    public sealed class UpdateMenuHandler
    {
        private readonly IAppDbContext _db;
        private readonly MenuEditDtoValidator _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateMenuHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
            _validator = new MenuEditDtoValidator(localizer);
        }

        public async Task HandleAsync(MenuEditDto dto, CancellationToken ct = default)
        {
            var validation = _validator.Validate(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            var menu = await _db.Set<Menu>()
                .Include(m => m.Items)
                .ThenInclude(i => i.Translations)
                .FirstOrDefaultAsync(m => m.Id == dto.Id, ct);

            if (menu is null) throw new InvalidOperationException(_localizer["MenuNotFound"]);

            // Concurrency check
            if (!menu.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);

            menu.Name = dto.Name.Trim();
            SyncItems(menu, dto);

            await _db.SaveChangesAsync(ct);
        }

        private static void SyncItems(Menu menu, MenuEditDto dto)
        {
            var retainedItemIds = dto.Items
                .Where(static i => i.Id.HasValue && i.Id.Value != System.Guid.Empty)
                .Select(static i => i.Id!.Value)
                .ToHashSet();

            foreach (var item in dto.Items)
            {
                var url = item.Url.Trim();
                var entity = item.Id.HasValue && item.Id.Value != System.Guid.Empty
                    ? menu.Items.FirstOrDefault(i => i.Id == item.Id.Value)
                    : menu.Items.FirstOrDefault(i => !i.IsDeleted && string.Equals(i.Url, url, System.StringComparison.OrdinalIgnoreCase));

                if (entity is null)
                {
                    entity = new MenuItem();
                    menu.Items.Add(entity);
                }

                entity.ParentId = item.ParentId;
                entity.Url = url;
                entity.SortOrder = item.SortOrder;
                entity.IsActive = item.IsActive;
                entity.IsDeleted = false;
                SyncTranslations(entity, item);
                retainedItemIds.Add(entity.Id);
            }

            foreach (var existing in menu.Items.Where(i => !i.IsDeleted && !retainedItemIds.Contains(i.Id)).ToList())
            {
                existing.IsDeleted = true;
            }
        }

        private static void SyncTranslations(MenuItem entity, MenuItemDto item)
        {
            foreach (var tr in item.Translations)
            {
                var culture = tr.Culture.Trim();
                var translation = entity.Translations.FirstOrDefault(t => string.Equals(t.Culture, culture, System.StringComparison.OrdinalIgnoreCase));
                if (translation is null)
                {
                    translation = new MenuItemTranslation { Culture = culture };
                    entity.Translations.Add(translation);
                }

                translation.Label = tr.Label.Trim();
                translation.IsDeleted = false;
            }
        }
    }
}

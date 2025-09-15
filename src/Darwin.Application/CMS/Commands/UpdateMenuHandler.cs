using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Validators;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Commands
{
    /// <summary>
    /// Updates a menu. Phase 1 strategy replaces the entire items collection for simplicity.
    /// </summary>
    public sealed class UpdateMenuHandler
    {
        private readonly IAppDbContext _db;
        private readonly MenuEditDtoValidator _validator = new();

        public UpdateMenuHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(MenuEditDto dto, CancellationToken ct = default)
        {
            var validation = _validator.Validate(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            var menu = await _db.Set<Menu>()
                .Include(m => m.Items)
                .ThenInclude(i => i.Translations)
                .FirstOrDefaultAsync(m => m.Id == dto.Id, ct);

            if (menu is null) throw new InvalidOperationException("Menu not found.");

            // Concurrency check
            if (!menu.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict detected.");

            // Update scalar
            menu.Name = dto.Name.Trim();

            // Replace items (simple and predictable for phase 1)
            // Remove existing
            menu.Items.Clear();

            // Add new
            foreach (var item in dto.Items)
            {
                var entity = new MenuItem
                {
                    ParentId = item.ParentId,
                    Url = item.Url.Trim(),
                    SortOrder = item.SortOrder,
                    IsActive = item.IsActive
                };
                foreach (var tr in item.Translations)
                {
                    entity.Translations.Add(new MenuItemTranslation
                    {
                        Culture = tr.Culture.Trim(),
                        Label = tr.Label.Trim()
                    });
                }
                menu.Items.Add(entity);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}

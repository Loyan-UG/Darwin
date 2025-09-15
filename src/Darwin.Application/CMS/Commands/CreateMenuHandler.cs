using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Validators;
using Darwin.Domain.Entities.CMS;

namespace Darwin.Application.CMS.Commands
{
    /// <summary>
    /// Creates a new menu with items and per-culture labels.
    /// </summary>
    public sealed class CreateMenuHandler
    {
        private readonly IAppDbContext _db;
        private readonly MenuCreateDtoValidator _validator = new();

        public CreateMenuHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(MenuCreateDto dto, CancellationToken ct = default)
        {
            var validation = _validator.Validate(dto);
            if (!validation.IsValid)
                throw new FluentValidation.ValidationException(validation.Errors);

            var menu = new Menu { Name = dto.Name.Trim() };

            foreach (var item in dto.Items)
            {
                var entity = new MenuItem
                {
                    ParentId = item.ParentId, // in create, typically null; hierarchical support can be added later
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

            _db.Set<Menu>().Add(menu);
            await _db.SaveChangesAsync(ct);
        }
    }
}

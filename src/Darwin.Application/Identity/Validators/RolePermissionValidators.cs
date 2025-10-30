using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    /// <summary>
    /// Basic shape validation for updating role-permission assignments.
    /// </summary>
    public sealed class RolePermissionsUpdateValidator : AbstractValidator<RolePermissionsUpdateDto>
    {
        public RolePermissionsUpdateValidator()
        {
            RuleFor(x => x.RoleId).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
            RuleFor(x => x.PermissionIds).NotNull();
            // Detailed existence checks are performed in the handler against the database.
        }
    }
}

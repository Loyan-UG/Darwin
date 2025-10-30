using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    /// <summary>
    /// Basic shape validation for updating user-role assignments.
    /// </summary>
    public sealed class UserRolesUpdateValidator : AbstractValidator<UserRolesUpdateDto>
    {
        public UserRolesUpdateValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
            RuleFor(x => x.RoleIds).NotNull();
        }
    }
}

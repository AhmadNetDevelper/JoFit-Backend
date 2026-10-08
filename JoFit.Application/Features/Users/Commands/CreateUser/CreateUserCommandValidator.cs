using FluentValidation;

namespace JoFit.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().Length(3, 256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GenderId).IsInEnum();
        RuleFor(x => x.CountryId).GreaterThan(0).When(x => x.CountryId.HasValue);
        RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue);
        RuleFor(x => x.JobStatusId).GreaterThan(0).When(x => x.JobStatusId.HasValue);
        RuleFor(x => x.BirthOfDate).LessThan(DateTime.UtcNow).When(x => x.BirthOfDate.HasValue);
        RuleFor(x => x.RoleName).NotEmpty();
    }
}

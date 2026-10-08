using FluentValidation;

namespace JoFit.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.UserName).NotEmpty().Length(3, 256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.GenderId).IsInEnum();
        RuleFor(x => x.CountryId).GreaterThan(0).When(x => x.CountryId.HasValue);
        RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue);
        RuleFor(x => x.JobStatusId).GreaterThan(0).When(x => x.JobStatusId.HasValue);
        RuleFor(x => x.BirthOfDate).LessThan(DateTime.UtcNow).When(x => x.BirthOfDate.HasValue);
    }
}

using FluentValidation;

namespace JoFit.Application.Features.Users.Queries.GetUsersPaged;

public sealed class GetUsersPagedQueryValidator : AbstractValidator<GetUsersPagedQuery>
{
    public GetUsersPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

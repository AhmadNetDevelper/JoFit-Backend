using FluentValidation;

namespace JoFit.Application.Features.UserJobTypes.Queries.GetUserJobTypesPaged;

public sealed class GetUserJobTypesPagedQueryValidator : AbstractValidator<GetUserJobTypesPagedQuery>
{
    public GetUserJobTypesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

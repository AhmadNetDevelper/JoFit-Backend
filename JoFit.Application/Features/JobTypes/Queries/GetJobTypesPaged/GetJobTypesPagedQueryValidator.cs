using FluentValidation;

namespace JoFit.Application.Features.JobTypes.Queries.GetJobTypesPaged;

public sealed class GetJobTypesPagedQueryValidator : AbstractValidator<GetJobTypesPagedQuery>
{
    public GetJobTypesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

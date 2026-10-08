using FluentValidation;

namespace JoFit.Application.Features.JobStatuses.Queries.GetJobStatusesPaged;

public sealed class GetJobStatusesPagedQueryValidator : AbstractValidator<GetJobStatusesPagedQuery>
{
    public GetJobStatusesPagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

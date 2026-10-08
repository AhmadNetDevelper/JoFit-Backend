using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Notifications;
using JoFit.Application.Features.Reviews;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Notifications;
using JoFit.Domain.Quotations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Quotations;

public sealed record QuotationSubjectDto(string Kind, string Label);

public sealed record QuotationReferenceDto(string Reference);

public sealed record AdminQuotationDto(
    long Id, string Reference, string SubjectKind, string OrganisationName, string ContactName, string Email, string? Phone, string PreferredContact,
    string Message, string? QuantityEstimate, string? BudgetRange, long? CustomerId, string Status, long? AssigneeId, string? AssigneeName,
    string? InternalNote, DateTimeOffset SubmittedAt);

public sealed record GetQuotationSubjectsQuery(string Language) : IRequest<Result<List<QuotationSubjectDto>>>;

public sealed record SubmitQuotationRequestCommand(
    string? SubjectKind, string? OrganisationName, string? ContactName, string? Email, string? Phone, string? PreferredContact,
    string? Message, string? QuantityEstimate, string? BudgetRange) : IRequest<Result<QuotationReferenceDto>>;

public sealed record GetAdminQuotationsQuery(
    int Page, int PageSize, string? Search, IReadOnlyList<string>? Statuses, IReadOnlyList<long>? AssigneeIds, bool UnassignedOnly,
    string? SortField, string? SortDir) : IRequest<Result<PagedResult<AdminQuotationDto>>>;

public sealed record GetAdminQuotationQuery(long Id) : IRequest<Result<AdminQuotationDto>>;

public sealed record CountNewQuotationsQuery : IRequest<Result<CountDto>>;

public sealed record AssignQuotationCommand(long Id, long? AssigneeId) : IRequest<Result<AdminQuotationDto>>;

public sealed record SetQuotationStatusCommand(long Id, string Status, string? InternalNote) : IRequest<Result<AdminQuotationDto>>;

public sealed record DeleteQuotationCommand(long Id) : IRequest<Result<Deleted>>;

internal static class QuotationMapper
{
    public static async Task<List<AdminQuotationDto>> MapAsync(IStaffService staff, IReadOnlyList<QuotationRequest> rows, CancellationToken ct)
    {
        var people = (await staff.OptionsAsync(ct)).ToDictionary(p => p.Id, p => p.Name);
        return rows.Select(q => new AdminQuotationDto(
            q.Id, q.Reference, q.SubjectKind, q.OrganisationName, q.ContactName, q.Email, q.Phone, q.PreferredContact, q.Message,
            q.QuantityEstimate, q.BudgetRange, q.CustomerId, q.Status, q.AssigneeId,
            q.AssigneeId is long id ? people.GetValueOrDefault(id) : null, q.InternalNote, q.SubmittedAtUtc)).ToList();
    }
}

public class GetQuotationSubjectsQueryHandler(ILocalizationService localization)
    : IRequestHandler<GetQuotationSubjectsQuery, Result<List<QuotationSubjectDto>>>
{
    public Task<Result<List<QuotationSubjectDto>>> Handle(GetQuotationSubjectsQuery request, CancellationToken ct)
        => Task.FromResult<Result<List<QuotationSubjectDto>>>(
            QuotationSubjects.All.Select(s => new QuotationSubjectDto(s.Kind, localization.GetLocalizedName(s.Ar, s.En, request.Language))).ToList());
}

public class SubmitQuotationRequestCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitQuotationRequestCommand, Result<QuotationReferenceDto>>
{
    public async Task<Result<QuotationReferenceDto>> Handle(SubmitQuotationRequestCommand request, CancellationToken ct)
    {
        // Every request is filed as a guest request: the website does not forward the customer's token today.
        var submitted = QuotationRequest.Submit(
            request.SubjectKind, request.OrganisationName, request.ContactName, request.Email, request.Phone, request.PreferredContact,
            request.Message, request.QuantityEstimate, request.BudgetRange, null);
        if (submitted.IsError)
        {
            return submitted.Errors;
        }

        var quotation = submitted.Value;
        await unitOfWork.GetRepository<QuotationRequest>().AddAsync(quotation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        NotificationWriter.ToStaff(unitOfWork, NotificationKinds.QuotationReceived,
            ("Quotation request received", "طلب عرض سعر جديد"),
            ($"{quotation.OrganisationName} asked for a quotation ({quotation.Reference}).", $"{quotation.OrganisationName} طلب عرض سعر ({quotation.Reference})."),
            $"/quotations/{quotation.Id}");
        await unitOfWork.SaveChangesAsync(ct);
        return new QuotationReferenceDto(quotation.Reference);
    }
}

public class GetAdminQuotationsQueryHandler(IUnitOfWork unitOfWork, IStaffService staff)
    : IRequestHandler<GetAdminQuotationsQuery, Result<PagedResult<AdminQuotationDto>>>
{
    public async Task<Result<PagedResult<AdminQuotationDto>>> Handle(GetAdminQuotationsQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<QuotationRequest>().GetTableNoTracking();

        if (request.Statuses is { Count: > 0 })
        {
            var statuses = request.Statuses;
            query = query.Where(q => statuses.Contains(q.Status));
        }

        if (request.AssigneeIds is { Count: > 0 })
        {
            var assignees = request.AssigneeIds;
            query = query.Where(q => q.AssigneeId != null && assignees.Contains(q.AssigneeId.Value));
        }

        // unassignedOnly=false means no filter.
        if (request.UnassignedOnly)
        {
            query = query.Where(q => q.AssigneeId == null);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(q => q.Id == id || q.OrganisationName.ToLower().Contains(term) || q.ContactName.ToLower().Contains(term) || q.Email.Contains(term));
        }

        var ascending = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = request.SortField?.ToLowerInvariant() switch
        {
            "reference" => ascending ? query.OrderBy(q => q.Id) : query.OrderByDescending(q => q.Id),
            "status" => ascending ? query.OrderBy(q => q.Status) : query.OrderByDescending(q => q.Status),
            "organisationname" => ascending ? query.OrderBy(q => q.OrganisationName) : query.OrderByDescending(q => q.OrganisationName),
            _ => ascending ? query.OrderBy(q => q.SubmittedAtUtc) : query.OrderByDescending(q => q.SubmittedAtUtc)
        };

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
        return new PagedResult<AdminQuotationDto>(await QuotationMapper.MapAsync(staff, page.Items.ToList(), ct), page.Page, page.PageSize, page.TotalCount);
    }
}

public class GetAdminQuotationQueryHandler(IUnitOfWork unitOfWork, IStaffService staff) : IRequestHandler<GetAdminQuotationQuery, Result<AdminQuotationDto>>
{
    public async Task<Result<AdminQuotationDto>> Handle(GetAdminQuotationQuery request, CancellationToken ct)
    {
        var row = await unitOfWork.GetRepository<QuotationRequest>().GetTableNoTracking().FirstOrDefaultAsync(q => q.Id == request.Id, ct);
        return row is null ? QuotationErrors.NotFound : (await QuotationMapper.MapAsync(staff, [row], ct))[0];
    }
}

public class CountNewQuotationsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<CountNewQuotationsQuery, Result<CountDto>>
{
    public async Task<Result<CountDto>> Handle(CountNewQuotationsQuery request, CancellationToken ct)
        => new CountDto(await unitOfWork.GetRepository<QuotationRequest>().GetTableNoTracking().CountAsync(q => q.Status == QuotationStatuses.New, ct));
}

public class AssignQuotationCommandHandler(IUnitOfWork unitOfWork, IStaffService staff) : IRequestHandler<AssignQuotationCommand, Result<AdminQuotationDto>>
{
    public async Task<Result<AdminQuotationDto>> Handle(AssignQuotationCommand request, CancellationToken ct)
    {
        var row = await unitOfWork.GetRepository<QuotationRequest>().GetTableAsTracking().FirstOrDefaultAsync(q => q.Id == request.Id, ct);
        if (row is null)
        {
            return QuotationErrors.NotFound;
        }

        if (request.AssigneeId is long assignee && !(await staff.OptionsAsync(ct)).Any(s => s.Id == assignee))
        {
            return QuotationErrors.AssigneeMissing;
        }

        row.Assign(request.AssigneeId);
        await unitOfWork.SaveChangesAsync(ct);
        return (await QuotationMapper.MapAsync(staff, [row], ct))[0];
    }
}

public class SetQuotationStatusCommandHandler(IUnitOfWork unitOfWork, IStaffService staff) : IRequestHandler<SetQuotationStatusCommand, Result<AdminQuotationDto>>
{
    public async Task<Result<AdminQuotationDto>> Handle(SetQuotationStatusCommand request, CancellationToken ct)
    {
        var row = await unitOfWork.GetRepository<QuotationRequest>().GetTableAsTracking().FirstOrDefaultAsync(q => q.Id == request.Id, ct);
        if (row is null)
        {
            return QuotationErrors.NotFound;
        }

        var set = row.SetStatus(request.Status, request.InternalNote);
        if (set.IsError)
        {
            return set.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await QuotationMapper.MapAsync(staff, [row], ct))[0];
    }
}

public class DeleteQuotationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteQuotationCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteQuotationCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<QuotationRequest>();
        var row = await repository.GetTableAsTracking().FirstOrDefaultAsync(q => q.Id == request.Id, ct);
        if (row is null)
        {
            return QuotationErrors.NotFound;
        }

        repository.DeleteAsync(row);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

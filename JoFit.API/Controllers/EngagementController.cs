using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Enrolments;
using JoFit.Application.Features.Quotations;
using JoFit.Application.Features.Reviews;
using JoFit.Contracts.Requests.Courses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record EnrolRequest(long CourseId);

public sealed record SubmitReviewRequest(string? Type, long Id, int Rating, string? Title, string? Body);

public sealed record ModerationRequest(string Status, string? Note);

public sealed record AssigneeRequest(long? AssigneeId);

public sealed record QuotationStatusRequest(string Status, string? InternalNote);

public sealed record QuotationRequestBody(
    string? SubjectKind, string? OrganisationName, string? ContactName, string? Email, string? Phone, string? PreferredContact,
    string? Message, string? QuantityEstimate, string? BudgetRange);

public sealed record AdminListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public string[]? ModerationStatuses { get; init; }
    public string[]? TargetTypes { get; init; }
    public int? MinRating { get; init; }
    public long? CourseId { get; init; }
    public long? CustomerId { get; init; }
    public long[]? AssigneeIds { get; init; }
    public bool UnassignedOnly { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

// ---- the customer's enrolments and reviews ------------------------------------

[Route("api/v{version:apiVersion}/me")]
[ApiVersion("1.0")]
[Authorize(Roles = "customer")]
public sealed class MeEngagementController(ISender sender, ILocalizationService localization) : ApiController
{
    private string Language => localization.GetCurrentLanguage() ?? "ar";

    [HttpGet("enrolments")]
    [ProducesResponseType(typeof(List<AccountEnrolmentDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The customer's enrolments.")]
    [EndpointName("GetMyEnrolments")]
    public async Task<IActionResult> GetEnrolments(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyEnrolmentsQuery(CurrentUserId, Language), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("enrolments")]
    [ProducesResponseType(typeof(AccountEnrolmentDto), StatusCodes.Status201Created)]
    [EndpointSummary("Reserves a seat on a published course. It starts pending and holds the seat.")]
    [EndpointName("Enrol")]
    public async Task<IActionResult> Enrol([FromBody] EnrolRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new EnrolCommand(CurrentUserId, r.CourseId, Language), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpGet("enrolments/by-course/{courseId:long}")]
    [ProducesResponseType(typeof(AccountEnrolmentDto), StatusCodes.Status200OK)]
    [EndpointSummary("The customer's latest enrolment in a course. Not found when there is none.")]
    [EndpointName("GetMyEnrolmentByCourse")]
    public async Task<IActionResult> GetByCourse(long courseId, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyEnrolmentByCourseQuery(CurrentUserId, courseId, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("reviews")]
    [ProducesResponseType(typeof(List<AccountReviewDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The customer's reviews and where they stand.")]
    [EndpointName("GetMyReviews")]
    public async Task<IActionResult> GetReviews(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyReviewsQuery(CurrentUserId), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("reviewable")]
    [ProducesResponseType(typeof(List<ReviewableDto>), StatusCodes.Status200OK)]
    [EndpointSummary("What the customer can review: completed purchases, returned hires, completed sessions, courses taken.")]
    [EndpointName("GetMyReviewable")]
    public async Task<IActionResult> GetReviewable(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyReviewableQuery(CurrentUserId, Language), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("reviews")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Leaves a review. It is hidden until staff approve it; a target you do not have is forbidden.")]
    [EndpointName("SubmitReview")]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SubmitReviewCommand(CurrentUserId, r.Type, r.Id, r.Rating, r.Title, r.Body), ct);
        return result.Match(_ => StatusCode(StatusCodes.Status201Created), Problem);
    }
}

// ---- admin --------------------------------------------------------------------

[Route("api/v{version:apiVersion}/admin/enrolments")]
[ApiVersion("1.0")]
public sealed class AdminEnrolmentsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Education.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminEnrolmentDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of enrolments.")]
    [EndpointName("GetAdminEnrolments")]
    public async Task<IActionResult> GetPaged([FromQuery] AdminListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminEnrolmentsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.CourseId, r.CustomerId, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(typeof(AdminEnrolmentDto), StatusCodes.Status200OK)]
    [EndpointSummary("Activates, completes or cancels an enrolment. Cancelling needs a note; activating records the course price as paid.")]
    [EndpointName("SetAdminEnrolmentStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] ModerationRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetEnrolmentStatusCommand(id, r.Status, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/reviews")]
[ApiVersion("1.0")]
public sealed class AdminReviewsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Reviews.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminReviewDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of reviews to moderate.")]
    [EndpointName("GetAdminReviews")]
    public async Task<IActionResult> GetPaged([FromQuery] AdminListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminReviewsQuery(r.Page, r.PageSize, r.Search, r.ModerationStatuses, r.TargetTypes, r.MinRating, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("pending-count")]
    [Authorize(Permissions.Reviews.Read)]
    [ProducesResponseType(typeof(CountDto), StatusCodes.Status200OK)]
    [EndpointSummary("How many reviews wait for moderation (for the sidebar badge).")]
    [EndpointName("CountPendingReviews")]
    public async Task<IActionResult> PendingCount(CancellationToken ct)
    {
        var result = await sender.Send(new CountPendingReviewsQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Reviews.Read)]
    [ProducesResponseType(typeof(AdminReviewDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a review.")]
    [EndpointName("GetAdminReview")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminReviewQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/moderation")]
    [Authorize(Permissions.Reviews.Moderate)]
    [ProducesResponseType(typeof(AdminReviewDto), StatusCodes.Status200OK)]
    [EndpointSummary("Approves or rejects a review. Rejecting needs a note; pending can never be set.")]
    [EndpointName("ModerateReview")]
    public async Task<IActionResult> Moderate(long id, [FromBody] ModerationRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new ModerateReviewCommand(id, r.Status, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Reviews.Moderate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a review.")]
    [EndpointName("DeleteReview")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteReviewCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/quotation-requests")]
[ApiVersion("1.0")]
public sealed class AdminQuotationsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Quotations.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminQuotationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of quotation requests.")]
    [EndpointName("GetAdminQuotations")]
    public async Task<IActionResult> GetPaged([FromQuery] AdminListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminQuotationsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.AssigneeIds, r.UnassignedOnly, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("new-count")]
    [Authorize(Permissions.Quotations.Read)]
    [ProducesResponseType(typeof(CountDto), StatusCodes.Status200OK)]
    [EndpointSummary("How many requests are new (for the sidebar badge).")]
    [EndpointName("CountNewQuotations")]
    public async Task<IActionResult> NewCount(CancellationToken ct)
    {
        var result = await sender.Send(new CountNewQuotationsQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Quotations.Read)]
    [ProducesResponseType(typeof(AdminQuotationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a quotation request.")]
    [EndpointName("GetAdminQuotation")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminQuotationQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/assignee")]
    [Authorize(Permissions.Quotations.Write)]
    [ProducesResponseType(typeof(AdminQuotationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Assigns a staff member. Assigning a new request moves it to in-review; clearing it does not move it back.")]
    [EndpointName("AssignQuotation")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssigneeRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new AssignQuotationCommand(id, r.AssigneeId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Quotations.Write)]
    [ProducesResponseType(typeof(AdminQuotationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Sets the status. Lost needs an internal note.")]
    [EndpointName("SetQuotationStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] QuotationStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetQuotationStatusCommand(id, r.Status, r.InternalNote), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Quotations.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a quotation request.")]
    [EndpointName("DeleteQuotation")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteQuotationCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

// ---- public -------------------------------------------------------------------

[Route("api/v{version:apiVersion}")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class PublicEngagementController(ISender sender, ILocalizationService localization) : ApiController
{
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(ReviewCollectionDto), StatusCodes.Status200OK)]
    [EndpointSummary("Approved reviews of a target, newest first, with the rating summary of all of them.")]
    [EndpointName("GetReviews")]
    public async Task<IActionResult> GetReviews([FromQuery] string? targetType, [FromQuery] long? targetId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPublicReviewsQuery(targetType, targetId, page, pageSize), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("reviews/summary")]
    [ProducesResponseType(typeof(RatingSummaryDto), StatusCodes.Status200OK)]
    [EndpointSummary("The average and distribution of approved reviews of a target.")]
    [EndpointName("GetReviewsSummary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? targetType, [FromQuery] long? targetId, CancellationToken ct)
    {
        var result = await sender.Send(new GetRatingSummaryQuery(targetType, targetId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("reviews/recent")]
    [ProducesResponseType(typeof(List<PublicReviewDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The newest approved reviews (default 5).")]
    [EndpointName("GetRecentReviews")]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 5, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetRecentReviewsQuery(limit), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("quotation-subjects")]
    [ProducesResponseType(typeof(List<QuotationSubjectDto>), StatusCodes.Status200OK)]
    [EndpointSummary("What a quotation can be for.")]
    [EndpointName("GetQuotationSubjects")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetSubjects(CancellationToken ct)
    {
        var result = await sender.Send(new GetQuotationSubjectsQuery(localization.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("quotation-requests")]
    [ProducesResponseType(typeof(QuotationReferenceDto), StatusCodes.Status201Created)]
    [EndpointSummary("Files a quotation request as a guest. Answers its reference.")]
    [EndpointName("SubmitQuotationRequest")]
    public async Task<IActionResult> SubmitQuotation([FromBody] QuotationRequestBody r, CancellationToken ct)
    {
        var result = await sender.Send(
            new SubmitQuotationRequestCommand(r.SubjectKind, r.OrganisationName, r.ContactName, r.Email, r.Phone, r.PreferredContact, r.Message, r.QuantityEstimate, r.BudgetRange), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }
}

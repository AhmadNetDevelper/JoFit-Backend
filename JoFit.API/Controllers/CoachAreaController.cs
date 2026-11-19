using Asp.Versioning;
using JoFit.API.Mapping;
using JoFit.API.OpenApi;
using JoFit.Contracts.Requests.Users;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.CoachPortal;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record CoachSessionStatusRequest(string? Status, string? Note);

/// <summary>The coach's own area. The coach always comes from the token; any other role is forbidden.</summary>
[Route("api/v{version:apiVersion}/coach")]
[ApiVersion("1.0")]
[Authorize(Roles = "coach")]
public sealed class CoachAreaController(ISender sender, ILocalizationService localization) : ApiController
{
    private string Language => localization.GetCurrentLanguage() ?? "ar";

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(CoachDashboardDto), StatusCodes.Status200OK)]
    [EndpointSummary("Counts, the next session and the requests waiting for an answer.")]
    [EndpointName("GetCoachDashboard")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachDashboardQuery(CurrentUserId, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(MyCoachProfileDto), StatusCodes.Status200OK)]
    [EndpointSummary("The coach's own profile, in the same shape register-coach accepts.")]
    [EndpointName("GetCoachProfile")]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCoachProfileQuery(CurrentUserId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("profile")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(MyCoachProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates the coach's own profile.")]
    [EndpointDescription("Multipart form with the register-coach field names, without username, email, password and slug. Educations, Certificates, Experiences and Skills replace what is stored; a certificate sent with its Id (from GET) is kept, and keeps its file unless a new CertificateFile is sent. The verification status is not changed.")]
    [EndpointName("UpdateCoachProfile")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateMyCoachProfileRequest request, CancellationToken ct)
    {
        var result = await sender.Send(request.ToCommand(CurrentUserId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<CoachPortalSessionDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The coach's sessions, newest first, with the moves allowed right now.")]
    [EndpointName("GetCoachSessions")]
    public async Task<IActionResult> Sessions(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachSessionsQuery(CurrentUserId), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("sessions/{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Confirms, completes or cancels a session. Completing is allowed only after the start time; cancelling needs a note.")]
    [EndpointName("SetCoachSessionStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] CoachSessionStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetCoachSessionStatusCommand(CurrentUserId, id, r.Status, r.Note), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("courses")]
    [ProducesResponseType(typeof(List<CoachPortalCourseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The courses this coach teaches.")]
    [EndpointName("GetCoachCourses")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Courses(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachCoursesQuery(CurrentUserId, Language), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("reviews")]
    [ProducesResponseType(typeof(List<CoachPortalReviewDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Approved reviews of this coach.")]
    [EndpointName("GetCoachReviews")]
    public async Task<IActionResult> Reviews(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachReviewsQuery(CurrentUserId), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("packages")]
    [ProducesResponseType(typeof(List<CoachPortalPackageDto>), StatusCodes.Status200OK)]
    [EndpointSummary("This coach's packages.")]
    [EndpointName("GetCoachPackages")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Packages(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachPackagesQuery(CurrentUserId, Language), ct);
        return result.Match(items => Ok(items), Problem);
    }
}

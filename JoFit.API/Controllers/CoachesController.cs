using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.API.Mapping;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Coaches;
using JoFit.Application.Features.Users.Commands.CreateCoach;
using JoFit.Application.Features.Users.Commands.CreateUser;
using JoFit.Application.Features.Users.Dtos;
using JoFit.Contracts.Requests.Coaches;
using JoFit.Contracts.Requests.Users;
using JoFit.Domain.Coaches;
using JoFit.Domain.Identity.Users;
using JoFit.Infrastructure.Data.Seeders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/coaches")]
[ApiVersion("1.0")]
public sealed class AdminCoachesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminCoachDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of coaches (both languages).")]
    [EndpointName("GetAdminCoaches")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminCoachesRequest r, CancellationToken ct)
    {
        var statuses = (r.Statuses ?? []).Concat(r.VerificationStatuses ?? []).ToArray();
        var result = await sender.Send(
            new GetAdminCoachesQuery(r.Page, r.PageSize, r.Search, statuses, r.SportIds, r.Specializations, r.LocationIds, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(AdminCoachDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a coach by id.")]
    [EndpointName("GetAdminCoach")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCoachQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a coach.")]
    [EndpointDescription("Creates a user with the Coach role together with their educations, certificates, experiences and skills. Multipart form with these parts: User (login and personal data), Profile (the coach page: slug, name, headline, location, sportIds, languages, packages), Educations[i], Certificates[i] (optional CertificateFile), Experiences[i] and Skills[i]. Everything is saved in one transaction.")]
    [Consumes("multipart/form-data")]
    [EndpointName("CreateAdminCoach")]
    public async Task<IActionResult> Create([FromForm] CreateCoachRequest request, CancellationToken ct)
    {
        var command = request.ToCommand();

        var result = await sender.Send(command, ct);
        return result.Match(
            dto => CreatedAtRoute("GetUserById", new { version = "1.0", userId = dto.UserId }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(typeof(AdminCoachDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates a coach. Send package ids to keep packages that bookings point at.")]
    [EndpointDescription("Updates the coach page and the coach's user together. Multipart form with the same parts as create: User (login and personal data, plus IsActive), Profile (the coach page), Educations[i], Certificates[i] (optional CertificateFile), Experiences[i] and Skills[i]. The education, certificate, experience and skill lists replace what is stored. Everything is saved in one transaction.")]
    [Consumes("multipart/form-data")]
    [EndpointName("UpdateAdminCoach")]
    public async Task<IActionResult> Update(long id, [FromForm] UpdateCoachRequest r, CancellationToken ct)
    {
        var result = await sender.Send(r.ToCommand(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/verification")]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(typeof(AdminCoachDto), StatusCodes.Status200OK)]
    [EndpointSummary("Approves, rejects or suspends a coach. Rejecting or suspending needs a note.")]
    [EndpointName("DecideCoachVerification")]
    public async Task<IActionResult> Decide(long id, [FromBody] CoachStatusDecisionRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new DecideCoachVerificationCommand(id, r.Status, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/certifications/{certificationId:long}")]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(typeof(AdminCoachDto), StatusCodes.Status200OK)]
    [EndpointSummary("Marks a certification as verified or not.")]
    [EndpointName("SetCoachCertificationVerified")]
    public async Task<IActionResult> SetCertification(long id, long certificationId, [FromBody] CertificationVerifiedRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetCoachCertificationVerifiedCommand(id, certificationId, r.IsVerified), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a coach with no pending or confirmed sessions.")]
    [EndpointName("DeleteAdminCoach")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCoachProfileCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/coach-locations")]
[ApiVersion("1.0")]
public sealed class AdminCoachLocationsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(List<LocationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists the locations a coach can be based in.")]
    [EndpointName("GetAdminCoachLocations")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachLocationsQuery(), ct);
        return result.Match(items => Ok(items), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/coach-bookings")]
[ApiVersion("1.0")]
public sealed class AdminCoachBookingsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(PagedResult<CoachBookingDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of coaching sessions.")]
    [EndpointName("GetAdminCoachBookings")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminCoachBookingsRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCoachBookingsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.CoachIds, r.CustomerId, r.UpcomingOnly, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Coaches.Read)]
    [ProducesResponseType(typeof(CoachBookingDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a coaching session.")]
    [EndpointName("GetAdminCoachBooking")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCoachBookingQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Coaches.Write)]
    [ProducesResponseType(typeof(CoachBookingDto), StatusCodes.Status200OK)]
    [EndpointSummary("Confirms, completes or cancels a session. Cancelling needs a note.")]
    [EndpointName("SetCoachBookingStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] CoachBookingStatusRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetCoachBookingStatusCommand(id, r.Status, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

/// <summary>Public storefront reads of coaches. Only approved coaches are listed.</summary>
[Route("api/v{version:apiVersion}/coaches")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class CoachesController(ISender sender, ILocalizationService localization) : ApiController
{
    private static DateOnly? Date(string? value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date) ? date : null;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PublicCoachDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists approved coaches (page size up to 200).")]
    [EndpointName("GetCoaches")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetList([FromQuery] GetPublicCoachesRequest r, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetPublicCoachesQuery(localization.GetCurrentLanguage() ?? "ar", r.Page, r.PageSize, r.Search, r.SportSlugs, r.Specializations, r.LocationIds, r.Languages, r.Sort), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("by-ids")]
    [ProducesResponseType(typeof(List<PublicCoachDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets approved coaches by id, in the order asked.")]
    [EndpointName("GetCoachesByIds")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetByIds([FromQuery] long[]? ids, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCoachesByIdsQuery(ids ?? [], localization.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicCoachDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets an approved coach by slug.")]
    [EndpointName("GetCoachBySlug")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCoachBySlugQuery(slug, localization.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("{id:long}/availability")]
    [ProducesResponseType(typeof(List<CoachSlotDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Free one-hour slots over a range (up to 60 days).")]
    [EndpointName("GetCoachAvailability")]
    public async Task<IActionResult> GetAvailability(long id, [FromQuery] string? from, [FromQuery] string? to, CancellationToken ct)
    {
        var result = await sender.Send(new GetCoachAvailabilityQuery(id, Date(from), Date(to)), ct);
        return result.Match(items => Ok(items), Problem);
    }
}

[Route("api/v{version:apiVersion}/coach-bookings")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class CoachBookingsController(ISender sender) : ApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(CoachBookingDto), StatusCodes.Status201Created)]
    [EndpointSummary("Books a slot. A signed-in customer's booking is filed on their account; a guest can book too.")]
    [EndpointName("CreateCoachBooking")]
    public async Task<IActionResult> Create([FromBody] CreateCoachBookingRequest r, CancellationToken ct)
    {
        long? customerUserId = CurrentAccountRole == "customer" ? CurrentUserId : null;
        var result = await sender.Send(
            new CreateCoachBookingCommand(r.CoachId, r.PackageId, r.SlotId, new CoachBookingContactInput(r.Contact.Name, r.Contact.Email, r.Contact.Phone), r.Goal, customerUserId), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }
}

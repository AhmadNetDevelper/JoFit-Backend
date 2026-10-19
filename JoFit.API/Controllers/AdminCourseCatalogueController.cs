using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Application.Features.Courses.Commands.DeleteCourse;
using JoFit.Contracts.Requests.Courses;
using JoFit.Domain.Courses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Back-office education catalogue. The original skill-lookup endpoints stay on api/v1/course.</summary>
[Route("api/v{version:apiVersion}/admin/courses")]
[ApiVersion("1.0")]
public sealed class AdminCourseCatalogueController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Education.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminCourseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of courses (both languages).")]
    [EndpointName("GetAdminCourses")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAdminCoursesRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetAdminCoursesQuery(request.Page, request.PageSize, request.Search, request.Statuses, request.CategoryIds, request.SortField, request.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Education.Read)]
    [ProducesResponseType(typeof(AdminCourseDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a course by id.")]
    [EndpointName("GetAdminCourse")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCourseQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(typeof(AdminCourseDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a course. It always starts as a draft.")]
    [EndpointName("CreateAdminCourse")]
    public async Task<IActionResult> Create([FromBody] CourseDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateCourseDraftCommand(ToInput(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(typeof(AdminCourseDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a course.")]
    [EndpointName("UpdateAdminCourse")]
    public async Task<IActionResult> Update(long id, [FromBody] CourseDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCourseDetailsCommand(id, ToInput(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("{id:long}/status")]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(typeof(AdminCourseDto), StatusCodes.Status200OK)]
    [EndpointSummary("Publishes, archives or returns a course to draft.")]
    [EndpointName("SetAdminCourseStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetCourseStatusCommand(id, request.Status), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a course that has no pending or active enrolments.")]
    [EndpointName("DeleteAdminCourse")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCourseCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    internal static CourseDraftInput ToInput(CourseDraftRequest r)
    {
        DateOnly? startsOn = DateOnly.TryParseExact(r.NextIntake?.StartsOn, "yyyy-MM-dd", out var date) ? date : null;
        var certification = r.Certification is null
            ? null
            : new CourseCertification(r.Certification.Name.En, r.Certification.Name.Ar, r.Certification.IssuedBy.En, r.Certification.IssuedBy.Ar);

        var details = new CourseDetails(
            r.Slug,
            r.Title.En, r.Title.Ar,
            r.Summary.En, r.Summary.Ar,
            r.Description?.En, r.Description?.Ar,
            r.Format, r.Level, r.DurationHours, r.ModuleCount,
            r.Price.Amount, r.CompareAtPrice?.Amount,
            r.Languages,
            r.Venue?.En, r.Venue?.Ar,
            certification,
            startsOn, r.NextIntake?.SeatsTotal);

        return new CourseDraftInput(
            details,
            r.CategoryIds,
            r.Media?.Select(m => new GalleryItem(m.Id, m.Alt)).ToList(),
            r.InstructorIds,
            r.SportIds);
    }
}

/// <summary>Public storefront reads of the education catalogue.</summary>
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class CourseCatalogueController(ISender sender, JoFit.Application.Common.Interfaces.ILocalizationService localization) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PublicCourseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists published courses with filters, sorting and paging (page size up to 200).")]
    [EndpointName("GetCourses")]
    [JoFit.API.OpenApi.LocalizedEndpoint]
    public async Task<IActionResult> GetList([FromQuery] GetPublicCoursesRequest request, CancellationToken ct)
    {
        long? Number(string key) => long.TryParse(Request.Query[key].FirstOrDefault(), out var n) ? n : null;
        var result = await sender.Send(
            new GetPublicCoursesQuery(
                localization.GetCurrentLanguage() ?? "ar", request.Page, request.PageSize, request.Search,
                request.CategorySlugs, request.Formats, request.Levels, request.Languages,
                Number("price.min"), Number("price.max"), request.Sort), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicCourseDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a published course by slug.")]
    [EndpointName("GetCourseBySlug")]
    [JoFit.API.OpenApi.LocalizedEndpoint]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCourseBySlugQuery(slug, localization.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

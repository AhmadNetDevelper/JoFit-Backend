using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.CourseCategories.Commands;
using JoFit.Application.Features.CourseCategories.Dtos;
using JoFit.Application.Features.CourseCategories.Queries;
using JoFit.Contracts.Requests.CourseCategories;
using JoFit.Domain.CourseCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/course-categories")]
[ApiVersion("1.0")]
public sealed class AdminCourseCategoriesController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Education.Read)]
    [ProducesResponseType(typeof(PagedResult<CourseCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of course categorys (both languages).")]
    [EndpointName("GetAdminCourseCategories")]
    public async Task<IActionResult> GetPaged([FromQuery] GetCourseCategoriesPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetCourseCategoriesPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Education.Read)]
    [ProducesResponseType(typeof(List<CourseCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every course category for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminCourseCategoryOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseCategoryOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Education.Read)]
    [ProducesResponseType(typeof(CourseCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a course category by id.")]
    [EndpointName("GetAdminCourseCategoryById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCourseCategoryByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(typeof(CourseCategoryDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a course category.")]
    [EndpointName("CreateCourseCategory")]
    public async Task<IActionResult> Create([FromBody] CourseCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateCourseCategoryCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(typeof(CourseCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a course category.")]
    [EndpointName("UpdateCourseCategory")]
    public async Task<IActionResult> Update(long id, [FromBody] CourseCategoryDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateCourseCategoryCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Education.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a course category that nothing uses.")]
    [EndpointName("DeleteCourseCategory")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCourseCategoryCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static CourseCategoryValues ToValues(CourseCategoryDraftRequest r)
        => new(r.Slug, r.Name.En, r.Name.Ar, r.Description?.En, r.Description?.Ar);
}

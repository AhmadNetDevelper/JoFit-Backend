using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.JobTypes.Commands.CreateJobType;
using JoFit.Application.Features.JobTypes.Commands.DeleteJobType;
using JoFit.Application.Features.JobTypes.Commands.UpdateJobType;
using JoFit.Application.Features.JobTypes.Dtos;
using JoFit.Application.Features.JobTypes.Queries.GetAllJobTypes;
using JoFit.Application.Features.JobTypes.Queries.GetJobTypeById;
using JoFit.Application.Features.JobTypes.Queries.GetJobTypesPaged;
using JoFit.Contracts.Requests.JobTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class JobTypeController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.JobTypes.Read)]
    [ProducesResponseType(typeof(PagedResult<JobTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a paged list of job types.")]
    [EndpointDescription("Supports optional search on the Arabic/English name and filtering by active status.")]
    [EndpointName("GetJobTypesPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetJobTypesPagedRequest request, CancellationToken ct)
    {
        var query = new GetJobTypesPagedQuery(request.Page, request.PageSize, request.Search, request.IsActive);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("all")]
    [Authorize(Permissions.JobTypes.Read)]
    [ProducesResponseType(typeof(List<JobTypeLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets all active job types (Id and localized Name).")]
    [EndpointDescription("Name is returned in Arabic (ar) or English (en) based on the `language` query parameter (e.g. ?language=en), then the Language claim or Accept-Language header, defaulting to Arabic.")]
    [EndpointName("GetAllJobTypes")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var language = localizationService.GetCurrentLanguage();
        var result = await sender.Send(new GetAllJobTypesQuery(language), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.JobTypes.Read)]
    [ProducesResponseType(typeof(JobTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a job type by id.")]
    [EndpointName("GetJobTypeById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetJobTypeByIdQuery(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.JobTypes.Create)]
    [ProducesResponseType(typeof(JobTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new job type.")]
    [EndpointName("CreateJobType")]
    public async Task<IActionResult> Create([FromBody] CreateJobTypeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateJobTypeCommand(request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(
            dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.JobTypes.Update)]
    [ProducesResponseType(typeof(JobTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates an existing job type.")]
    [EndpointName("UpdateJobType")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJobTypeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateJobTypeCommand(id, request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.JobTypes.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Deletes a job type (soft delete).")]
    [EndpointName("DeleteJobType")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteJobTypeCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

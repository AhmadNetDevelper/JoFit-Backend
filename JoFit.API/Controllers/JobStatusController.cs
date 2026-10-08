using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.JobStatuses.Commands.CreateJobStatus;
using JoFit.Application.Features.JobStatuses.Commands.DeleteJobStatus;
using JoFit.Application.Features.JobStatuses.Commands.UpdateJobStatus;
using JoFit.Application.Features.JobStatuses.Dtos;
using JoFit.Application.Features.JobStatuses.Queries.GetAllJobStatuses;
using JoFit.Application.Features.JobStatuses.Queries.GetJobStatusById;
using JoFit.Application.Features.JobStatuses.Queries.GetJobStatusesPaged;
using JoFit.Contracts.Requests.JobStatuses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class JobStatusController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.JobStatuses.Read)]
    [ProducesResponseType(typeof(PagedResult<JobStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a paged list of job statuses.")]
    [EndpointDescription("Supports optional search on the Arabic/English name and filtering by active status.")]
    [EndpointName("GetJobStatusesPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetJobStatusesPagedRequest request, CancellationToken ct)
    {
        var query = new GetJobStatusesPagedQuery(request.Page, request.PageSize, request.Search, request.IsActive);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("all")]
    [Authorize(Permissions.JobStatuses.Read)]
    [ProducesResponseType(typeof(List<JobStatusLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets all active job statuses (Id and localized Name).")]
    [EndpointDescription("Name is returned in Arabic (ar) or English (en) based on the `language` query parameter (e.g. ?language=en), then the Language claim or Accept-Language header, defaulting to Arabic.")]
    [EndpointName("GetAllJobStatuses")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var language = localizationService.GetCurrentLanguage();
        var result = await sender.Send(new GetAllJobStatusesQuery(language), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.JobStatuses.Read)]
    [ProducesResponseType(typeof(JobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a job status by id.")]
    [EndpointName("GetJobStatusById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetJobStatusByIdQuery(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.JobStatuses.Create)]
    [ProducesResponseType(typeof(JobStatusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new job status.")]
    [EndpointName("CreateJobStatus")]
    public async Task<IActionResult> Create([FromBody] CreateJobStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateJobStatusCommand(request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(
            dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.JobStatuses.Update)]
    [ProducesResponseType(typeof(JobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates an existing job status.")]
    [EndpointName("UpdateJobStatus")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJobStatusRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateJobStatusCommand(id, request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.JobStatuses.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Deletes a job status (soft delete).")]
    [EndpointName("DeleteJobStatus")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteJobStatusCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

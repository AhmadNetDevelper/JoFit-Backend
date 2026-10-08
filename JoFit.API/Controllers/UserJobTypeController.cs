using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.UserJobTypes.Commands.CreateUserJobType;
using JoFit.Application.Features.UserJobTypes.Commands.DeleteUserJobType;
using JoFit.Application.Features.UserJobTypes.Commands.UpdateUserJobType;
using JoFit.Application.Features.UserJobTypes.Dtos;
using JoFit.Application.Features.UserJobTypes.Queries.GetAllUserJobTypes;
using JoFit.Application.Features.UserJobTypes.Queries.GetUserJobTypeById;
using JoFit.Application.Features.UserJobTypes.Queries.GetUserJobTypesPaged;
using JoFit.Contracts.Requests.UserJobTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[JoFit.API.OpenApi.SharedApi]
public sealed class UserJobTypeController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.UserJobTypes.Read)]
    [ProducesResponseType(typeof(PagedResult<UserJobTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a paged list of user job types.")]
    [EndpointDescription("Supports optional search on the Arabic/English name and filtering by active status.")]
    [EndpointName("GetUserJobTypesPaged")]
    public async Task<IActionResult> GetPaged([FromQuery] GetUserJobTypesPagedRequest request, CancellationToken ct)
    {
        var query = new GetUserJobTypesPagedQuery(request.Page, request.PageSize, request.Search, request.IsActive);
        var result = await sender.Send(query, ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("all")]
    [Authorize(Permissions.UserJobTypes.Read)]
    [ProducesResponseType(typeof(List<UserJobTypeLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets all active user job types (Id and localized Name).")]
    [EndpointDescription("Name is returned in Arabic (ar) or English (en) based on the `language` query parameter (e.g. ?language=en), then the Language claim or Accept-Language header, defaulting to Arabic.")]
    [EndpointName("GetAllUserJobTypes")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var language = localizationService.GetCurrentLanguage();
        var result = await sender.Send(new GetAllUserJobTypesQuery(language), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.UserJobTypes.Read)]
    [ProducesResponseType(typeof(UserJobTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Gets a user job type by id.")]
    [EndpointName("GetUserJobTypeById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserJobTypeByIdQuery(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.UserJobTypes.Create)]
    [ProducesResponseType(typeof(UserJobTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new user job type.")]
    [EndpointName("CreateUserJobType")]
    public async Task<IActionResult> Create([FromBody] CreateUserJobTypeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateUserJobTypeCommand(request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(
            dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto),
            Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.UserJobTypes.Update)]
    [ProducesResponseType(typeof(UserJobTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Updates an existing user job type.")]
    [EndpointName("UpdateUserJobType")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserJobTypeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateUserJobTypeCommand(id, request.NameAr, request.NameEn, request.IsActive), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.UserJobTypes.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Deletes a user job type (soft delete).")]
    [EndpointName("DeleteUserJobType")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteUserJobTypeCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

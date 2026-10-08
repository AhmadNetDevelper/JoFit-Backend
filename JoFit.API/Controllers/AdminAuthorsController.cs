using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Authors.Commands;
using JoFit.Application.Features.Authors.Dtos;
using JoFit.Application.Features.Authors.Queries;
using JoFit.Contracts.Requests.Authors;
using JoFit.Domain.Authors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/authors")]
[ApiVersion("1.0")]
public sealed class AdminAuthorsController(ISender sender) : ApiController
{
    [HttpGet]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(PagedResult<AuthorDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a page of authors (both languages).")]
    [EndpointName("GetAdminAuthors")]
    public async Task<IActionResult> GetPaged([FromQuery] GetAuthorsPagedRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetAuthorsPagedQuery(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, request.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("options")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(List<AuthorDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Gets every author for pickers, optionally limited to ids.")]
    [EndpointName("GetAdminAuthorOptions")]
    public async Task<IActionResult> GetOptions([FromQuery] long[]? ids, [FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetAuthorOptionsQuery(ids, locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("{id:long}")]
    [Authorize(Permissions.Content.Read)]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a author by id.")]
    [EndpointName("GetAdminAuthorById")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAuthorByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a author.")]
    [EndpointName("CreateAuthor")]
    public async Task<IActionResult> Create([FromBody] AuthorDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateAuthorCommand(ToValues(request)), ct);
        return result.Match(dto => CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto), Problem);
    }

    [HttpPut("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a author.")]
    [EndpointName("UpdateAuthor")]
    public async Task<IActionResult> Update(long id, [FromBody] AuthorDraftRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateAuthorCommand(id, ToValues(request)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Permissions.Content.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a author that nothing uses.")]
    [EndpointName("DeleteAuthor")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteAuthorCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    private static AuthorValues ToValues(AuthorDraftRequest r)
        => new(r.Slug, r.Name, r.Role?.En, r.Role?.Ar, r.Bio?.En, r.Bio?.Ar, r.Avatar?.Id, r.Avatar?.Url, r.Avatar?.Alt);
}

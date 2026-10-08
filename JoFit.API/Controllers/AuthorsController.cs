using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Authors.Dtos;
using JoFit.Application.Features.Authors.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/authors")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class AuthorsController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(PublicAuthorDto), StatusCodes.Status200OK)]
    [EndpointSummary("Gets a author.")]
    [EndpointName("GetAuthor")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetOne(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicAuthorQuery(id, localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

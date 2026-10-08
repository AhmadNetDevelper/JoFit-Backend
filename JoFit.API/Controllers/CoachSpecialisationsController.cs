using Asp.Versioning;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.CoachSpecialisations.Dtos;
using JoFit.Application.Features.CoachSpecialisations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

/// <summary>Public storefront reads. Text is resolved to the requested language.</summary>
[Route("api/v{version:apiVersion}/coach-specialisations")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class CoachSpecialisationsController(ISender sender, ILocalizationService localizationService) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PublicCoachSpecialisationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Lists coach specialisations.")]
    [EndpointName("GetCoachSpecialisations")]
    [LocalizedEndpoint]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCoachSpecialisationsQuery(localizationService.GetCurrentLanguage() ?? "ar"), ct);
        return result.Match(items => Ok(items), Problem);
    }
}

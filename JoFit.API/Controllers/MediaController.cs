using Asp.Versioning;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Media.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[Route("api/v{version:apiVersion}/admin/media")]
[ApiVersion("1.0")]
[Authorize]
public sealed class AdminMediaController(ISender sender) : ApiController
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(MediaImageDto), StatusCodes.Status201Created)]
    [EndpointSummary("Uploads an image and returns it as a MediaImage to save on a record.")]
    [EndpointDescription("multipart/form-data: file (required), purpose (required), alt (optional).")]
    [EndpointName("UploadMedia")]
    public async Task<IActionResult> Upload([FromForm] UploadMediaForm form, CancellationToken ct)
    {
        var bytes = await MediaForm.ReadAsync(form.File, ct);
        var result = await sender.Send(new UploadMediaCommand(bytes, form.Purpose, form.Alt, MediaUploadKind.Admin), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes an uploaded image.")]
    [EndpointName("DeleteMedia")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteMediaCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

/// <summary>Public, cacheable image bytes. File names are unique per upload, so a URL never changes content.</summary>
[Route("media")]
[ApiController]
[AllowAnonymous]
public sealed class MediaFilesController(IMediaStorage storage) : ControllerBase
{
    [HttpGet("{file}")]
    [EndpointName("GetMediaFile")]
    public IActionResult Get(string file)
    {
        var media = storage.Open(file);
        if (media is null)
        {
            return NotFound();
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        return File(media.Content, media.MimeType);
    }
}

public sealed class UploadMediaForm
{
    public IFormFile? File { get; init; }
    public string? Purpose { get; init; }
    public string? Alt { get; init; }
}

internal static class MediaForm
{
    public static async Task<byte[]?> ReadAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}

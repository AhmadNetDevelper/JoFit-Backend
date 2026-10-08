using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Community;
using JoFit.Application.Features.Reviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

public sealed record GetCommunityPostsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? TopicSlugs { get; init; }
}

public sealed record GetAdminCommunityPostsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public long[]? TopicIds { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
}

public sealed record CommunityModerationRequest(string Status, string? Note);

/// <summary>Public community reads. Hidden posts and comments are not shown.</summary>
[Route("api/v{version:apiVersion}/community")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class CommunityController(ISender sender, ILocalizationService localization) : ApiController
{
    private string Language => localization.GetCurrentLanguage() ?? "ar";

    [HttpGet("topics")]
    [ProducesResponseType(typeof(List<TopicDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Discussion topics.")]
    [EndpointName("GetCommunityTopics")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Topics(CancellationToken ct)
    {
        var result = await sender.Send(new GetCommunityTopicsQuery(Language), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("posts")]
    [ProducesResponseType(typeof(PagedResult<CommunityPostDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Posts, newest first (page size up to 200).")]
    [EndpointName("GetCommunityPosts")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Posts([FromQuery] GetCommunityPostsRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetCommunityPostsQuery(Language, r.Page, r.PageSize, r.Search, r.TopicSlugs), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("posts/{slug}")]
    [ProducesResponseType(typeof(CommunityPostDto), StatusCodes.Status200OK)]
    [EndpointSummary("A post with its body. A hidden post is not found.")]
    [EndpointName("GetCommunityPost")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Post(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetCommunityPostBySlugQuery(slug, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("posts/{id:long}/comments")]
    [ProducesResponseType(typeof(List<CommunityCommentDto>), StatusCodes.Status200OK)]
    [EndpointSummary("A post's comments, oldest first.")]
    [EndpointName("GetCommunityComments")]
    public async Task<IActionResult> Comments(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetCommunityCommentsQuery(id), ct);
        return result.Match(items => Ok(items), Problem);
    }
}

[Route("api/v{version:apiVersion}/admin/community")]
[ApiVersion("1.0")]
[Authorize(Permissions.Community.Moderate)]
public sealed class AdminCommunityController(ISender sender) : ApiController
{
    [HttpGet("posts")]
    [ProducesResponseType(typeof(PagedResult<AdminCommunityPostDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Posts to moderate.")]
    [EndpointName("GetAdminCommunityPosts")]
    public async Task<IActionResult> Posts([FromQuery] GetAdminCommunityPostsRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCommunityPostsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.TopicIds, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("flagged-count")]
    [ProducesResponseType(typeof(CountDto), StatusCodes.Status200OK)]
    [EndpointSummary("Flagged posts plus flagged comments (for the sidebar badge).")]
    [EndpointName("CountFlaggedCommunity")]
    public async Task<IActionResult> FlaggedCount(CancellationToken ct)
    {
        var result = await sender.Send(new CountFlaggedCommunityQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("posts/{id:long}")]
    [ProducesResponseType(typeof(AdminCommunityPostDto), StatusCodes.Status200OK)]
    [EndpointSummary("A post.")]
    [EndpointName("GetAdminCommunityPost")]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCommunityPostQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("posts/{id:long}/comments")]
    [ProducesResponseType(typeof(PagedResult<AdminCommunityCommentDto>), StatusCodes.Status200OK)]
    [EndpointSummary("A post's comments, oldest first. An unknown post answers an empty page.")]
    [EndpointName("GetAdminCommunityComments")]
    public async Task<IActionResult> Comments(long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAdminCommunityCommentsQuery(id, page, pageSize), ct);
        return result.Match(p => Ok(new { p.Items, p.Meta }), Problem);
    }

    [HttpGet("topics")]
    [ProducesResponseType(typeof(List<AdminTopicDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Topics in both languages.")]
    [EndpointName("GetAdminCommunityTopics")]
    public async Task<IActionResult> Topics(CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminCommunityTopicsQuery(), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPut("posts/{id:long}/moderation")]
    [ProducesResponseType(typeof(AdminCommunityPostDto), StatusCodes.Status200OK)]
    [EndpointSummary("Hides a post (a note is required) or makes it visible again (reports reset to 0).")]
    [EndpointName("ModerateCommunityPost")]
    public async Task<IActionResult> ModeratePost(long id, [FromBody] CommunityModerationRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new ModeratePostCommand(id, r.Status, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("comments/{id:long}/moderation")]
    [ProducesResponseType(typeof(AdminCommunityCommentDto), StatusCodes.Status200OK)]
    [EndpointSummary("Hides a comment (a note is required) or makes it visible again.")]
    [EndpointName("ModerateCommunityComment")]
    public async Task<IActionResult> ModerateComment(long id, [FromBody] CommunityModerationRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new ModerateCommentCommand(id, r.Status, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("posts/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a post and its comments.")]
    [EndpointName("DeleteCommunityPost")]
    public async Task<IActionResult> DeletePost(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeletePostCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("comments/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a comment and its replies.")]
    [EndpointName("DeleteCommunityComment")]
    public async Task<IActionResult> DeleteComment(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCommentCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}

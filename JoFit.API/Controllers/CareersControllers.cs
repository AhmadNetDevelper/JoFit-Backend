using System.Text.Json;
using Asp.Versioning;
using JoFit.API.Authorization;
using JoFit.API.OpenApi;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Auth;
using JoFit.Application.Features.Careers;
using JoFit.Application.Features.Media.Commands;
using JoFit.Application.Features.Reviews;
using JoFit.Contracts.Requests.Common;
using JoFit.Contracts.Requests.Courses;
using JoFit.Domain.Careers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JoFit.API.Controllers;

// ---- request bodies ---------------------------------------------------------------------------

public sealed record RegisterEmployerRequest
{
    public string CompanyName { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string CityId { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Website { get; init; }
    public string Password { get; init; } = string.Empty;
    public string? Locale { get; init; }
}

public sealed record EmployerProfileRequest
{
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string CityId { get; init; } = string.Empty;
    public string? About { get; init; }
    public string? Website { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string? Size { get; init; }

    /// <summary>Absent keeps the logo, null removes it, an image (uploaded first) replaces it.</summary>
    public JsonElement? Logo { get; init; }
}

public sealed record SalaryRequest
{
    public MoneyRequest? Min { get; init; }
    public MoneyRequest? Max { get; init; }
    public string Period { get; init; } = "month";
}

public sealed record JobDraftRequest
{
    public string Title { get; init; } = string.Empty;
    public long CategoryId { get; init; }
    public List<long> SportIds { get; init; } = [];
    public string CityId { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public string WorkMode { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public SalaryRequest? Salary { get; init; }
    public bool ShowSalary { get; init; }
    public string Description { get; init; } = string.Empty;
    public List<string> Requirements { get; init; } = [];
    public List<string> Benefits { get; init; } = [];
    public List<string> Languages { get; init; } = [];
    public int Positions { get; init; } = 1;

    /// <summary>yyyy-MM-dd. Send it omitted, never null, for "no closing date".</summary>
    public string? ClosesOn { get; init; }

    /// <summary>On create: submit for review straight away.</summary>
    public bool Submit { get; init; }
}

public sealed record ApplicationRequest
{
    public long JobId { get; init; }
    public long CvId { get; init; }
    public string? CoverLetter { get; init; }
    public int YearsExperience { get; init; }
    public string? Certifications { get; init; }
    public string? AvailableFrom { get; init; }
    public MoneyRequest? ExpectedSalary { get; init; }
}

public sealed record ReasonRequest(string? Reason);

public sealed record StatusNoteRequest(string? Status, string? Note);

public sealed record NoteRequest(string? Note);

public sealed record DecisionRequest(string? Decision, string? Note);

public sealed record FeaturedRequest(bool Featured);

public sealed record UploadFileForm
{
    public IFormFile? File { get; init; }
    public string? Purpose { get; init; }
}

public sealed record CareersListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? Statuses { get; init; }
    public string[]? VerificationStatuses { get; init; }
    public long[]? EmployerIds { get; init; }
    public long[]? CategoryIds { get; init; }
    public long[]? JobIds { get; init; }
    public long? JobId { get; init; }
    public long? CustomerId { get; init; }
    public string? SortField { get; init; }
    public string? SortDir { get; init; }
    public string? Sort { get; init; }
    public string? Locale { get; init; }
}

public sealed record JobCategoryDraftRequest
{
    public string? Slug { get; init; }
    public LocalizedTextRequest Name { get; init; } = new();
}

public sealed record PublicJobsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string[]? CategorySlugs { get; init; }
    public string[]? CityIds { get; init; }
    public string[]? EmploymentTypes { get; init; }
    public string[]? WorkModes { get; init; }
    public string[]? ExperienceLevels { get; init; }
    public string[]? SportSlugs { get; init; }
    public string? EmployerSlug { get; init; }
    public string? Sort { get; init; }
}

internal static class CareersMapping
{
    public static DateOnly? Date(string? value) => DateOnly.TryParseExact(value, "yyyy-MM-dd", out var d) ? d : null;

    public static JobInput ToInput(JobDraftRequest r)
    {
        var salary = r.Salary is null ? null : new SalaryInput(r.Salary.Min?.Amount, r.Salary.Max?.Amount, r.Salary.Period);
        return new JobInput(
            new JobDetailsInput(r.Title, r.CategoryId, r.EmploymentType, r.WorkMode, r.ExperienceLevel, salary, r.ShowSalary, r.Description,
                r.Requirements, r.Benefits, r.Languages, r.Positions, Date(r.ClosesOn)),
            r.SportIds, r.CityId);
    }

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

    /// <summary>A private file: served as an attachment, never cached.</summary>
    public static IActionResult Download(ControllerBase controller, PrivateFile file)
    {
        controller.Response.Headers.CacheControl = "private, no-store";
        return controller.File(file.Content, file.MimeType, file.FileName);
    }
}

// ---- employer sign-up -----------------------------------------------------------------------------

[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public sealed class EmployerAuthController(ISender sender) : ApiController
{
    [HttpPost("register-employer")]
    [AllowAnonymous]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(SignedInDto), StatusCodes.Status201Created)]
    [EndpointSummary("Opens an employer account and its company (pending verification) and signs it in.")]
    [EndpointName("RegisterEmployer")]
    public async Task<IActionResult> Register([FromBody] RegisterEmployerRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterEmployerCommand(r.CompanyName, r.Kind, r.CityId, r.ContactName, r.Email, r.Phone, r.Website, r.Password, r.Locale), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }
}

// ---- the public board -----------------------------------------------------------------------------

[Route("api/v{version:apiVersion}/careers")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class CareersController(ISender sender, ILocalizationService localization) : ApiController
{
    private string Language => localization.GetCurrentLanguage() ?? "ar";

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(JobListResultDto), StatusCodes.Status200OK)]
    [EndpointSummary("The job board: open jobs of verified employers, with filters, sort (newest, closing-soon, salary-desc) and facets.")]
    [EndpointName("GetCareersJobs")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Jobs([FromQuery] PublicJobsRequest r, CancellationToken ct)
    {
        var filters = new JobFilters(r.CategorySlugs, r.CityIds, r.EmploymentTypes, r.WorkModes, r.ExperienceLevels, r.SportSlugs, r.EmployerSlug, r.Search);
        var result = await sender.Send(new GetPublicJobsQuery(Language, r.Page, r.PageSize, filters, r.Sort), ct);
        return result.Match(dto => Ok(new { dto.Items, dto.Meta, dto.Facets }), Problem);
    }

    [HttpGet("jobs/{slug}")]
    [ProducesResponseType(typeof(JobPostingDto), StatusCodes.Status200OK)]
    [EndpointSummary("A job. A closed job of a verified employer stays readable; drafts and rejections never are.")]
    [EndpointName("GetCareersJob")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Job(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicJobBySlugQuery(slug, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<PublicJobCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Job categories with their open job counts.")]
    [EndpointName("GetCareersCategories")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicJobCategoriesQuery(Language), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("cities")]
    [ProducesResponseType(typeof(List<CityDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The 12 cities jobs can be in.")]
    [EndpointName("GetCareersCities")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Cities(CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicCitiesQuery(Language), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("employers")]
    [ProducesResponseType(typeof(PagedResult<EmployerSummaryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Verified employers, most open jobs first.")]
    [EndpointName("GetCareersEmployers")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Employers([FromQuery] CareersListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicEmployersQuery(Language, r.Page, r.PageSize, r.Search, r.Sort), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("employers/{slug}")]
    [ProducesResponseType(typeof(EmployerProfileDto), StatusCodes.Status200OK)]
    [EndpointSummary("A verified employer with its open jobs.")]
    [EndpointName("GetCareersEmployer")]
    [LocalizedEndpoint]
    public async Task<IActionResult> Employer(string slug, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicEmployerBySlugQuery(slug, Language), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(CareersStatsDto), StatusCodes.Status200OK)]
    [EndpointSummary("Headline numbers for the careers home page.")]
    [EndpointName("GetCareersStats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var result = await sender.Send(new GetCareersStatsQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

// ---- the employer portal --------------------------------------------------------------------------

[Route("api/v{version:apiVersion}/employer")]
[ApiVersion("1.0")]
[Authorize(Roles = "employer")]
public sealed class EmployerAreaController(ISender sender) : ApiController
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(EmployerDashboardDto), StatusCodes.Status200OK)]
    [EndpointSummary("Counts (drafts include rejected jobs), verification and the newest applications.")]
    [EndpointName("GetEmployerDashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployerDashboardQuery(CurrentUserId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(EmployerAccountProfileDto), StatusCodes.Status200OK)]
    [EndpointSummary("The employer's company profile.")]
    [EndpointName("GetEmployerProfile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployerProfileQuery(CurrentUserId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(EmployerAccountProfileDto), StatusCodes.Status200OK)]
    [EndpointSummary("Saves the company profile. The logo is uploaded first, then saved here.")]
    [EndpointName("UpdateEmployerProfile")]
    public async Task<IActionResult> UpdateProfile([FromBody] EmployerProfileRequest r, CancellationToken ct)
    {
        EmployerLogoChange logo;
        if (r.Logo is JsonElement element)
        {
            logo = element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var id)
                ? new EmployerLogoChange(false, id.GetString(), element.TryGetProperty("alt", out var alt) ? alt.GetString() : null)
                : new EmployerLogoChange(false, null, null);
        }
        else
        {
            logo = new EmployerLogoChange(true, null, null);
        }

        var result = await sender.Send(new UpdateEmployerProfileCommand(CurrentUserId, r.Name, r.Kind, r.CityId, r.About, r.Website, r.Email, r.Phone, r.ContactName, r.Size, logo), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("media")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(4 * 1024 * 1024)]
    [ProducesResponseType(typeof(MediaImageDto), StatusCodes.Status201Created)]
    [EndpointSummary("Uploads the company logo (PNG, JPEG or WebP, up to 2 MB, purpose=employer-logo).")]
    [EndpointName("UploadEmployerLogo")]
    public async Task<IActionResult> UploadLogo([FromForm] UploadFileForm form, CancellationToken ct)
    {
        var bytes = await CareersMapping.ReadAsync(form.File, ct);
        var result = await sender.Send(new UploadMediaCommand(bytes, form.Purpose, null, MediaUploadKind.EmployerLogo), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(PagedResult<EmployerJobDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The employer's jobs, latest change first. Filter by statuses and search title or reference.")]
    [EndpointName("GetEmployerJobs")]
    public async Task<IActionResult> Jobs([FromQuery] CareersListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployerJobsQuery(CurrentUserId, r.Page, r.PageSize, r.Statuses, r.Search), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpPost("jobs")]
    [ProducesResponseType(typeof(EmployerJobDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a job draft, or submits it straight away with submit: true.")]
    [EndpointName("CreateEmployerJob")]
    public async Task<IActionResult> CreateJob([FromBody] JobDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new CreateEmployerJobCommand(CurrentUserId, CareersMapping.ToInput(r), r.Submit), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpGet("jobs/{id:long}")]
    [ProducesResponseType(typeof(EmployerJobDto), StatusCodes.Status200OK)]
    [EndpointSummary("One of the employer's jobs. Someone else's is not found.")]
    [EndpointName("GetEmployerJob")]
    public async Task<IActionResult> GetJob(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployerJobQuery(CurrentUserId, id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("jobs/{id:long}")]
    [ProducesResponseType(typeof(EmployerJobDto), StatusCodes.Status200OK)]
    [EndpointSummary("Edits a job. An open job goes back to review.")]
    [EndpointName("UpdateEmployerJob")]
    public async Task<IActionResult> UpdateJob(long id, [FromBody] JobDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateEmployerJobCommand(CurrentUserId, id, CareersMapping.ToInput(r)), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("jobs/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a job nobody has applied to; otherwise close it.")]
    [EndpointName("DeleteEmployerJob")]
    public async Task<IActionResult> DeleteJob(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteEmployerJobCommand(CurrentUserId, id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("jobs/{id:long}/submit")]
    [ProducesResponseType(typeof(EmployerJobDto), StatusCodes.Status201Created)]
    [EndpointSummary("Sends a draft or rejected job for review.")]
    [EndpointName("SubmitEmployerJob")]
    public Task<IActionResult> Submit(long id, CancellationToken ct) => Act(id, "submit", ct);

    [HttpPost("jobs/{id:long}/withdraw")]
    [ProducesResponseType(typeof(EmployerJobDto), StatusCodes.Status201Created)]
    [EndpointSummary("Takes a pending job back to draft.")]
    [EndpointName("WithdrawEmployerJob")]
    public Task<IActionResult> Withdraw(long id, CancellationToken ct) => Act(id, "withdraw", ct);

    [HttpPost("jobs/{id:long}/close")]
    [ProducesResponseType(typeof(EmployerJobDto), StatusCodes.Status201Created)]
    [EndpointSummary("Closes an open job.")]
    [EndpointName("CloseEmployerJob")]
    public Task<IActionResult> Close(long id, CancellationToken ct) => Act(id, "close", ct);

    [HttpPost("jobs/{id:long}/reopen")]
    [ProducesResponseType(typeof(EmployerJobDto), StatusCodes.Status201Created)]
    [EndpointSummary("Puts a closed job back in review (not past its closing date).")]
    [EndpointName("ReopenEmployerJob")]
    public Task<IActionResult> Reopen(long id, CancellationToken ct) => Act(id, "reopen", ct);

    private async Task<IActionResult> Act(long id, string action, CancellationToken ct)
    {
        var result = await sender.Send(new EmployerJobActionCommand(CurrentUserId, id, action), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpGet("applications")]
    [ProducesResponseType(typeof(PagedResult<EmployerApplicationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Applications to the employer's jobs, newest first.")]
    [EndpointName("GetEmployerApplications")]
    public async Task<IActionResult> Applications([FromQuery] CareersListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployerApplicationsQuery(CurrentUserId, r.Page, r.PageSize, r.Statuses, r.JobId, r.Search), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("applications/{id:long}")]
    [ProducesResponseType(typeof(EmployerApplicationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Opens an application (it is no longer new). Another employer's is not found.")]
    [EndpointName("GetEmployerApplication")]
    public async Task<IActionResult> Application(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployerApplicationQuery(CurrentUserId, id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("applications/{id:long}/status")]
    [ProducesResponseType(typeof(EmployerApplicationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Moves an application one step, or rejects it. The note goes to the candidate.")]
    [EndpointName("SetEmployerApplicationStatus")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] StatusNoteRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetEmployerApplicationStatusCommand(CurrentUserId, id, r.Status ?? string.Empty, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("applications/{id:long}/note")]
    [ProducesResponseType(typeof(EmployerApplicationDto), StatusCodes.Status200OK)]
    [EndpointSummary("Saves the employer's private note (up to 2000 characters). Never shown to the candidate.")]
    [EndpointName("SetEmployerApplicationNote")]
    public async Task<IActionResult> SetNote(long id, [FromBody] NoteRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetEmployerApplicationNoteCommand(CurrentUserId, id, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("applications/{id:long}/cv")]
    [EndpointSummary("Downloads the candidate's CV. Private: never cached.")]
    [EndpointName("DownloadEmployerApplicationCv")]
    public async Task<IActionResult> Cv(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DownloadCvQuery(id, "employer", CurrentUserId), ct);
        return result.Match(file => CareersMapping.Download(this, file), Problem);
    }
}

// ---- the candidate's side -------------------------------------------------------------------------

[Route("api/v{version:apiVersion}/me")]
[ApiVersion("1.0")]
[Authorize(Roles = "customer")]
public sealed class MeCareersController(ISender sender) : ApiController
{
    [HttpPost("cv")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(7 * 1024 * 1024)]
    [ProducesResponseType(typeof(CvFileDto), StatusCodes.Status201Created)]
    [EndpointSummary("Uploads a CV (PDF, DOC or DOCX, up to 5 MB). Stored privately.")]
    [EndpointName("UploadMyCv")]
    public async Task<IActionResult> UploadCv([FromForm] UploadFileForm form, CancellationToken ct)
    {
        var bytes = await CareersMapping.ReadAsync(form.File, ct);
        var result = await sender.Send(new UploadCvCommand(CurrentUserId, bytes, form.File?.FileName), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpPost("job-applications")]
    [ProducesResponseType(typeof(CandidateApplicationDto), StatusCodes.Status201Created)]
    [EndpointSummary("Applies to a job that is taking applications, with one of the candidate's own CVs.")]
    [EndpointName("ApplyToJob")]
    public async Task<IActionResult> Apply([FromBody] ApplicationRequest r, CancellationToken ct)
    {
        var input = new ApplicationInput(r.JobId, r.CvId, r.CoverLetter, r.YearsExperience, r.Certifications, CareersMapping.Date(r.AvailableFrom), r.ExpectedSalary?.Amount);
        var result = await sender.Send(new SubmitApplicationCommand(CurrentUserId, input), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpGet("job-applications")]
    [ProducesResponseType(typeof(List<CandidateApplicationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("The candidate's applications. Never includes an employer's private note.")]
    [EndpointName("GetMyJobApplications")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyApplicationsQuery(CurrentUserId), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("job-applications/by-job/{jobId:long}")]
    [ProducesResponseType(typeof(CandidateApplicationDto), StatusCodes.Status200OK)]
    [EndpointSummary("The latest non-withdrawn application to a job; not found when there is none.")]
    [EndpointName("GetMyJobApplicationByJob")]
    public async Task<IActionResult> ByJob(long jobId, CancellationToken ct)
    {
        var result = await sender.Send(new GetMyApplicationByJobQuery(CurrentUserId, jobId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("job-applications/{id:long}/withdraw")]
    [ProducesResponseType(typeof(CandidateApplicationDto), StatusCodes.Status201Created)]
    [EndpointSummary("Withdraws an application that has not been decided.")]
    [EndpointName("WithdrawMyJobApplication")]
    public async Task<IActionResult> Withdraw(long id, [FromBody] ReasonRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new WithdrawMyApplicationCommand(CurrentUserId, id, r.Reason), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpGet("job-applications/{id:long}/cv")]
    [EndpointSummary("Downloads the CV of one of the candidate's own applications. Private: never cached.")]
    [EndpointName("DownloadMyApplicationCv")]
    public async Task<IActionResult> Cv(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DownloadCvQuery(id, "candidate", CurrentUserId), ct);
        return result.Match(file => CareersMapping.Download(this, file), Problem);
    }
}

// ---- staff ----------------------------------------------------------------------------------------

[Route("api/v{version:apiVersion}/admin")]
[ApiVersion("1.0")]
public sealed class AdminCareersController(ISender sender) : ApiController
{
    [HttpGet("employers")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminEmployerDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Employers to verify: pending first, then suspended, rejected and verified.")]
    [EndpointName("GetAdminEmployers")]
    public async Task<IActionResult> Employers([FromQuery] CareersListRequest r, CancellationToken ct)
    {
        var statuses = (r.VerificationStatuses ?? []).Concat(r.Statuses ?? []).ToArray();
        var result = await sender.Send(new GetAdminEmployersQuery(r.Page, r.PageSize, r.Search, statuses, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("employers/options")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(List<EmployerOptionDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Every employer by name, for pickers.")]
    [EndpointName("GetAdminEmployerOptions")]
    public async Task<IActionResult> EmployerOptions(CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployerOptionsQuery(), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpGet("employers/{id:long}")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(AdminEmployerDto), StatusCodes.Status200OK)]
    [EndpointSummary("An employer.")]
    [EndpointName("GetAdminEmployer")]
    public async Task<IActionResult> Employer(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminEmployerQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("employers/{id:long}/verification")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(typeof(AdminEmployerDto), StatusCodes.Status200OK)]
    [EndpointSummary("Verifies, rejects or suspends an employer. Rejecting or suspending needs a note; the moves follow a graph.")]
    [EndpointName("DecideEmployerVerification")]
    public async Task<IActionResult> Decide(long id, [FromBody] StatusNoteRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new DecideEmployerVerificationCommand(id, r.Status ?? string.Empty, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("jobs")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminJobDto>), StatusCodes.Status200OK)]
    [EndpointSummary("All jobs, for moderation.")]
    [EndpointName("GetAdminJobs")]
    public async Task<IActionResult> Jobs([FromQuery] CareersListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminJobsQuery(r.Page, r.PageSize, r.Search, r.Statuses, r.EmployerIds, r.CategoryIds, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("jobs/{id:long}")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(AdminJobDto), StatusCodes.Status200OK)]
    [EndpointSummary("A job.")]
    [EndpointName("GetAdminJob")]
    public async Task<IActionResult> Job(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminJobQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("jobs/{id:long}/moderation")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(typeof(AdminJobDto), StatusCodes.Status200OK)]
    [EndpointSummary("Approves (the employer must be verified) or rejects (a note is required) a pending job.")]
    [EndpointName("ModerateJob")]
    public async Task<IActionResult> Moderate(long id, [FromBody] DecisionRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new ModerateJobCommand(id, r.Decision, r.Note), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPut("jobs/{id:long}/featured")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(typeof(AdminJobDto), StatusCodes.Status200OK)]
    [EndpointSummary("Features a job (only an open one) or un-features it.")]
    [EndpointName("SetJobFeatured")]
    public async Task<IActionResult> Featured(long id, [FromBody] FeaturedRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SetJobFeaturedCommand(id, r.Featured), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost("jobs/{id:long}/close")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(typeof(AdminJobDto), StatusCodes.Status200OK)]
    [EndpointSummary("Closes an open job on the employer's behalf.")]
    [EndpointName("CloseJobAsStaff")]
    public async Task<IActionResult> Close(long id, CancellationToken ct)
    {
        var result = await sender.Send(new CloseJobAsStaffCommand(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("jobs/{id:long}")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a job nobody has applied to.")]
    [EndpointName("DeleteJobAsStaff")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteJobAsStaffCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("job-applications")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminJobApplicationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Every application, read-only.")]
    [EndpointName("GetAdminJobApplications")]
    public async Task<IActionResult> Applications([FromQuery] CareersListRequest r, CancellationToken ct)
    {
        var jobIds = (r.JobIds ?? []).Concat(r.JobId is long j ? [j] : []).ToArray();
        var result = await sender.Send(new GetAdminApplicationsQuery(r.Page, r.PageSize, r.Search, r.Statuses, jobIds, r.EmployerIds, r.CustomerId, r.SortField, r.SortDir), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("job-applications/{id:long}")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(AdminJobApplicationDto), StatusCodes.Status200OK)]
    [EndpointSummary("An application (no private employer note; CV metadata only).")]
    [EndpointName("GetAdminJobApplication")]
    public async Task<IActionResult> Application(long id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminApplicationQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet("job-applications/{id:long}/cv")]
    [Authorize(Permissions.Careers.Read)]
    [EndpointSummary("Downloads a CV. Private: never cached.")]
    [EndpointName("DownloadAdminApplicationCv")]
    public async Task<IActionResult> Cv(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DownloadCvQuery(id, "staff", CurrentUserId), ct);
        return result.Match(file => CareersMapping.Download(this, file), Problem);
    }

    [HttpGet("job-categories")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(PagedResult<AdminJobCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Job categories with job counts.")]
    [EndpointName("GetAdminJobCategories")]
    public async Task<IActionResult> Categories([FromQuery] CareersListRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminJobCategoriesQuery(r.Page, r.PageSize, r.Search, r.SortField, r.SortDir, r.Locale), ct);
        return result.Match(page => Ok(new { page.Items, page.Meta }), Problem);
    }

    [HttpGet("job-categories/options")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(List<AdminJobCategoryDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Every job category, for pickers.")]
    [EndpointName("GetAdminJobCategoryOptions")]
    public async Task<IActionResult> CategoryOptions([FromQuery] string? locale, CancellationToken ct)
    {
        var result = await sender.Send(new GetJobCategoryOptionsQuery(locale), ct);
        return result.Match(items => Ok(items), Problem);
    }

    [HttpPost("job-categories")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(typeof(AdminJobCategoryDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a job category.")]
    [EndpointName("CreateJobCategory")]
    public async Task<IActionResult> CreateCategory([FromBody] JobCategoryDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SaveJobCategoryCommand(null, r.Slug, r.Name.En, r.Name.Ar), ct);
        return result.Match(dto => StatusCode(StatusCodes.Status201Created, dto), Problem);
    }

    [HttpPut("job-categories/{id:long}")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(typeof(AdminJobCategoryDto), StatusCodes.Status200OK)]
    [EndpointSummary("Updates a job category.")]
    [EndpointName("UpdateJobCategory")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] JobCategoryDraftRequest r, CancellationToken ct)
    {
        var result = await sender.Send(new SaveJobCategoryCommand(id, r.Slug, r.Name.En, r.Name.Ar), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpDelete("job-categories/{id:long}")]
    [Authorize(Permissions.Careers.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Deletes a job category that no job uses.")]
    [EndpointName("DeleteJobCategory")]
    public async Task<IActionResult> DeleteCategory(long id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteJobCategoryCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("careers/pending-count")]
    [Authorize(Permissions.Careers.Read)]
    [ProducesResponseType(typeof(CareersPendingCountDto), StatusCodes.Status200OK)]
    [EndpointSummary("Employers waiting for verification and jobs waiting for review (for the sidebar badges).")]
    [EndpointName("GetCareersPendingCount")]
    public async Task<IActionResult> PendingCount(CancellationToken ct)
    {
        var result = await sender.Send(new GetCareersPendingCountQuery(), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}

using System.Text.RegularExpressions;
using FluentValidation;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Auth;
using JoFit.Application.Features.Notifications;
using JoFit.Domain.Careers;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Identity.Users;
using JoFit.Domain.Locations;
using JoFit.Domain.Media;
using JoFit.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Careers;

public sealed record RegisterEmployerCommand(
    string CompanyName, string Kind, string CityId, string ContactName, string Email, string Phone, string? Website, string Password, string? Locale)
    : IRequest<Result<SignedInDto>>;

public sealed record GetEmployerProfileQuery(long UserId) : IRequest<Result<EmployerAccountProfileDto>>;

/// <summary>Logo: Keep leaves it, Clear removes it, otherwise MediaId names an uploaded employer-logo image.</summary>
public sealed record EmployerLogoChange(bool Keep, string? MediaId, string? Alt);

public sealed record UpdateEmployerProfileCommand(
    long UserId, string Name, string Kind, string CityId, string? About, string? Website, string? Email, string? Phone, string ContactName, string? Size,
    EmployerLogoChange Logo) : IRequest<Result<EmployerAccountProfileDto>>;

public sealed record CountsDto(int OpenJobs, int PendingJobs, int DraftJobs, int ClosedJobs, int NewApplications, int TotalApplications);

public sealed record EmployerDashboardDto(EmployerAccountProfileDto Profile, CountsDto Counts, IReadOnlyList<EmployerApplicationDto> RecentApplications);

public sealed record GetEmployerDashboardQuery(long UserId) : IRequest<Result<EmployerDashboardDto>>;

public sealed class RegisterEmployerCommandValidator : AbstractValidator<RegisterEmployerCommand>
{
    private static readonly Regex Website = new(@"^https?://[^\s.]+\.[^\s]+$", RegexOptions.Compiled);

    public RegisterEmployerCommandValidator()
    {
        RuleFor(x => x.CompanyName).Must(n => Employer.Collapse(n).Length >= 2).WithMessage("Enter the company name.");
        RuleFor(x => x.Kind).Must(k => CareersRules.EmployerKinds.Contains(k)).WithMessage("Choose what kind of organisation you are.");
        RuleFor(x => x.ContactName).Must(n => { var c = Employer.Collapse(n); return c.Length >= 3 && c.Contains(' '); }).WithMessage("Enter the contact person's full name.");
        RuleFor(x => x.Email).Must(AccountRules.IsValidEmail).WithMessage("An email address we can reach you on.");
        RuleFor(x => x.Phone).Must(p => AccountRules.IsValidPhone(p)).WithMessage("A phone number with at least 8 digits, for example +962 7 9000 0000.");
        RuleFor(x => x.Website).Must(w => string.IsNullOrWhiteSpace(w) || Website.IsMatch(w.Trim())).WithMessage("Use a full address such as https://example.com.");
        RuleFor(x => x.Password).Password();
    }
}

internal static class EmployerSupport
{
    public static async Task<Employer?> OfUserAsync(IUnitOfWork uow, long userId, bool tracking, CancellationToken ct)
    {
        var query = tracking ? uow.GetRepository<Employer>().GetTableAsTracking() : uow.GetRepository<Employer>().GetTableNoTracking();
        return await query.FirstOrDefaultAsync(e => e.UserId == userId, ct);
    }

    public static async Task<EmployerAccountProfileDto> ProfileAsync(IUnitOfWork uow, Employer e, CancellationToken ct)
    {
        var db = uow.GetDbContext();
        var city = await db.Set<Location>().AsNoTracking().Where(l => l.Id == e.LocationId).Select(l => l.Slug).FirstOrDefaultAsync(ct);
        var email = await db.Set<User>().AsNoTracking().Where(u => u.Id == e.UserId).Select(u => u.Email).FirstOrDefaultAsync(ct);
        return new EmployerAccountProfileDto(
            e.Id, e.Slug, e.Name, e.Kind, city ?? string.Empty, e.About, e.Website, e.PublicEmail, e.PublicPhone, e.ContactName, e.Size,
            CareersSupport.Logo(e), new VerificationDto(e.VerificationStatus, e.VerificationNote), email ?? string.Empty, e.CreatedAtUtc);
    }
}

public class RegisterEmployerCommandHandler(IUnitOfWork unitOfWork, IAccountService accounts)
    : IRequestHandler<RegisterEmployerCommand, Result<SignedInDto>>
{
    public async Task<Result<SignedInDto>> Handle(RegisterEmployerCommand request, CancellationToken ct)
    {
        var location = await unitOfWork.GetDbContext().Set<Location>().AsNoTracking().FirstOrDefaultAsync(l => l.Slug == request.CityId, ct);
        if (location is null)
        {
            return CareersErrors.Field("cityId", "Choose one of the cities in the list.");
        }

        var signedIn = await accounts.RegisterEmployerAsync(
            new RegisterEmployerInput(
                Employer.Collapse(request.CompanyName), request.Kind, location.Id, Employer.Collapse(request.ContactName), request.Email,
                request.Phone.Trim(), string.IsNullOrWhiteSpace(request.Website) ? null : request.Website.Trim(), request.Password, request.Locale ?? "en"),
            ct);
        if (signedIn.IsError)
        {
            return signedIn.Errors;
        }

        var employer = await unitOfWork.GetRepository<Employer>().GetTableNoTracking().FirstAsync(e => e.Id == signedIn.Value.Session.Account.EmployerId, ct);
        NotificationWriter.ToStaff(unitOfWork, NotificationKinds.EmployerRegistered,
            ("New employer", "صاحب عمل جديد"),
            ($"{employer.Name} signed up and is waiting for verification.", $"{employer.Name} سجّل وينتظر التحقق."),
            $"/careers/employers/{employer.Id}");
        await unitOfWork.SaveChangesAsync(ct);
        return signedIn;
    }
}

public class GetEmployerProfileQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEmployerProfileQuery, Result<EmployerAccountProfileDto>>
{
    public async Task<Result<EmployerAccountProfileDto>> Handle(GetEmployerProfileQuery request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        return employer is null ? CareersErrors.EmployerNotFound : await EmployerSupport.ProfileAsync(unitOfWork, employer, ct);
    }
}

public class UpdateEmployerProfileCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<UpdateEmployerProfileCommand, Result<EmployerAccountProfileDto>>
{
    public async Task<Result<EmployerAccountProfileDto>> Handle(UpdateEmployerProfileCommand request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, true, ct);
        if (employer is null)
        {
            return CareersErrors.EmployerNotFound;
        }

        var errors = new List<Error>();
        var location = await unitOfWork.GetDbContext().Set<Location>().AsNoTracking().FirstOrDefaultAsync(l => l.Slug == request.CityId, ct);
        if (location is null) errors.Add(CareersErrors.Field("cityId", "Choose one of the cities in the list."));

        var updated = employer.UpdateProfile(new EmployerProfileDetails(
            request.Name, request.Kind, location?.Id ?? 0, request.About, request.Website, request.Email, request.Phone, request.ContactName, request.Size));
        if (updated.IsError) errors.AddRange(updated.Errors);

        // The logo was uploaded first; saving it here only points at that upload. Absent keeps it, null removes it.
        if (!request.Logo.Keep)
        {
            if (request.Logo.MediaId is null)
            {
                employer.SetLogo(null, null, null);
            }
            else
            {
                var asset = await unitOfWork.GetRepository<MediaAsset>().GetTableNoTracking()
                    .FirstOrDefaultAsync(a => a.PublicId == request.Logo.MediaId && a.Purpose == "employer-logo", ct);
                var alt = request.Logo.Alt?.Trim();
                if (asset is null) errors.Add(CareersErrors.Field("logo", "That logo was not uploaded."));
                else if (alt is { Length: > 300 }) errors.Add(CareersErrors.Field("logo", "The logo description cannot exceed 300 characters."));
                else employer.SetLogo(asset.PublicId, urlService.ToAbsoluteUrl($"/media/{asset.StorageKey}"), alt ?? asset.Alt);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await EmployerSupport.ProfileAsync(unitOfWork, employer, ct);
    }
}

public class GetEmployerDashboardQueryHandler(IUnitOfWork unitOfWork, IMediator mediator) : IRequestHandler<GetEmployerDashboardQuery, Result<EmployerDashboardDto>>
{
    public async Task<Result<EmployerDashboardDto>> Handle(GetEmployerDashboardQuery request, CancellationToken ct)
    {
        var employer = await EmployerSupport.OfUserAsync(unitOfWork, request.UserId, false, ct);
        if (employer is null)
        {
            return CareersErrors.EmployerNotFound;
        }

        var db = unitOfWork.GetDbContext();
        var jobs = await db.Set<Job>().AsNoTracking().Where(j => j.EmployerId == employer.Id).Select(j => new { j.Id, j.Status }).ToListAsync(ct);
        var jobIds = jobs.Select(j => j.Id).ToList();
        var applications = await db.Set<JobApplication>().AsNoTracking().Where(a => jobIds.Contains(a.JobId)).ToListAsync(ct);

        // Draft includes rejected jobs: both are waiting for the employer to act.
        var counts = new CountsDto(
            jobs.Count(j => j.Status == JobStates.Open), jobs.Count(j => j.Status == JobStates.Pending),
            jobs.Count(j => j.Status is JobStates.Draft or JobStates.Rejected), jobs.Count(j => j.Status == JobStates.Closed),
            applications.Count(a => a.IsNew), applications.Count);

        var recent = await mediator.Send(new GetEmployerApplicationsQuery(request.UserId, 1, 5, null, null, null), ct);
        return new EmployerDashboardDto(
            await EmployerSupport.ProfileAsync(unitOfWork, employer, ct), counts,
            recent.IsError ? [] : recent.Value.Items);
    }
}

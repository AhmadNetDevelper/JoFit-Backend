using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Careers;

/// <summary>The fields an employer edits on their own profile.</summary>
public sealed record EmployerProfileDetails(
    string Name,
    string Kind,
    long LocationId,
    string? About,
    string? Website,
    string? PublicEmail,
    string? PublicPhone,
    string ContactName,
    string? Size);

/// <summary>A gym, club, academy, clinic or sports company that posts jobs. Staff verify it before its jobs go public.</summary>
public class Employer : AuditableEntity
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[\d\s]{8,}$", RegexOptions.Compiled);
    private static readonly Regex WebsitePattern = new(@"^https?://[^\s.]+\.[^\s]+$", RegexOptions.Compiled);

    public long UserId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Kind { get; private set; } = "other";
    public long LocationId { get; private set; }
    public string? About { get; private set; }
    public string? Website { get; private set; }
    public string? PublicEmail { get; private set; }
    public string? PublicPhone { get; private set; }
    public string ContactName { get; private set; } = string.Empty;
    public string? Size { get; private set; }
    public string? LogoMediaId { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? LogoAlt { get; private set; }
    public string VerificationStatus { get; private set; } = EmployerVerification.Pending;
    public string? VerificationNote { get; private set; }

    private Employer()
    {
    }

    public bool IsVerified => VerificationStatus == EmployerVerification.Verified;

    public static Employer Register(long userId, string slug, string name, string kind, long locationId, string contactName, string? website, string phone, string email)
        => new()
        {
            UserId = userId,
            Slug = slug,
            Name = name,
            Kind = kind,
            LocationId = locationId,
            ContactName = contactName,
            Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim(),
            PublicPhone = phone,
            PublicEmail = email
        };

    public Result<Updated> UpdateProfile(EmployerProfileDetails d)
    {
        var errors = new List<Error>();
        var name = Collapse(d.Name);
        var contact = Collapse(d.ContactName);
        var about = string.IsNullOrWhiteSpace(d.About) ? null : d.About.Trim();
        var website = string.IsNullOrWhiteSpace(d.Website) ? null : d.Website.Trim();
        var email = string.IsNullOrWhiteSpace(d.PublicEmail) ? null : d.PublicEmail.Trim().ToLowerInvariant();
        var phone = string.IsNullOrWhiteSpace(d.PublicPhone) ? null : d.PublicPhone.Trim();

        if (name.Length < 2) errors.Add(CareersErrors.Field("name", "Enter the company name."));
        if (!CareersRules.EmployerKinds.Contains(d.Kind)) errors.Add(CareersErrors.Field("kind", "Choose what kind of organisation this is."));
        if (d.LocationId <= 0) errors.Add(CareersErrors.Field("cityId", "Choose a city."));
        if (about is { Length: > 3000 }) errors.Add(CareersErrors.Field("about", "About can be at most 3000 characters."));
        if (website is not null && !WebsitePattern.IsMatch(website)) errors.Add(CareersErrors.Field("website", "Use a full address such as https://example.com."));
        if (email is not null && !EmailPattern.IsMatch(email)) errors.Add(CareersErrors.Field("email", "Enter a valid email address."));
        if (phone is not null && !PhonePattern.IsMatch(phone)) errors.Add(CareersErrors.Field("phone", "A phone number with at least 8 digits."));
        if (contact.Length < 3 || !contact.Contains(' ')) errors.Add(CareersErrors.Field("contactName", "Enter the contact person's full name."));
        if (d.Size is not null && !CareersRules.EmployerSizes.Contains(d.Size)) errors.Add(CareersErrors.Field("size", "Choose a company size from the list."));
        if (errors.Count > 0)
        {
            return errors;
        }

        Name = name;
        Kind = d.Kind;
        LocationId = d.LocationId;
        About = about;
        Website = website;
        PublicEmail = email;
        PublicPhone = phone;
        ContactName = contact;
        Size = d.Size;
        return Result.Updated;
    }

    public void SetLogo(string? mediaId, string? url, string? alt)
    {
        LogoMediaId = mediaId;
        LogoUrl = url;
        LogoAlt = alt;
    }

    /// <summary>
    /// Staff decide: verified, rejected or suspended (never "pending"). The moves are a graph, and a rejection or
    /// suspension needs a note of up to 1000 characters. Verifying clears the old note.
    /// </summary>
    public Result<Updated> Decide(string status, string? note)
    {
        if (status is not (EmployerVerification.Verified or EmployerVerification.Rejected or EmployerVerification.Suspended))
        {
            return CareersErrors.Field("status", "Choose verified, rejected or suspended.");
        }

        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (status is EmployerVerification.Rejected or EmployerVerification.Suspended && trimmed is null)
        {
            return CareersErrors.Field("note", "Say why, so the employer knows what to fix.");
        }

        if (trimmed is { Length: > 1000 })
        {
            return CareersErrors.Field("note", "The note can be at most 1000 characters.");
        }

        if (!EmployerVerification.CanMove(VerificationStatus, status))
        {
            return CareersErrors.IllegalEmployerMove(VerificationStatus, status);
        }

        VerificationStatus = status;
        VerificationNote = status == EmployerVerification.Verified ? null : trimmed;
        return Result.Updated;
    }

    public static string Collapse(string? text) => string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

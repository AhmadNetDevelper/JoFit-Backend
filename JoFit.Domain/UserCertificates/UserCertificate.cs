using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UserCertificates;

public class UserCertificate : AuditableEntity
{
    public long UserId { get; private set; }
    public string CertificateName { get; private set; } = string.Empty;
    public string OrganizationName { get; private set; } = string.Empty;
    public DateTime IssuedDate { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public string CredentialId { get; private set; } = string.Empty;
    public string? CredentialUrl { get; private set; }

    /// <summary>
    /// Link generated after the certificate file is uploaded.
    /// The file details are stored in the Attachments table
    /// (EntityTypeId = UserCertificate, EntityId = this certificate's Id).
    /// </summary>
    public string? CertificateUrl { get; private set; }

    private UserCertificate()
    {
    }

    private UserCertificate(
        long userId,
        string certificateName,
        string organizationName,
        DateTime issuedDate,
        DateTime expiryDate,
        string credentialId,
        string? credentialUrl,
        string? certificateUrl) : base()
    {
        UserId = userId;
        CertificateName = certificateName;
        OrganizationName = organizationName;
        IssuedDate = issuedDate;
        ExpiryDate = expiryDate;
        CredentialId = credentialId;
        CredentialUrl = credentialUrl;
        CertificateUrl = certificateUrl;
    }

    /// <summary>
    /// Creates a new UserCertificate instance with validation.
    /// </summary>
    public static Result<UserCertificate> Create(
        long userId,
        string certificateName,
        string organizationName,
        DateTime issuedDate,
        DateTime expiryDate,
        string credentialId,
        string? credentialUrl = null,
        string? certificateUrl = null)
    {
        if (userId <= 0)
        {
            return UserCertificateErrors.UserIdRequired;
        }

        var validation = Validate(
            certificateName, organizationName, issuedDate, expiryDate, credentialId, credentialUrl, certificateUrl);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new UserCertificate(
            userId,
            certificateName, organizationName, issuedDate, expiryDate, credentialId, credentialUrl, certificateUrl);
    }

    /// <summary>
    /// Updates the certificate information with validation.
    /// </summary>
    public Result<Updated> Update(
        string certificateName,
        string organizationName,
        DateTime issuedDate,
        DateTime expiryDate,
        string credentialId,
        string? credentialUrl,
        string? certificateUrl)
    {
        var validation = Validate(
            certificateName, organizationName, issuedDate, expiryDate, credentialId, credentialUrl, certificateUrl);
        if (validation is not null)
        {
            return validation.Value;
        }

        CertificateName = certificateName;
        OrganizationName = organizationName;
        IssuedDate = issuedDate;
        ExpiryDate = expiryDate;
        CredentialId = credentialId;
        CredentialUrl = credentialUrl;
        CertificateUrl = certificateUrl;

        return Result.Updated;
    }

    /// <summary>
    /// Sets the link of the uploaded certificate file.
    /// </summary>
    public Result<Updated> SetCertificateUrl(string? certificateUrl)
    {
        if (certificateUrl is { Length: > 500 })
        {
            return UserCertificateErrors.CertificateUrlMaxLengthExceeded;
        }

        CertificateUrl = certificateUrl;

        return Result.Updated;
    }

    private static Error? Validate(
        string certificateName,
        string organizationName,
        DateTime issuedDate,
        DateTime expiryDate,
        string credentialId,
        string? credentialUrl,
        string? certificateUrl)
    {
        if (string.IsNullOrWhiteSpace(certificateName))
        {
            return UserCertificateErrors.CertificateNameRequired;
        }

        if (certificateName.Length > 100)
        {
            return UserCertificateErrors.CertificateNameMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(organizationName))
        {
            return UserCertificateErrors.OrganizationNameRequired;
        }

        if (organizationName.Length > 100)
        {
            return UserCertificateErrors.OrganizationNameMaxLengthExceeded;
        }

        if (expiryDate < issuedDate)
        {
            return UserCertificateErrors.ExpiryDateBeforeIssuedDate;
        }

        if (string.IsNullOrWhiteSpace(credentialId))
        {
            return UserCertificateErrors.CredentialIdRequired;
        }

        if (credentialId.Length > 100)
        {
            return UserCertificateErrors.CredentialIdMaxLengthExceeded;
        }

        if (credentialUrl is { Length: > 500 })
        {
            return UserCertificateErrors.CredentialUrlMaxLengthExceeded;
        }

        if (certificateUrl is { Length: > 500 })
        {
            return UserCertificateErrors.CertificateUrlMaxLengthExceeded;
        }

        return null;
    }
}

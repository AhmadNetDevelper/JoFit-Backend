using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UserCertificates;

public static class UserCertificateErrors
{
    public static readonly Error UserIdRequired = Error.Validation(
        "UserCertificate.UserIdRequired",
        "User is required");

    public static readonly Error CertificateNameRequired = Error.Validation(
        "UserCertificate.CertificateNameRequired",
        "Certificate name is required");

    public static readonly Error CertificateNameMaxLengthExceeded = Error.Validation(
        "UserCertificate.CertificateNameMaxLengthExceeded",
        "Certificate name cannot exceed 100 characters");

    public static readonly Error OrganizationNameRequired = Error.Validation(
        "UserCertificate.OrganizationNameRequired",
        "Organization name is required");

    public static readonly Error OrganizationNameMaxLengthExceeded = Error.Validation(
        "UserCertificate.OrganizationNameMaxLengthExceeded",
        "Organization name cannot exceed 100 characters");

    public static readonly Error ExpiryDateBeforeIssuedDate = Error.Validation(
        "UserCertificate.ExpiryDateBeforeIssuedDate",
        "Expiry date cannot be before issued date");

    public static readonly Error CredentialIdRequired = Error.Validation(
        "UserCertificate.CredentialIdRequired",
        "Credential ID is required");

    public static readonly Error CredentialIdMaxLengthExceeded = Error.Validation(
        "UserCertificate.CredentialIdMaxLengthExceeded",
        "Credential ID cannot exceed 100 characters");

    public static readonly Error CredentialUrlMaxLengthExceeded = Error.Validation(
        "UserCertificate.CredentialUrlMaxLengthExceeded",
        "Credential URL cannot exceed 500 characters");

    public static readonly Error CertificateUrlMaxLengthExceeded = Error.Validation(
        "UserCertificate.CertificateUrlMaxLengthExceeded",
        "Certificate URL cannot exceed 500 characters");

    public static readonly Error UserCertificateNotFound = Error.NotFound(
        "UserCertificate.NotFound",
        "User certificate not found");
}

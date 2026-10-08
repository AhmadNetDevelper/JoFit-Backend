using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace JoFit.Contracts.Requests.UserCertificates;

public sealed record UserCertificateRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string CertificateName { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string OrganizationName { get; init; }

    public DateTime IssuedDate { get; init; }

    public DateTime ExpiryDate { get; init; }

    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public required string CredentialId { get; init; }

    [StringLength(500)]
    public string? CredentialUrl { get; init; }

    /// <summary>
    /// Optional certificate file. It is uploaded, its details are stored in Attachments,
    /// and the generated link is saved as the certificate's CertificateUrl.
    /// </summary>
    public IFormFile? CertificateFile { get; init; }
}

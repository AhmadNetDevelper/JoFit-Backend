using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Customers;

/// <summary>A delivery address saved by a customer.</summary>
public class Address : AuditableEntity
{
    private static readonly Regex PhonePattern = new(@"^\+?[\d\s]{8,}$", RegexOptions.Compiled);

    public long CustomerId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    private Address()
    {
    }

    public static Result<Address> Create(long customerId, string? recipient, string? phone, string? line1, string? line2, string? city, string? notes)
    {
        var errors = new List<Error>();
        recipient = (recipient ?? string.Empty).Trim();
        phone = (phone ?? string.Empty).Trim();
        line1 = (line1 ?? string.Empty).Trim();
        city = (city ?? string.Empty).Trim();

        if (recipient.Length < 2) errors.Add(Field("recipient", "Who should receive it? At least 2 characters."));
        if (!PhonePattern.IsMatch(phone)) errors.Add(Field("phone", "A phone number with at least 8 digits, for example +962 7 9000 0000."));
        if (line1.Length < 3) errors.Add(Field("line1", "Give the street address (at least 3 characters)."));
        if (city.Length < 2) errors.Add(Field("city", "Give the city (at least 2 characters)."));
        if (errors.Count > 0)
        {
            return errors;
        }

        return new Address
        {
            CustomerId = customerId,
            Recipient = recipient,
            Phone = phone,
            Line1 = line1,
            Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim(),
            City = city,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    private static Error Field(string field, string message) => Error.Validation($"Address.{field}", message).ForField(field);
}

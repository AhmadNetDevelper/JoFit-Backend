using System.Text.RegularExpressions;
using FluentValidation;

namespace JoFit.Application.Features.Auth;

/// <summary>Input rules shared by sign-up, profile and security forms.</summary>
public static partial class AccountRules
{
    public const int MinPasswordLength = 8;

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+?[\d\s]{8,}$")]
    private static partial Regex PhonePattern();

    public static string CollapseWhitespace(string? value)
        => string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static bool IsValidEmail(string? email) => !string.IsNullOrWhiteSpace(email) && EmailPattern().IsMatch(email.Trim());

    public static bool IsValidPhone(string? phone) => phone is not null && PhonePattern().IsMatch(phone.Trim());

    /// <summary>A full name: at least two words and three characters once whitespace is collapsed.</summary>
    public static bool IsValidFullName(string? name)
    {
        var collapsed = CollapseWhitespace(name);
        return collapsed.Length >= 3 && collapsed.Contains(' ');
    }

    public static IRuleBuilderOptions<T, string?> FullName<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(IsValidFullName).WithMessage("Enter your full name (first and last name).");

    public static IRuleBuilderOptions<T, string?> Email<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(IsValidEmail).WithMessage("An email address we can reach you on.");

    public static IRuleBuilderOptions<T, string?> OptionalPhone<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(p => string.IsNullOrWhiteSpace(p) || IsValidPhone(p)).WithMessage("A phone number with at least 8 digits, for example +962 7 9000 0000.");

    public static IRuleBuilderOptions<T, string?> Password<T>(this IRuleBuilder<T, string?> rule)
        => rule.Must(p => p is { Length: >= MinPasswordLength }).WithMessage($"Use at least {MinPasswordLength} characters.");
}

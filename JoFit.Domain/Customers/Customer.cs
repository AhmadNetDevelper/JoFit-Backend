using JoFit.Domain.Common;

namespace JoFit.Domain.Customers;

/// <summary>A shopper. Signs in through the linked identity user.</summary>
public class Customer : AuditableEntity
{
    public long UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string Locale { get; private set; } = "en";
    public bool MarketingOptIn { get; private set; }

    private Customer()
    {
    }

    public static Customer Create(long userId, string name, string? phone, string locale, bool marketingOptIn)
        => new()
        {
            UserId = userId,
            Name = name,
            Phone = phone,
            Locale = locale == "ar" ? "ar" : "en",
            MarketingOptIn = marketingOptIn
        };

    public void UpdateProfile(string name, string? phone)
    {
        Name = name;
        Phone = phone;
    }

    public void UpdatePreferences(string locale, bool marketingOptIn)
    {
        Locale = locale == "ar" ? "ar" : "en";
        MarketingOptIn = marketingOptIn;
    }
}

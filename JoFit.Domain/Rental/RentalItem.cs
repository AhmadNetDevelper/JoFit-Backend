using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Rental;

public static class RentalConditions
{
    public static readonly string[] All = ["new", "like-new", "good", "fair"];
}

public static class RentalUnits
{
    public const string Hour = "hour";
    public const string Day = "day";
    public const string Week = "week";

    /// <summary>In the order rates are stored and shown.</summary>
    public static readonly string[] All = [Hour, Day, Week];
}

public sealed record RentalRateInput(string Unit, long PriceAmount, int MinUnits, int? MaxUnits);

/// <summary>The editable fields of a rental item, as the back-office form saves them.</summary>
public sealed record RentalItemDetails(
    string Slug,
    string Status,
    string TitleEn,
    string TitleAr,
    string SummaryEn,
    string SummaryAr,
    string? DescriptionEn,
    string? DescriptionAr,
    string Condition,
    long DepotId,
    int TotalUnits,
    int UnitsOutOfService,
    long? DepositAmount,
    IReadOnlyList<RentalRateInput> Rates);

public class RentalItem : AuditableEntity
{
    public string Slug { get; private set; } = string.Empty;
    public string Status { get; private set; } = "draft";
    public string TitleEn { get; private set; } = string.Empty;
    public string TitleAr { get; private set; } = string.Empty;
    public string SummaryEn { get; private set; } = string.Empty;
    public string SummaryAr { get; private set; } = string.Empty;
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string Condition { get; private set; } = "good";
    public long DepotId { get; private set; }
    public int TotalUnits { get; private set; }
    public int UnitsOutOfService { get; private set; }

    /// <summary>Minor units of JOD, per unit hired. Null means no deposit.</summary>
    public long? DepositAmount { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public List<RentalRate> Rates { get; private set; } = [];

    private RentalItem()
    {
    }

    public bool IsPublished => Status == "published";

    public static Result<RentalItem> Create(RentalItemDetails details)
    {
        var item = new RentalItem();
        var applied = item.Apply(details);
        return applied.IsError ? applied.Errors : item;
    }

    public Result<Updated> Apply(RentalItemDetails d)
    {
        var errors = new List<Error>();
        var slug = (d.Slug ?? string.Empty).Trim();
        var titleEn = (d.TitleEn ?? string.Empty).Trim();
        var titleAr = (d.TitleAr ?? string.Empty).Trim();
        var summaryEn = (d.SummaryEn ?? string.Empty).Trim();
        var summaryAr = (d.SummaryAr ?? string.Empty).Trim();
        var descriptionEn = string.IsNullOrWhiteSpace(d.DescriptionEn) ? null : d.DescriptionEn.Trim();
        var descriptionAr = string.IsNullOrWhiteSpace(d.DescriptionAr) ? null : d.DescriptionAr.Trim();

        if (slug.Length == 0) errors.Add(RentalErrors.Field("slug", "A slug is required."));
        else if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(-[a-z0-9]+)*$")) errors.Add(RentalErrors.Field("slug", "Use lowercase letters, numbers and single hyphens."));
        if (!new[] { "draft", "published", "archived" }.Contains(d.Status)) errors.Add(RentalErrors.Field("status", "Choose draft, published or archived."));
        if (titleEn.Length == 0) errors.Add(RentalErrors.Field("title.en", "The English title is required."));
        if (titleAr.Length == 0) errors.Add(RentalErrors.Field("title.ar", "The Arabic title is required."));
        if (summaryEn.Length == 0) errors.Add(RentalErrors.Field("summary.en", "The English summary is required."));
        if (summaryAr.Length == 0) errors.Add(RentalErrors.Field("summary.ar", "The Arabic summary is required."));
        if ((descriptionEn is null) != (descriptionAr is null)) errors.Add(RentalErrors.Field("description", "Give the description in both languages or in neither."));
        if (!RentalConditions.All.Contains(d.Condition)) errors.Add(RentalErrors.Field("condition", "Choose the item's condition."));
        if (d.DepotId <= 0) errors.Add(RentalErrors.Field("depotId", "Choose a depot."));
        if (d.TotalUnits < 1) errors.Add(RentalErrors.Field("totalUnits", "There must be at least one unit."));
        if (d.UnitsOutOfService < 0 || d.UnitsOutOfService > d.TotalUnits) errors.Add(RentalErrors.Field("unitsOutOfService", "Units out of service must be between 0 and the total."));
        if (d.DepositAmount is <= 0) errors.Add(RentalErrors.Field("deposit", "A deposit must be more than 0, or left out."));

        // One rate per unit; errors are keyed by unit, not by position.
        var rates = new List<RentalRateInput>();
        if (d.Rates is null || d.Rates.Count == 0)
        {
            errors.Add(RentalErrors.Field("rates", "Add at least one rate."));
        }
        else
        {
            foreach (var group in d.Rates.GroupBy(r => r.Unit))
            {
                var rate = group.First();
                if (!RentalUnits.All.Contains(rate.Unit))
                {
                    errors.Add(RentalErrors.Field("rates", "Rates are per hour, day or week."));
                    continue;
                }

                if (group.Count() > 1) errors.Add(RentalErrors.Field($"rates.{rate.Unit}.price", "Only one rate per unit."));
                if (rate.PriceAmount <= 0) errors.Add(RentalErrors.Field($"rates.{rate.Unit}.price", "The price must be more than 0."));
                if (rate.MinUnits < 1) errors.Add(RentalErrors.Field($"rates.{rate.Unit}.minUnits", "The minimum must be at least 1."));
                if (rate.MaxUnits is int max && max < rate.MinUnits) errors.Add(RentalErrors.Field($"rates.{rate.Unit}.maxUnits", "The maximum cannot be below the minimum."));
                rates.Add(rate);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        Slug = slug;
        TitleEn = titleEn;
        TitleAr = titleAr;
        SummaryEn = summaryEn;
        SummaryAr = summaryAr;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
        Condition = d.Condition;
        DepotId = d.DepotId;
        TotalUnits = d.TotalUnits;
        UnitsOutOfService = d.UnitsOutOfService;
        DepositAmount = d.DepositAmount;

        // Stored sorted by unit.
        Rates.Clear();
        Rates.AddRange(rates.OrderBy(r => Array.IndexOf(RentalUnits.All, r.Unit))
            .Select(r => RentalRate.Create(r.Unit, r.PriceAmount, r.MinUnits, r.MaxUnits)));

        if (d.Status == "published")
        {
            PublishedAtUtc ??= DateTimeOffset.UtcNow;
        }

        Status = d.Status;
        return Result.Updated;
    }

    public Result<Updated> SetUnitsOutOfService(int units)
    {
        if (units < 0 || units > TotalUnits)
        {
            return RentalErrors.Field("unitsOutOfService", $"Choose a number from 0 to {TotalUnits}.");
        }

        UnitsOutOfService = units;
        return Result.Updated;
    }
}

public class RentalRate : Entity
{
    public long RentalItemId { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public long PriceAmount { get; private set; }
    public int MinUnits { get; private set; } = 1;
    public int? MaxUnits { get; private set; }

    private RentalRate()
    {
    }

    public static RentalRate Create(string unit, long priceAmount, int minUnits, int? maxUnits)
        => new() { Unit = unit, PriceAmount = priceAmount, MinUnits = minUnits, MaxUnits = maxUnits };
}

public class RentalItemCategoryLink
{
    public long RentalItemId { get; set; }
    public long RentalCategoryId { get; set; }
}

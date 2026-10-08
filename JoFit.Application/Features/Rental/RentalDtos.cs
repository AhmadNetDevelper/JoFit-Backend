using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Courses.Catalogue;
using JoFit.Domain.Rental;

namespace JoFit.Application.Features.Rental;

public sealed record RentalRateDto(string Unit, MoneyDto Price, int MinUnits, int? MaxUnits)
{
    public static RentalRateDto From(RentalRate r) => new(r.Unit, new MoneyDto(r.PriceAmount), r.MinUnits, r.MaxUnits);
}

public sealed record AdminRentalItemDto(
    long Id,
    string Slug,
    string Status,
    LocalizedText Title,
    LocalizedText Summary,
    LocalizedText? Description,
    string Condition,
    long DepotId,
    IReadOnlyList<long> CategoryIds,
    int TotalUnits,
    int UnitsOutOfService,
    MoneyDto? Deposit,
    IReadOnlyList<RentalRateDto> Rates,
    IReadOnlyList<MediaImageDto> Media,
    DateTimeOffset? PublishedAt,
    AuditDto Audit)
{
    public static AdminRentalItemDto From(RentalItem i, IReadOnlyList<long> categoryIds, IReadOnlyList<MediaImageDto> media)
        => new(
            i.Id,
            i.Slug,
            i.Status,
            new LocalizedText(i.TitleEn, i.TitleAr),
            new LocalizedText(i.SummaryEn, i.SummaryAr),
            i.DescriptionEn is null || i.DescriptionAr is null ? null : new LocalizedText(i.DescriptionEn, i.DescriptionAr),
            i.Condition,
            i.DepotId,
            categoryIds,
            i.TotalUnits,
            i.UnitsOutOfService,
            i.DepositAmount is long d ? new MoneyDto(d) : null,
            i.Rates.OrderBy(r => Array.IndexOf(RentalUnits.All, r.Unit)).Select(RentalRateDto.From).ToList(),
            media,
            i.PublishedAtUtc,
            AuditDto.From(i));
}

public sealed record DepotRefDto(long Id, string Name, string City, string CountryCode);

public sealed record PublicRentalItemDto(
    long Id,
    string Slug,
    string Title,
    string Summary,
    string? Description,
    string Condition,
    DepotRefDto? Depot,
    IReadOnlyList<CourseCategoryRefDto> Categories,
    IReadOnlyList<RentalRateDto> Rates,
    MoneyDto? LowestRate,
    MoneyDto? Deposit,
    IReadOnlyList<MediaImageDto> Media,
    DateTimeOffset? PublishedAt)
{
    public static PublicRentalItemDto From(
        RentalItem i, ILocalizationService loc, string? language, DepotRefDto? depot,
        IReadOnlyList<CourseCategoryRefDto> categories, IReadOnlyList<MediaImageDto> media)
    {
        var rates = i.Rates.OrderBy(r => Array.IndexOf(RentalUnits.All, r.Unit)).Select(RentalRateDto.From).ToList();
        return new PublicRentalItemDto(
            i.Id,
            i.Slug,
            loc.GetLocalizedName(i.TitleAr, i.TitleEn, language),
            loc.GetLocalizedName(i.SummaryAr, i.SummaryEn, language),
            i.DescriptionEn is null || i.DescriptionAr is null ? null : loc.GetLocalizedName(i.DescriptionAr, i.DescriptionEn, language),
            i.Condition,
            depot,
            categories,
            rates,
            rates.Count == 0 ? null : rates.MinBy(r => r.Price.Amount)!.Price,
            i.DepositAmount is long d ? new MoneyDto(d) : null,
            media,
            i.PublishedAtUtc);
    }
}

public sealed record RentalAvailabilityDto(long ItemId, string From, string To, int MinAvailable, IReadOnlyList<DayAvailability> Days);

/// <summary>A price for a period. Problems are a normal answer here (isAvailable false), never an HTTP error.</summary>
public sealed record RentalQuoteDto(
    long ItemId,
    string Unit,
    string From,
    string To,
    int Quantity,
    int Units,
    MoneyDto UnitPrice,
    MoneyDto Subtotal,
    MoneyDto? Deposit,
    bool IsAvailable,
    string? UnavailableReason);

public sealed record AdminRentalBookingDto(
    long Id,
    string Reference,
    long ItemId,
    string ItemTitle,
    long? CustomerId,
    string? CustomerName,
    long? OrderId,
    string From,
    string To,
    string Unit,
    int Quantity,
    int Units,
    MoneyDto UnitPrice,
    MoneyDto Subtotal,
    MoneyDto? Deposit,
    bool DepositReleased,
    string Status,
    AuditDto Audit);

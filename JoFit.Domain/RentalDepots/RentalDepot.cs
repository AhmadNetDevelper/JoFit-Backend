using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.RentalDepots;

public sealed record RentalDepotValues(
    string NameEn,
    string NameAr,
    string CityEn,
    string CityAr,
    string CountryCode);

public class RentalDepot : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string CityEn { get; private set; } = string.Empty;
    public string CityAr { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;

    private RentalDepot()
    {
    }

    public static Result<RentalDepot> Create(RentalDepotValues values)
    {
        var entity = new RentalDepot();
        var result = entity.Apply(values);
        return result.IsError ? result.Errors : entity;
    }

    public Result<Updated> Update(RentalDepotValues values) => Apply(values);

    public Result<Updated> SetParent(long? parentId)
    {
        return Result.Updated;
    }

    private Result<Updated> Apply(RentalDepotValues v)
    {
        var errors = new List<Error>();
        var nameEn = (v.NameEn ?? string.Empty).Trim();
        var nameAr = (v.NameAr ?? string.Empty).Trim();
        if (nameEn.Length == 0) errors.Add(RentalDepotErrors.NameEnRequired); else if (nameEn.Length > 100) errors.Add(RentalDepotErrors.NameTooLong);
        if (nameAr.Length == 0) errors.Add(RentalDepotErrors.NameArRequired); else if (nameAr.Length > 100) errors.Add(RentalDepotErrors.NameTooLong.ForField("name.ar"));
        var cityEn = (v.CityEn ?? string.Empty).Trim();
        var cityAr = (v.CityAr ?? string.Empty).Trim();
        if (cityEn.Length == 0) errors.Add(RentalDepotErrors.CityEnRequired);
        if (cityAr.Length == 0) errors.Add(RentalDepotErrors.CityArRequired);
        var countryCode = (v.CountryCode ?? string.Empty).Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(countryCode, "^[A-Z]{2}$")) errors.Add(RentalDepotErrors.CountryCodeInvalid);

        if (errors.Count > 0)
        {
            return errors;
        }

        NameEn = nameEn;
        NameAr = nameAr;
        CityEn = cityEn;
        CityAr = cityAr;
        CountryCode = countryCode;
        return Result.Updated;
    }
}

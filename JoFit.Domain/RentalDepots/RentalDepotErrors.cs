using JoFit.Domain.Common.Results;

namespace JoFit.Domain.RentalDepots;

public static class RentalDepotErrors
{
    public static readonly Error NameEnRequired = Error.Validation("RentalDepot.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("RentalDepot.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("RentalDepot.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("RentalDepot.NameTaken", "Another rental depot already uses this name.").ForField("name.en");
    public static readonly Error CityEnRequired = Error.Validation("RentalDepot.CityEnRequired", "The English city is required.").ForField("city.en");
    public static readonly Error CityArRequired = Error.Validation("RentalDepot.CityArRequired", "The Arabic city is required.").ForField("city.ar");
    public static readonly Error CountryCodeInvalid = Error.Validation("RentalDepot.CountryCodeInvalid", "Use a two-letter uppercase country code, for example JO.").ForField("countryCode");
    public static readonly Error NotFound = Error.NotFound("RentalDepot.NotFound", "That rental depot does not exist.");
}

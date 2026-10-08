using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Cities;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Countries;
using JoFit.Domain.JobStatuses;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Users;

/// <summary>
/// Checks that the optional country, city and job status a user points to exist.
/// </summary>
internal static class UserReferenceChecker
{
    public static async Task<Error?> CheckAsync(
        IUnitOfWork unitOfWork,
        long? countryId,
        long? cityId,
        long? jobStatusId,
        CancellationToken ct)
    {
        if (countryId.HasValue
            && !await unitOfWork.GetRepository<Country>().GetTableNoTracking()
                .AnyAsync(x => x.Id == countryId.Value, ct))
        {
            return CountryErrors.CountryNotFound;
        }

        if (cityId.HasValue)
        {
            var city = await unitOfWork.GetRepository<City>().GetTableNoTracking()
                .Where(x => x.Id == cityId.Value)
                .Select(x => new { x.CountryId })
                .FirstOrDefaultAsync(ct);

            if (city is null)
            {
                return CityErrors.CityNotFound;
            }

            if (countryId.HasValue && city.CountryId != countryId.Value)
            {
                return Error.Validation("User.CityCountryMismatch", "The city does not belong to the selected country.");
            }
        }

        if (jobStatusId.HasValue
            && !await unitOfWork.GetRepository<JobStatus>().GetTableNoTracking()
                .AnyAsync(x => x.Id == jobStatusId.Value, ct))
        {
            return JobStatusErrors.JobStatusNotFound;
        }

        return null;
    }
}

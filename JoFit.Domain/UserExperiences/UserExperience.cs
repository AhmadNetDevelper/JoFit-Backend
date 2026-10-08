using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;
using JoFit.Domain.JobTypes;

namespace JoFit.Domain.UserExperiences;

public class UserExperience : AuditableEntity
{
    public long UserId { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public long JobTypeId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string CompanyAddress { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsStillWorking { get; private set; }

    public JobType? JobType { get; private set; }

    private UserExperience()
    {
    }

    private UserExperience(
        long userId,
        string address,
        long jobTypeId,
        string companyName,
        string companyAddress,
        DateTime startDate,
        DateTime? endDate,
        bool isStillWorking) : base()
    {
        UserId = userId;
        Address = address;
        JobTypeId = jobTypeId;
        CompanyName = companyName;
        CompanyAddress = companyAddress;
        StartDate = startDate;
        EndDate = endDate;
        IsStillWorking = isStillWorking;
    }

    /// <summary>
    /// Creates a new UserExperience instance with validation.
    /// </summary>
    public static Result<UserExperience> Create(
        long userId,
        string address,
        long jobTypeId,
        string companyName,
        string companyAddress,
        DateTime startDate,
        DateTime? endDate = null,
        bool isStillWorking = false)
    {
        if (userId <= 0)
        {
            return UserExperienceErrors.UserIdRequired;
        }

        var validation = Validate(address, jobTypeId, companyName, companyAddress, startDate, endDate);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new UserExperience(userId, address, jobTypeId, companyName, companyAddress, startDate, endDate, isStillWorking);
    }

    /// <summary>
    /// Updates the experience information with validation.
    /// </summary>
    public Result<Updated> Update(
        string address,
        long jobTypeId,
        string companyName,
        string companyAddress,
        DateTime startDate,
        DateTime? endDate,
        bool isStillWorking)
    {
        var validation = Validate(address, jobTypeId, companyName, companyAddress, startDate, endDate);
        if (validation is not null)
        {
            return validation.Value;
        }

        Address = address;
        JobTypeId = jobTypeId;
        CompanyName = companyName;
        CompanyAddress = companyAddress;
        StartDate = startDate;
        EndDate = endDate;
        IsStillWorking = isStillWorking;

        return Result.Updated;
    }

    private static Error? Validate(
        string address,
        long jobTypeId,
        string companyName,
        string companyAddress,
        DateTime startDate,
        DateTime? endDate)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return UserExperienceErrors.AddressRequired;
        }

        if (address.Length > 300)
        {
            return UserExperienceErrors.AddressMaxLengthExceeded;
        }

        if (jobTypeId <= 0)
        {
            return UserExperienceErrors.JobTypeIdRequired;
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            return UserExperienceErrors.CompanyNameRequired;
        }

        if (companyName.Length > 100)
        {
            return UserExperienceErrors.CompanyNameMaxLengthExceeded;
        }

        if (string.IsNullOrWhiteSpace(companyAddress))
        {
            return UserExperienceErrors.CompanyAddressRequired;
        }

        if (companyAddress.Length > 100)
        {
            return UserExperienceErrors.CompanyAddressMaxLengthExceeded;
        }

        if (endDate.HasValue && endDate.Value < startDate)
        {
            return UserExperienceErrors.EndDateBeforeStartDate;
        }

        return null;
    }
}

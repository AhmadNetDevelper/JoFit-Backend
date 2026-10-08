using JoFit.Domain.Common.Results;

namespace JoFit.Domain.UserExperiences;

public static class UserExperienceErrors
{
    public static readonly Error UserIdRequired = Error.Validation(
        "UserExperience.UserIdRequired",
        "User is required");

    public static readonly Error AddressRequired = Error.Validation(
        "UserExperience.AddressRequired",
        "Address is required");

    public static readonly Error AddressMaxLengthExceeded = Error.Validation(
        "UserExperience.AddressMaxLengthExceeded",
        "Address cannot exceed 300 characters");

    public static readonly Error JobTypeIdRequired = Error.Validation(
        "UserExperience.JobTypeIdRequired",
        "Job type is required");

    public static readonly Error CompanyNameRequired = Error.Validation(
        "UserExperience.CompanyNameRequired",
        "Company name is required");

    public static readonly Error CompanyNameMaxLengthExceeded = Error.Validation(
        "UserExperience.CompanyNameMaxLengthExceeded",
        "Company name cannot exceed 100 characters");

    public static readonly Error CompanyAddressRequired = Error.Validation(
        "UserExperience.CompanyAddressRequired",
        "Company address is required");

    public static readonly Error CompanyAddressMaxLengthExceeded = Error.Validation(
        "UserExperience.CompanyAddressMaxLengthExceeded",
        "Company address cannot exceed 100 characters");

    public static readonly Error EndDateBeforeStartDate = Error.Validation(
        "UserExperience.EndDateBeforeStartDate",
        "End date cannot be before start date");

    public static readonly Error UserExperienceNotFound = Error.NotFound(
        "UserExperience.NotFound",
        "User experience not found");
}

using JoFit.Domain.AcademicDegrees;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;
using JoFit.Domain.UniversityMajors;

namespace JoFit.Domain.UserEducations;

public class UserEducation : AuditableEntity
{
    public long UserId { get; private set; }
    public string SchoolName { get; private set; } = string.Empty;
    public long AcademicDegreeId { get; private set; }
    public long UniversityMajorId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsStillEducation { get; private set; }

    public AcademicDegree? AcademicDegree { get; private set; }
    public UniversityMajor? UniversityMajor { get; private set; }

    private UserEducation()
    {
    }

    private UserEducation(
        long userId,
        string schoolName,
        long academicDegreeId,
        long universityMajorId,
        DateTime startDate,
        DateTime endDate,
        bool isStillEducation) : base()
    {
        UserId = userId;
        SchoolName = schoolName;
        AcademicDegreeId = academicDegreeId;
        UniversityMajorId = universityMajorId;
        StartDate = startDate;
        EndDate = endDate;
        IsStillEducation = isStillEducation;
    }

    /// <summary>
    /// Creates a new UserEducation instance with validation.
    /// </summary>
    public static Result<UserEducation> Create(
        long userId,
        string schoolName,
        long academicDegreeId,
        long universityMajorId,
        DateTime startDate,
        DateTime endDate,
        bool isStillEducation = false)
    {
        if (userId <= 0)
        {
            return UserEducationErrors.UserIdRequired;
        }

        var validation = Validate(schoolName, academicDegreeId, universityMajorId, startDate, endDate);
        if (validation is not null)
        {
            return validation.Value;
        }

        return new UserEducation(userId, schoolName, academicDegreeId, universityMajorId, startDate, endDate, isStillEducation);
    }

    /// <summary>
    /// Updates the education information with validation.
    /// </summary>
    public Result<Updated> Update(
        string schoolName,
        long academicDegreeId,
        long universityMajorId,
        DateTime startDate,
        DateTime endDate,
        bool isStillEducation)
    {
        var validation = Validate(schoolName, academicDegreeId, universityMajorId, startDate, endDate);
        if (validation is not null)
        {
            return validation.Value;
        }

        SchoolName = schoolName;
        AcademicDegreeId = academicDegreeId;
        UniversityMajorId = universityMajorId;
        StartDate = startDate;
        EndDate = endDate;
        IsStillEducation = isStillEducation;

        return Result.Updated;
    }

    private static Error? Validate(
        string schoolName,
        long academicDegreeId,
        long universityMajorId,
        DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(schoolName))
        {
            return UserEducationErrors.SchoolNameRequired;
        }

        if (schoolName.Length > 100)
        {
            return UserEducationErrors.SchoolNameMaxLengthExceeded;
        }

        if (academicDegreeId <= 0)
        {
            return UserEducationErrors.AcademicDegreeIdRequired;
        }

        if (universityMajorId <= 0)
        {
            return UserEducationErrors.UniversityMajorIdRequired;
        }

        if (endDate < startDate)
        {
            return UserEducationErrors.EndDateBeforeStartDate;
        }

        return null;
    }
}

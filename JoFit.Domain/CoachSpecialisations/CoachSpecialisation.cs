using System.Text.RegularExpressions;
using JoFit.Domain.Common;
using JoFit.Domain.Common.Results;

namespace JoFit.Domain.CoachSpecialisations;

public sealed record CoachSpecialisationValues(
    string NameEn,
    string NameAr);

public class CoachSpecialisation : AuditableEntity
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string NameEn { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;

    private CoachSpecialisation()
    {
    }

    public static Result<CoachSpecialisation> Create(CoachSpecialisationValues values)
    {
        var entity = new CoachSpecialisation();
        var result = entity.Apply(values);
        return result.IsError ? result.Errors : entity;
    }

    public Result<Updated> Update(CoachSpecialisationValues values) => Apply(values);

    public Result<Updated> SetParent(long? parentId)
    {
        return Result.Updated;
    }

    private Result<Updated> Apply(CoachSpecialisationValues v)
    {
        var errors = new List<Error>();
        var nameEn = (v.NameEn ?? string.Empty).Trim();
        var nameAr = (v.NameAr ?? string.Empty).Trim();
        if (nameEn.Length == 0) errors.Add(CoachSpecialisationErrors.NameEnRequired); else if (nameEn.Length > 100) errors.Add(CoachSpecialisationErrors.NameTooLong);
        if (nameAr.Length == 0) errors.Add(CoachSpecialisationErrors.NameArRequired); else if (nameAr.Length > 100) errors.Add(CoachSpecialisationErrors.NameTooLong.ForField("name.ar"));

        if (errors.Count > 0)
        {
            return errors;
        }

        NameEn = nameEn;
        NameAr = nameAr;
        return Result.Updated;
    }
}

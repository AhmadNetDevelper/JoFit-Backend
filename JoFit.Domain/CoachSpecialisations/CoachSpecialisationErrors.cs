using JoFit.Domain.Common.Results;

namespace JoFit.Domain.CoachSpecialisations;

public static class CoachSpecialisationErrors
{
    public static readonly Error NameEnRequired = Error.Validation("CoachSpecialisation.NameEnRequired", "The English name is required.").ForField("name.en");
    public static readonly Error NameArRequired = Error.Validation("CoachSpecialisation.NameArRequired", "The Arabic name is required.").ForField("name.ar");
    public static readonly Error NameTooLong = Error.Validation("CoachSpecialisation.NameTooLong", "A name cannot exceed 100 characters.").ForField("name.en");
    public static readonly Error NameTaken = Error.Validation("CoachSpecialisation.NameTaken", "Another coach specialisation already uses this name.").ForField("name.en");
    public static readonly Error NotFound = Error.NotFound("CoachSpecialisation.NotFound", "That coach specialisation does not exist.");
}

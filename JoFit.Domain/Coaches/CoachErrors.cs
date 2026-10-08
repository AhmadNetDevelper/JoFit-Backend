using JoFit.Domain.Common.Results;

namespace JoFit.Domain.Coaches;

public static class CoachErrors
{
    public static readonly Error NotFound = Error.NotFound("Coach.NotFound", "That coach does not exist.");
    public static readonly Error BookingNotFound = Error.NotFound("Coach.BookingNotFound", "That session does not exist.");
    public static readonly Error CertificationNotFound = Error.NotFound("Coach.CertificationNotFound", "That certification does not exist.");
    public static readonly Error SlotNotFound = Error.NotFound("Coach.SlotNotFound", "That time is not open for booking.");
    public static readonly Error SlotTaken = Error.Conflict("Coach.SlotTaken", "That time has just been taken.");

    public static Error Field(string field, string message)
        => Error.Validation($"Coach.{field}", message).ForField(field);

    public static readonly Error SlugTaken = Field("slug", "Another coach already uses this slug.");
    public static readonly Error EmailTaken = Field("email", "Another coach already uses this email address.");
    public static readonly Error LocationMissing = Field("locationId", "That location does not exist.");
    public static readonly Error SportsMissing = Field("sportIds", "Choose at least one sport, all of which must exist.");
    public static readonly Error SpecialisationsMissing = Field("specializations", "One of the specialisations does not exist.");
    public static readonly Error PackageNotOffered = Field("packageId", "That package is not offered by this coach.");

    public static Error IllegalMove(string from, string to)
        => Error.Conflict("Coach.IllegalMove", $"A {from} session cannot become {to}.");

    public static Error InUse(int sessions) => Error.Conflict(
        "Coach.InUse",
        $"{sessions} session(s) are still pending or confirmed. Finish or cancel them first.");
}

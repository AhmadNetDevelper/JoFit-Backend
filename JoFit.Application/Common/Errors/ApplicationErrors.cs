using JoFit.Domain.Common.Results;

namespace JoFit.Application.Common.Errors
{
    public static class ApplicationErrors
    {
        public static Error CarVinNotFound => Error.NotFound(
            code: "ApplicationError.CarVin.NotFound",
            description: "The specified VIN does not exist in the system."
        );

        public static Error ColorNotFound = Error.NotFound(
            code: "ApplicationError.Color.NotFound",
            description: "The specified color does not exist in the system."
        );

        public static Error MeasurementUnitNotFound = Error.NotFound(
            code: "ApplicationError.MeasurementUnit.NotFound",
            description: "The specified measurement unit does not exist in the system."
        );

        public static Error JoFitPriorityNotFound = Error.NotFound(
            code: "ApplicationError.JoFitPriority.NotFound",
            description: "The specified JoFit priority does not exist in the system."
        );

        public static Error JoFitPostponedReasonNotFound = Error.NotFound(
            code: "ApplicationError.JoFitPostponedReason.NotFound",
            description: "The specified JoFit postponed reason does not exist in the system."
        );

        public static Error JoFitDelayReasonNotFound = Error.NotFound(
            code: "ApplicationError.JoFitDelayReason.NotFound",
            description: "The specified JoFit delay reason does not exist in the system."
        );

        public static Error InspectorStatusNotFound = Error.NotFound(
            code: "ApplicationError.InspectorStatus.NotFound",
            description: "The specified inspector status does not exist in the system."
        );

        public static Error YearNotFound = Error.NotFound(
            code: "ApplicationError.Year.NotFound",
            description: "The specified year does not exist in the system."
        );

        public static Error CarBrandNotFound = Error.NotFound(
            code: "ApplicationError.CarBrand.NotFound",
            description: "The specified car brand does not exist in the system."
        );

        public static Error RoleNotFound = Error.NotFound(
            code: "ApplicationError.Role.NotFound",
            description: "The specified role does not exist in the system."
        );

        public static Error NoteTypeNotFound = Error.NotFound(
            code: "ApplicationError.NoteType.NotFound",
            description: "The specified note type does not exist in the system."
        );

        public static readonly Error ExpiredAccessTokenInvalid = Error.Conflict(
         code: "Auth.ExpiredAccessToken.Invalid",
         description: "Expired access token is not valid.");

        public static readonly Error UserIdClaimInvalid = Error.Conflict(
        code: "Auth.UserIdClaim.Invalid",
        description: "Invalid userId claim.");

        public static readonly Error RefreshTokenExpired = Error.Conflict(
        code: "Auth.RefreshToken.Expired",
        description: "Refresh token is invalid or has expired.");

        public static Error ValuationSummaryNotFound = Error.NotFound(
            code: "ApplicationError.ValuationSummary.NotFound",
            description: "The specified valuation summary does not exist in the system."
        );

        public static Error TenantNotFound = Error.NotFound(
            code: "ApplicationError.Tenant.NotFound",
            description: "The specified tenant does not exist in the system."
        );

        public static Error ExternalReportsPriceNotFound = Error.NotFound(
            code: "ApplicationError.ExternalReportsPrice.NotFound",
            description: "The specified external reports price does not exist in the system."
        );

        public static Error ScoreNotFound = Error.NotFound(
            code: "ApplicationError.Score.NotFound",
            description: "The specified score does not exist in the system."
        );
    }
}

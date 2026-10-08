using JoFit.Domain.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace JoFit.API.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected long CurrentUserId => long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    /// <summary>The refresh-token session this access token came from (the "session_id" claim).</summary>
    protected long? CurrentSessionId => long.TryParse(User.FindFirst("session_id")?.Value, out var id) ? id : null;
    protected string? CurrentAccountRole => User.FindFirst("account_role")?.Value;

    /// <summary>
    /// Writes the API error envelope: { error: { code, message, fieldErrors } }.
    /// The HTTP status follows the first error's kind.
    /// </summary>
    protected IActionResult Problem(IReadOnlyCollection<Error> errors)
    {
        var first = errors.First();
        var (status, code) = first.ErrorType switch
        {
            ErrorKindsEnum.Validation => (StatusCodes.Status422UnprocessableEntity, "validation"),
            ErrorKindsEnum.NotFound => (StatusCodes.Status404NotFound, "not-found"),
            ErrorKindsEnum.Conflict => (StatusCodes.Status409Conflict, "conflict"),
            ErrorKindsEnum.Unauthorized => (StatusCodes.Status401Unauthorized, "unauthorized"),
            ErrorKindsEnum.Forbidden => (StatusCodes.Status403Forbidden, "forbidden"),
            ErrorKindsEnum.PayloadTooLarge => (StatusCodes.Status413PayloadTooLarge, "validation"),
            _ => (StatusCodes.Status500InternalServerError, "unknown")
        };

        var fieldErrors = errors
            .Where(e => e.ErrorType is ErrorKindsEnum.Validation or ErrorKindsEnum.Conflict)
            .Select(e => (Field: e.Field ?? (e.ErrorType == ErrorKindsEnum.Validation ? FieldFromCode(e.Code) : null), e.Description))
            .Where(x => x.Field is not null)
            .GroupBy(x => x.Field!)
            .ToDictionary(g => g.Key, g => g.First().Description);

        var error = new Dictionary<string, object> { ["code"] = code, ["message"] = first.Description };
        if (fieldErrors.Count > 0)
        {
            error["fieldErrors"] = fieldErrors;
        }

        var body = new { error };

        return new ObjectResult(body)
        {
            StatusCode = status,
            ContentTypes = { "application/json" }
        };
    }

    // FluentValidation reports the property name as the code ("PageSize"); domain errors use
    // "Entity.Reason" codes and carry no field, so only single-token codes map to a field.
    private static string? FieldFromCode(string code)
        => code.Length > 0 && !code.Contains('.') && char.IsUpper(code[0])
            ? char.ToLowerInvariant(code[0]) + code[1..]
            : null;
}

using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Coaches;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.CoachPortal;

/// <summary>
/// What a coach sends to add or change a package. Prices are in fils, like every other amount.
/// Send <c>Name</c> to use one name for both languages, or <c>NameEn</c> and <c>NameAr</c> to set them apart.
/// </summary>
public sealed record CoachPackageBody(
    string? Name, string? NameEn, string? NameAr, int DurationMinutes, int SessionCount, string Mode, MoneyDto Price, MoneyDto? CompareAtPrice);

public sealed record CreateCoachPackageCommand(long UserId, string Language, CoachPackageBody Body) : IRequest<Result<CoachPortalPackageDto>>;

public sealed record UpdateCoachPackageCommand(long UserId, string Language, long PackageId, CoachPackageBody Body) : IRequest<Result<CoachPortalPackageDto>>;

public sealed record DeleteCoachPackageCommand(long UserId, long PackageId) : IRequest<Result<Deleted>>;

internal static class CoachPackageSupport
{
    public static Task<Coach?> TrackedCoachAsync(IUnitOfWork unitOfWork, long userId, CancellationToken ct)
        => unitOfWork.GetRepository<Coach>().GetTableAsTracking().Include(c => c.Packages).FirstOrDefaultAsync(c => c.UserId == userId, ct);

    /// <summary>Turns the request into a package input, or says what is wrong with it. Nothing is written here.</summary>
    public static Result<CoachPackageInput> ToInput(CoachPackageBody body, long? id)
    {
        var nameEn = string.IsNullOrWhiteSpace(body.NameEn) ? body.Name : body.NameEn;
        var nameAr = string.IsNullOrWhiteSpace(body.NameAr) ? body.Name : body.NameAr;
        var errors = new List<Error>();
        if (body.Price is null) errors.Add(CoachErrors.Field("price", "The price is required."));

        // A single "name" is reported on "name"; the two-language form is reported on name.en / name.ar.
        var named = !string.IsNullOrWhiteSpace(body.Name) || !string.IsNullOrWhiteSpace(body.NameEn) || !string.IsNullOrWhiteSpace(body.NameAr);
        var singleName = string.IsNullOrWhiteSpace(body.NameEn) && string.IsNullOrWhiteSpace(body.NameAr);
        if (!named && singleName) errors.Add(CoachErrors.Field("name", "The package name is required."));

        var input = new CoachPackageInput(
            id, nameEn?.Trim() ?? string.Empty, nameAr?.Trim() ?? string.Empty, body.DurationMinutes, body.SessionCount,
            body.Mode ?? string.Empty, body.Price?.Amount ?? 0, body.CompareAtPrice?.Amount);

        var rules = CoachPackageRules.Validate(input, string.Empty);
        // The "name" error above already covers a missing name, so the per-language ones are dropped.
        errors.AddRange(named ? rules : rules.Where(e => e.Field is not ("name.en" or "name.ar")));
        return errors.Count > 0 ? errors : input;
    }

    public static CoachPortalPackageDto ToDto(CoachPackage p, string language, ILocalizationService localization)
        => new(p.Id, localization.GetLocalizedName(p.NameAr, p.NameEn, language), p.DurationMinutes, p.SessionCount, p.Mode,
            new MoneyDto(p.PriceAmount), p.CompareAtAmount is long c ? new MoneyDto(c) : null);
}

public class CreateCoachPackageCommandHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<CreateCoachPackageCommand, Result<CoachPortalPackageDto>>
{
    public async Task<Result<CoachPortalPackageDto>> Handle(CreateCoachPackageCommand request, CancellationToken ct)
    {
        var coach = await CoachPackageSupport.TrackedCoachAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var input = CoachPackageSupport.ToInput(request.Body, null);
        if (input.IsError)
        {
            return input.Errors;
        }

        var position = coach.Packages.Count == 0 ? 0 : coach.Packages.Max(p => p.Position) + 1;
        var package = CoachPackage.Create(input.Value, position);
        coach.Packages.Add(package);
        await unitOfWork.SaveChangesAsync(ct);

        return CoachPackageSupport.ToDto(package, request.Language, localization);
    }
}

public class UpdateCoachPackageCommandHandler(IUnitOfWork unitOfWork, ILocalizationService localization)
    : IRequestHandler<UpdateCoachPackageCommand, Result<CoachPortalPackageDto>>
{
    public async Task<Result<CoachPortalPackageDto>> Handle(UpdateCoachPackageCommand request, CancellationToken ct)
    {
        var coach = await CoachPackageSupport.TrackedCoachAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        // Only this coach's own packages are searched, so another coach's id looks like any unknown id.
        var package = coach.Packages.FirstOrDefault(p => p.Id == request.PackageId);
        if (package is null)
        {
            return CoachErrors.PackageNotFound;
        }

        var input = CoachPackageSupport.ToInput(request.Body, package.Id);
        if (input.IsError)
        {
            return input.Errors;
        }

        package.Apply(input.Value, package.Position);
        await unitOfWork.SaveChangesAsync(ct);

        return CoachPackageSupport.ToDto(package, request.Language, localization);
    }
}

public class DeleteCoachPackageCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCoachPackageCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCoachPackageCommand request, CancellationToken ct)
    {
        var coach = await CoachPackageSupport.TrackedCoachAsync(unitOfWork, request.UserId, ct);
        if (coach is null)
        {
            return CoachErrors.NotFound;
        }

        var package = coach.Packages.FirstOrDefault(p => p.Id == request.PackageId);
        if (package is null)
        {
            return CoachErrors.PackageNotFound;
        }

        // Soft delete, so past sessions still show the package name.
        unitOfWork.GetRepository<CoachPackage>().DeleteAsync(package);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}

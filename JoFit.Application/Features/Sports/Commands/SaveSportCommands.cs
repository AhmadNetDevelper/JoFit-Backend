using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Sports.Dtos;
using JoFit.Domain.Sports;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Sports.Commands;

public sealed record CreateSportCommand(SportValues Values) : IRequest<Result<SportDto>>;

public sealed record UpdateSportCommand(long Id, SportValues Values) : IRequest<Result<SportDto>>;

public sealed record DeleteSportCommand(long Id) : IRequest<Result<Deleted>>;

public sealed record SetSportFeaturedCommand(long Id, bool IsFeatured) : IRequest<Result<SportDto>>;

public class CreateSportCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSportCommand, Result<SportDto>>
{
    public async Task<Result<SportDto>> Handle(CreateSportCommand request, CancellationToken ct)
    {
        var created = Sport.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<Sport>();
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue, ct))
        {
            return SportErrors.SlugTaken;
        }

        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return SportDto.From(entity);
    }
}

public class UpdateSportCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSportCommand, Result<SportDto>>
{
    public async Task<Result<SportDto>> Handle(UpdateSportCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Sport>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return SportErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var slugValue = entity.Slug;
        if (slugValue is not null && await repository.GetTableNoTracking().AnyAsync(x => x.Slug == slugValue && x.Id != entity.Id, ct))
        {
            return SportErrors.SlugTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return SportDto.From(entity);
    }
}

public class DeleteSportCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSportCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteSportCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<Sport>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return SportErrors.NotFound;
        }

        var usedProductSportLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Products.ProductSportLink>().CountAsync(x => x.SportId == entity.Id, ct);
        if (usedProductSportLink > 0)
        {
            return Error.Conflict("Sport.InUse", $"This sport is used by {usedProductSportLink} product(s). Move them first.");
        }

        var usedCoachSportLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Coaches.CoachSportLink>().CountAsync(x => x.SportId == entity.Id, ct);
        if (usedCoachSportLink > 0)
        {
            return Error.Conflict("Sport.InUse", $"This sport is used by {usedCoachSportLink} coach(es). Move them first.");
        }

        var usedArticleSportLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Articles.ArticleSportLink>().CountAsync(x => x.SportId == entity.Id, ct);
        if (usedArticleSportLink > 0)
        {
            return Error.Conflict("Sport.InUse", $"This sport is used by {usedArticleSportLink} article(s). Move them first.");
        }

        var usedJobSportLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Careers.JobSportLink>().CountAsync(x => x.SportId == entity.Id, ct);
        if (usedJobSportLink > 0)
        {
            return Error.Conflict("Sport.InUse", $"This sport is used by {usedJobSportLink} job(s). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class SetSportFeaturedCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetSportFeaturedCommand, Result<SportDto>>
{
    public async Task<Result<SportDto>> Handle(SetSportFeaturedCommand request, CancellationToken ct)
    {
        var entity = await unitOfWork.GetRepository<Sport>().GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return SportErrors.NotFound;
        }

        entity.SetFeatured(request.IsFeatured);
        await unitOfWork.SaveChangesAsync(ct);
        return SportDto.From(entity);
    }
}

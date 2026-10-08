using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.CoachSpecialisations.Dtos;
using JoFit.Domain.CoachSpecialisations;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.CoachSpecialisations.Commands;

public sealed record CreateCoachSpecialisationCommand(CoachSpecialisationValues Values) : IRequest<Result<CoachSpecialisationDto>>;

public sealed record UpdateCoachSpecialisationCommand(long Id, CoachSpecialisationValues Values) : IRequest<Result<CoachSpecialisationDto>>;

public sealed record DeleteCoachSpecialisationCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateCoachSpecialisationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCoachSpecialisationCommand, Result<CoachSpecialisationDto>>
{
    public async Task<Result<CoachSpecialisationDto>> Handle(CreateCoachSpecialisationCommand request, CancellationToken ct)
    {
        var created = CoachSpecialisation.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<CoachSpecialisation>();
        var nameEnValue = entity.NameEn.ToLower();
        var nameArValue = entity.NameAr.ToLower();
        if (await repository.GetTableNoTracking().AnyAsync(x => (x.NameEn.ToLower() == nameEnValue || x.NameAr.ToLower() == nameArValue), ct))
        {
            return CoachSpecialisationErrors.NameTaken;
        }

        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return CoachSpecialisationDto.From(entity);
    }
}

public class UpdateCoachSpecialisationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCoachSpecialisationCommand, Result<CoachSpecialisationDto>>
{
    public async Task<Result<CoachSpecialisationDto>> Handle(UpdateCoachSpecialisationCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<CoachSpecialisation>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return CoachSpecialisationErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var nameEnValue = entity.NameEn.ToLower();
        var nameArValue = entity.NameAr.ToLower();
        if (await repository.GetTableNoTracking().AnyAsync(x => (x.NameEn.ToLower() == nameEnValue || x.NameAr.ToLower() == nameArValue) && x.Id != entity.Id, ct))
        {
            return CoachSpecialisationErrors.NameTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return CoachSpecialisationDto.From(entity);
    }
}

public class DeleteCoachSpecialisationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCoachSpecialisationCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteCoachSpecialisationCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<CoachSpecialisation>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return CoachSpecialisationErrors.NotFound;
        }

        var usedCoachSpecialisationLink = await unitOfWork.GetDbContext().Set<JoFit.Domain.Coaches.CoachSpecialisationLink>().CountAsync(x => x.CoachSpecialisationId == entity.Id, ct);
        if (usedCoachSpecialisationLink > 0)
        {
            return Error.Conflict("CoachSpecialisation.InUse", $"This coach specialisation is used by {usedCoachSpecialisationLink} coach(es). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

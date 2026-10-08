using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.RentalDepots.Dtos;
using JoFit.Domain.RentalDepots;
using JoFit.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.RentalDepots.Commands;

public sealed record CreateRentalDepotCommand(RentalDepotValues Values) : IRequest<Result<RentalDepotDto>>;

public sealed record UpdateRentalDepotCommand(long Id, RentalDepotValues Values) : IRequest<Result<RentalDepotDto>>;

public sealed record DeleteRentalDepotCommand(long Id) : IRequest<Result<Deleted>>;

public class CreateRentalDepotCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateRentalDepotCommand, Result<RentalDepotDto>>
{
    public async Task<Result<RentalDepotDto>> Handle(CreateRentalDepotCommand request, CancellationToken ct)
    {
        var created = RentalDepot.Create(request.Values);
        if (created.IsError)
        {
            return created.Errors;
        }

        var entity = created.Value;
        var repository = unitOfWork.GetRepository<RentalDepot>();
        var nameEnValue = entity.NameEn.ToLower();
        if (await repository.GetTableNoTracking().AnyAsync(x => x.NameEn.ToLower() == nameEnValue, ct))
        {
            return RentalDepotErrors.NameTaken;
        }

        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return RentalDepotDto.From(entity);
    }
}

public class UpdateRentalDepotCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateRentalDepotCommand, Result<RentalDepotDto>>
{
    public async Task<Result<RentalDepotDto>> Handle(UpdateRentalDepotCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<RentalDepot>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return RentalDepotErrors.NotFound;
        }

        var updated = entity.Update(request.Values);
        if (updated.IsError)
        {
            return updated.Errors;
        }
        var nameEnValue = entity.NameEn.ToLower();
        if (await repository.GetTableNoTracking().AnyAsync(x => x.NameEn.ToLower() == nameEnValue && x.Id != entity.Id, ct))
        {
            return RentalDepotErrors.NameTaken;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return RentalDepotDto.From(entity);
    }
}

public class DeleteRentalDepotCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRentalDepotCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteRentalDepotCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<RentalDepot>();
        var entity = await repository.GetTableAsTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            return RentalDepotErrors.NotFound;
        }

        var usedRentalItem = await unitOfWork.GetDbContext().Set<JoFit.Domain.Rental.RentalItem>().CountAsync(x => x.DepotId == entity.Id, ct);
        if (usedRentalItem > 0)
        {
            return Error.Conflict("RentalDepot.InUse", $"This rental depot is used by {usedRentalItem} rental item(s). Move them first.");
        }

        // Records that reference this one add their own in-use check here when they exist.
        repository.DeleteAsync(entity);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

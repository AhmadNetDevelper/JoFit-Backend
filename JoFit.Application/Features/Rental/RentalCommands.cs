using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Services;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.RentalCategories;
using JoFit.Domain.RentalDepots;
using JoFit.Domain.Rental;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Rental;

public sealed record RentalItemInput(
    RentalItemDetails Details,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<GalleryItem>? Media);

public sealed record CreateRentalItemCommand(RentalItemInput Input) : IRequest<Result<AdminRentalItemDto>>;

public sealed record UpdateRentalItemCommand(long Id, RentalItemInput Input) : IRequest<Result<AdminRentalItemDto>>;

public sealed record DeleteRentalItemCommand(long Id) : IRequest<Result<Deleted>>;

public sealed record SetUnitsOutOfServiceCommand(long Id, int Units) : IRequest<Result<AdminRentalItemDto>>;

public sealed record SetRentalBookingStatusCommand(long Id, string Status) : IRequest<Result<AdminRentalBookingDto>>;

public sealed record ReleaseRentalDepositCommand(long Id) : IRequest<Result<AdminRentalBookingDto>>;

internal static class RentalSaving
{
    public const string OwnerType = "rental-item";
    public const string MediaField = "media";

    public static async Task<List<Error>> CheckReferencesAsync(IUnitOfWork unitOfWork, RentalItemInput input, CancellationToken ct)
    {
        var errors = new List<Error>();
        if (input.Details.DepotId > 0
            && !await unitOfWork.GetRepository<RentalDepot>().GetTableNoTracking().AnyAsync(d => d.Id == input.Details.DepotId, ct))
        {
            errors.Add(RentalErrors.DepotMissing);
        }

        var ids = input.CategoryIds.Distinct().ToList();
        if (ids.Count == 0
            || await unitOfWork.GetRepository<RentalCategory>().GetTableNoTracking().CountAsync(c => ids.Contains(c.Id), ct) != ids.Count)
        {
            errors.Add(RentalErrors.CategoriesMissing);
        }

        return errors;
    }

    public static async Task ReplaceCategoriesAsync(IUnitOfWork unitOfWork, long itemId, IReadOnlyList<long> categoryIds, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        db.RemoveRange(await db.Set<RentalItemCategoryLink>().Where(l => l.RentalItemId == itemId).ToListAsync(ct));
        db.AddRange(categoryIds.Distinct().Select(id => new RentalItemCategoryLink { RentalItemId = itemId, RentalCategoryId = id }));
    }

    public static async Task<AdminRentalItemDto> ToDtoAsync(IUnitOfWork unitOfWork, long id, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var item = await db.Set<RentalItem>().AsNoTracking().Include(i => i.Rates).FirstAsync(i => i.Id == id, ct);
        var categoryIds = await db.Set<RentalItemCategoryLink>().AsNoTracking().Where(l => l.RentalItemId == id).Select(l => l.RentalCategoryId).OrderBy(x => x).ToListAsync(ct);
        var media = await GalleryService.LoadAsync(unitOfWork, OwnerType, MediaField, [id], ct);
        return AdminRentalItemDto.From(item, categoryIds, media.GetValueOrDefault(id) ?? []);
    }

    public static async Task<AdminRentalBookingDto> BookingDtoAsync(IUnitOfWork unitOfWork, RentalBooking b, CancellationToken ct)
        => (await BookingDtosAsync(unitOfWork, [b], ct))[0];

    public static async Task<List<AdminRentalBookingDto>> BookingDtosAsync(IUnitOfWork unitOfWork, IReadOnlyList<RentalBooking> bookings, CancellationToken ct)
    {
        var db = unitOfWork.GetDbContext();
        var itemIds = bookings.Select(b => b.RentalItemId).Distinct().ToList();
        var customerIds = bookings.Where(b => b.CustomerId != null).Select(b => b.CustomerId!.Value).Distinct().ToList();
        var titles = await db.Set<RentalItem>().AsNoTracking().Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.TitleEn, ct);
        var customers = await db.Set<Customer>().AsNoTracking().Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return bookings.Select(b => new AdminRentalBookingDto(
            b.Id,
            b.Reference,
            b.RentalItemId,
            titles.GetValueOrDefault(b.RentalItemId) ?? string.Empty,
            b.CustomerId,
            b.CustomerId is long cid ? customers.GetValueOrDefault(cid) : null,
            b.OrderId,
            b.From.ToString("yyyy-MM-dd"),
            b.To.ToString("yyyy-MM-dd"),
            b.Unit,
            b.Quantity,
            b.Units,
            new Common.Models.MoneyDto(b.UnitPriceAmount),
            new Common.Models.MoneyDto(b.SubtotalAmount),
            b.DepositAmount > 0 ? new Common.Models.MoneyDto(b.DepositAmount) : null,
            b.DepositReleasedAtUtc is not null,
            b.Status,
            Courses.Catalogue.AuditDto.From(b))).ToList();
    }
}

public class CreateRentalItemCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<CreateRentalItemCommand, Result<AdminRentalItemDto>>
{
    public async Task<Result<AdminRentalItemDto>> Handle(CreateRentalItemCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var errors = await RentalSaving.CheckReferencesAsync(unitOfWork, input, ct);
        var created = RentalItem.Create(input.Details);
        if (created.IsError) errors.AddRange(created.Errors);

        var slug = input.Details.Slug?.Trim();
        if (!string.IsNullOrEmpty(slug) && await unitOfWork.GetRepository<RentalItem>().GetTableNoTracking().AnyAsync(i => i.Slug == slug, ct))
        {
            errors.Add(RentalErrors.SlugTaken);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var item = created.Value;
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await unitOfWork.GetRepository<RentalItem>().AddAsync(item, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await RentalSaving.ReplaceCategoriesAsync(unitOfWork, item.Id, input.CategoryIds, ct);

            if (input.Media is not null)
            {
                var gallery = await GalleryService.SyncAsync(unitOfWork, urlService, RentalSaving.OwnerType, item.Id, RentalSaving.MediaField, input.Media, ct);
                if (gallery.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return gallery.Errors;
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await RentalSaving.ToDtoAsync(unitOfWork, item.Id, ct);
    }
}

public class UpdateRentalItemCommandHandler(IUnitOfWork unitOfWork, IUrlService urlService)
    : IRequestHandler<UpdateRentalItemCommand, Result<AdminRentalItemDto>>
{
    public async Task<Result<AdminRentalItemDto>> Handle(UpdateRentalItemCommand request, CancellationToken ct)
    {
        var input = request.Input;
        var item = await unitOfWork.GetRepository<RentalItem>().GetTableAsTracking().Include(i => i.Rates).FirstOrDefaultAsync(i => i.Id == request.Id, ct);
        if (item is null)
        {
            return RentalErrors.ItemNotFound;
        }

        var errors = await RentalSaving.CheckReferencesAsync(unitOfWork, input, ct);
        var slug = input.Details.Slug?.Trim();
        if (!string.IsNullOrEmpty(slug) && await unitOfWork.GetRepository<RentalItem>().GetTableNoTracking().AnyAsync(i => i.Slug == slug && i.Id != item.Id, ct))
        {
            errors.Add(RentalErrors.SlugTaken);
        }

        // Rates are replaced wholesale: remove the old rows first so the unit index never collides.
        var oldRates = item.Rates.ToList();
        var applied = item.Apply(input.Details);
        if (applied.IsError) errors.AddRange(applied.Errors);
        if (errors.Count > 0)
        {
            return errors;
        }

        unitOfWork.GetDbContext().RemoveRange(oldRates);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await RentalSaving.ReplaceCategoriesAsync(unitOfWork, item.Id, input.CategoryIds, ct);
            if (input.Media is not null)
            {
                var gallery = await GalleryService.SyncAsync(unitOfWork, urlService, RentalSaving.OwnerType, item.Id, RentalSaving.MediaField, input.Media, ct);
                if (gallery.IsError)
                {
                    await unitOfWork.RollbackAsync(ct);
                    return gallery.Errors;
                }
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }

        return await RentalSaving.ToDtoAsync(unitOfWork, item.Id, ct);
    }
}

public class DeleteRentalItemCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteRentalItemCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteRentalItemCommand request, CancellationToken ct)
    {
        var repository = unitOfWork.GetRepository<RentalItem>();
        var item = await repository.GetTableAsTracking().FirstOrDefaultAsync(i => i.Id == request.Id, ct);
        if (item is null)
        {
            return RentalErrors.ItemNotFound;
        }

        var holding = RentalBookingStatuses.Holding;
        var live = await unitOfWork.GetRepository<RentalBooking>().GetTableNoTracking()
            .CountAsync(b => b.RentalItemId == item.Id && holding.Contains(b.Status), ct);
        if (live > 0)
        {
            return RentalErrors.ItemInUse(live);
        }

        repository.DeleteAsync(item);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class SetUnitsOutOfServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetUnitsOutOfServiceCommand, Result<AdminRentalItemDto>>
{
    public async Task<Result<AdminRentalItemDto>> Handle(SetUnitsOutOfServiceCommand request, CancellationToken ct)
    {
        var item = await unitOfWork.GetRepository<RentalItem>().GetTableAsTracking().FirstOrDefaultAsync(i => i.Id == request.Id, ct);
        if (item is null)
        {
            return RentalErrors.ItemNotFound;
        }

        var result = item.SetUnitsOutOfService(request.Units);
        if (result.IsError)
        {
            return result.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await RentalSaving.ToDtoAsync(unitOfWork, item.Id, ct);
    }
}

public class SetRentalBookingStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetRentalBookingStatusCommand, Result<AdminRentalBookingDto>>
{
    public async Task<Result<AdminRentalBookingDto>> Handle(SetRentalBookingStatusCommand request, CancellationToken ct)
    {
        var booking = await unitOfWork.GetRepository<RentalBooking>().GetTableAsTracking().FirstOrDefaultAsync(b => b.Id == request.Id, ct);
        if (booking is null)
        {
            return RentalErrors.BookingNotFound;
        }

        var moved = booking.MoveTo(request.Status);
        if (moved.IsError)
        {
            return moved.Errors;
        }

        if (booking.CustomerId is long customerId)
        {
            Notifications.NotificationEvents.RentalStatusMoved(unitOfWork, customerId, booking.Reference, request.Status);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await RentalSaving.BookingDtoAsync(unitOfWork, booking, ct);
    }
}

public class ReleaseRentalDepositCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ReleaseRentalDepositCommand, Result<AdminRentalBookingDto>>
{
    public async Task<Result<AdminRentalBookingDto>> Handle(ReleaseRentalDepositCommand request, CancellationToken ct)
    {
        var booking = await unitOfWork.GetRepository<RentalBooking>().GetTableAsTracking().FirstOrDefaultAsync(b => b.Id == request.Id, ct);
        if (booking is null)
        {
            return RentalErrors.BookingNotFound;
        }

        var released = booking.ReleaseDeposit();
        if (released.IsError)
        {
            return released.Errors;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await RentalSaving.BookingDtoAsync(unitOfWork, booking, ct);
    }
}

using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Notifications;

public sealed record NotificationItemDto(long Id, string Kind, string Title, string Body, string Href, DateTimeOffset CreatedAt, bool IsRead);

public sealed record NotificationFeedDto(IReadOnlyList<NotificationItemDto> Items, int UnreadCount);

public sealed record StaffNotificationItemDto(long Id, string Kind, LocalizedText Title, LocalizedText Body, string Href, DateTimeOffset CreatedAt, bool IsRead);

public sealed record StaffNotificationFeedDto(IReadOnlyList<StaffNotificationItemDto> Items, int UnreadCount);

public sealed record GetMyNotificationsQuery(long UserId, string Language) : IRequest<Result<NotificationFeedDto>>;

/// <summary>Ids null (or empty) marks everything read.</summary>
public sealed record MarkMyNotificationsReadCommand(long UserId, IReadOnlyList<long>? Ids) : IRequest<Result<Deleted>>;

public sealed record GetStaffNotificationsQuery : IRequest<Result<StaffNotificationFeedDto>>;

public sealed record MarkStaffNotificationsReadCommand(IReadOnlyList<long>? Ids) : IRequest<Result<Deleted>>;

internal static class NotificationScope
{
    /// <summary>The audience and recipient id of the signed-in account (customer, coach or employer).</summary>
    public static async Task<(string Audience, long RecipientId)?> OfAsync(IAccountService accounts, long userId, CancellationToken ct)
    {
        var session = await accounts.GetSessionAsync(userId, ct);
        if (session.IsError)
        {
            return null;
        }

        var account = session.Value.Account;
        return account.Role switch
        {
            "customer" when account.CustomerId is long id => (NotificationAudiences.Customer, id),
            "coach" when account.CoachId is long id => (NotificationAudiences.Coach, id),
            "employer" when account.EmployerId is long id => (NotificationAudiences.Employer, id),
            _ => null
        };
    }
}

public class GetMyNotificationsQueryHandler(IUnitOfWork unitOfWork, IAccountService accounts, ILocalizationService localization)
    : IRequestHandler<GetMyNotificationsQuery, Result<NotificationFeedDto>>
{
    public async Task<Result<NotificationFeedDto>> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        var scope = await NotificationScope.OfAsync(accounts, request.UserId, ct);
        if (scope is null)
        {
            return Error.Unauthorized("Notification.NoAccount", "Sign in to continue.");
        }

        var (audience, recipientId) = scope.Value;
        var mine = unitOfWork.GetRepository<Notification>().GetTableNoTracking().Where(n => n.Audience == audience && n.RecipientId == recipientId);
        var rows = await mine.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id).Take(30).ToListAsync(ct);
        var unread = await mine.CountAsync(n => n.ReadAtUtc == null, ct);

        return new NotificationFeedDto(
            rows.Select(n => new NotificationItemDto(
                n.Id, n.Kind,
                localization.GetLocalizedName(n.TitleAr, n.TitleEn, request.Language),
                localization.GetLocalizedName(n.BodyAr, n.BodyEn, request.Language),
                n.Href, n.CreatedAtUtc, n.ReadAtUtc != null)).ToList(),
            unread);
    }
}

public class MarkMyNotificationsReadCommandHandler(IUnitOfWork unitOfWork, IAccountService accounts)
    : IRequestHandler<MarkMyNotificationsReadCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(MarkMyNotificationsReadCommand request, CancellationToken ct)
    {
        var scope = await NotificationScope.OfAsync(accounts, request.UserId, ct);
        if (scope is null)
        {
            return Error.Unauthorized("Notification.NoAccount", "Sign in to continue.");
        }

        var (audience, recipientId) = scope.Value;
        var query = unitOfWork.GetRepository<Notification>().GetTableAsTracking()
            .Where(n => n.Audience == audience && n.RecipientId == recipientId && n.ReadAtUtc == null);
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(n => ids.Contains(n.Id));
        }

        (await query.ToListAsync(ct)).ForEach(n => n.MarkRead());
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

public class GetStaffNotificationsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetStaffNotificationsQuery, Result<StaffNotificationFeedDto>>
{
    public async Task<Result<StaffNotificationFeedDto>> Handle(GetStaffNotificationsQuery request, CancellationToken ct)
    {
        var team = unitOfWork.GetRepository<Notification>().GetTableNoTracking().Where(n => n.Audience == NotificationAudiences.Staff);
        var rows = await team.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id).Take(50).ToListAsync(ct);
        var unread = await team.CountAsync(n => n.ReadAtUtc == null, ct);

        return new StaffNotificationFeedDto(
            rows.Select(n => new StaffNotificationItemDto(
                n.Id, n.Kind, new LocalizedText(n.TitleEn, n.TitleAr), new LocalizedText(n.BodyEn, n.BodyAr), n.Href, n.CreatedAtUtc, n.ReadAtUtc != null)).ToList(),
            unread);
    }
}

public class MarkStaffNotificationsReadCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<MarkStaffNotificationsReadCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(MarkStaffNotificationsReadCommand request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<Notification>().GetTableAsTracking()
            .Where(n => n.Audience == NotificationAudiences.Staff && n.ReadAtUtc == null);
        if (request.Ids is { Count: > 0 })
        {
            var ids = request.Ids;
            query = query.Where(n => ids.Contains(n.Id));
        }

        // One person reading it clears it for the whole team.
        (await query.ToListAsync(ct)).ForEach(n => n.MarkRead());
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

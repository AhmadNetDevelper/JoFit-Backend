using JoFit.Application.Common.Interfaces;
using JoFit.Domain.Notifications;

namespace JoFit.Application.Common.Services;

/// <summary>
/// Writes notifications into the current unit of work. It only adds them; the caller saves, so a notification
/// is stored in the same transaction as the event it describes (and is never left behind if the event fails).
/// </summary>
public static class NotificationWriter
{
    public static void ToCustomer(IUnitOfWork uow, long customerId, string kind, (string En, string Ar) title, (string En, string Ar) body, string href)
        => Add(uow, NotificationAudiences.Customer, customerId, kind, title, body, href);

    public static void ToCoach(IUnitOfWork uow, long coachId, string kind, (string En, string Ar) title, (string En, string Ar) body, string href)
        => Add(uow, NotificationAudiences.Coach, coachId, kind, title, body, href);

    public static void ToEmployer(IUnitOfWork uow, long employerId, string kind, (string En, string Ar) title, (string En, string Ar) body, string href)
        => Add(uow, NotificationAudiences.Employer, employerId, kind, title, body, href);

    public static void ToStaff(IUnitOfWork uow, string kind, (string En, string Ar) title, (string En, string Ar) body, string href)
        => Add(uow, NotificationAudiences.Staff, null, kind, title, body, href);

    private static void Add(IUnitOfWork uow, string audience, long? recipientId, string kind, (string En, string Ar) title, (string En, string Ar) body, string href)
        => uow.GetDbContext().Add(Notification.Create(audience, recipientId, kind, title.En, title.Ar, body.En, body.Ar, href));
}

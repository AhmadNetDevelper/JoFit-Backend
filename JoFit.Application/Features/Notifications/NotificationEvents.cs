using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Services;
using JoFit.Domain.Notifications;

namespace JoFit.Application.Features.Notifications;

/// <summary>The wording of each event, in both languages. Hrefs carry no locale prefix.</summary>
public static class NotificationEvents
{
    private static (string En, string Ar) T(string en, string ar) => (en, ar);

    private static (string En, string Ar) StatusWord(string status) => status switch
    {
        "confirmed" => T("confirmed", "تم تأكيده"),
        "processing" => T("being prepared", "قيد التجهيز"),
        "shipped" => T("on its way", "في الطريق إليك"),
        "completed" => T("completed", "مكتمل"),
        "cancelled" => T("cancelled", "ملغي"),
        "refunded" => T("refunded", "تم استرداده"),
        "collected" => T("collected", "تم استلامه"),
        "returned" => T("returned", "تم إرجاعه"),
        "pending" => T("pending", "قيد الانتظار"),
        _ => T(status, status)
    };

    // ---- orders ---------------------------------------------------------

    public static void OrderPlaced(IUnitOfWork uow, long customerId, long orderId, string reference, string customerName)
    {
        NotificationWriter.ToCustomer(uow, customerId, NotificationKinds.OrderPlaced,
            T("Reservation received", "تم استلام حجزك"),
            T($"We received order {reference}. We will confirm it shortly; you pay on collection or delivery.",
              $"استلمنا الطلب {reference}. سنؤكده قريباً، ويتم الدفع عند الاستلام أو التوصيل."),
            "/account/orders");
        NotificationWriter.ToStaff(uow, NotificationKinds.OrderPlaced,
            T("New order", "طلب جديد"),
            T($"{customerName} placed order {reference}.", $"{customerName} أنشأ الطلب {reference}."),
            $"/orders/{orderId}");
    }

    public static void OrderStatusMoved(IUnitOfWork uow, long customerId, string reference, string status)
    {
        var word = StatusWord(status);
        NotificationWriter.ToCustomer(uow, customerId, NotificationKinds.OrderStatus,
            T($"Order {reference} is {word.En}", $"الطلب {reference} {word.Ar}"),
            T($"Your order {reference} is now {word.En}.", $"أصبح طلبك {reference} {word.Ar}."),
            "/account/orders");
    }

    public static void OrderCancelledByCustomer(IUnitOfWork uow, long orderId, string reference, string customerName)
        => NotificationWriter.ToStaff(uow, NotificationKinds.OrderStatus,
            T("Order cancelled by the customer", "ألغى العميل الطلب"),
            T($"{customerName} cancelled order {reference}.", $"{customerName} ألغى الطلب {reference}."),
            $"/orders/{orderId}");

    // ---- rental -----------------------------------------------------------

    public static void RentalStatusMoved(IUnitOfWork uow, long customerId, string reference, string status)
    {
        var word = StatusWord(status);
        NotificationWriter.ToCustomer(uow, customerId, NotificationKinds.RentalStatus,
            T($"Hire {reference} is {word.En}", $"الحجز {reference} {word.Ar}"),
            T($"Your hire {reference} is now {word.En}.", $"أصبح حجز التأجير {reference} {word.Ar}."),
            "/account/rentals");
    }

    // ---- coaching sessions ---------------------------------------------------

    public static void SessionRequested(IUnitOfWork uow, long coachId, long bookingId, string reference, string whoName)
    {
        NotificationWriter.ToCoach(uow, coachId, NotificationKinds.SessionRequested,
            T("New session request", "طلب جلسة جديد"),
            T($"{whoName} requested session {reference}.", $"{whoName} طلب الجلسة {reference}."),
            "/coach/sessions");
        NotificationWriter.ToStaff(uow, NotificationKinds.SessionRequested,
            T("Session requested", "تم طلب جلسة"),
            T($"{whoName} requested session {reference}.", $"{whoName} طلب الجلسة {reference}."),
            $"/coaching-sessions/{bookingId}");
    }

    /// <summary>Tells the customer, and the coach too when staff made the change.</summary>
    public static void SessionStatusMoved(IUnitOfWork uow, long? customerId, long coachId, string reference, string status, bool byStaff)
    {
        var word = StatusWord(status);
        if (customerId is long cid)
        {
            NotificationWriter.ToCustomer(uow, cid, NotificationKinds.SessionStatus,
                T($"Your session is {word.En}", $"جلستك {word.Ar}"),
                T($"Session {reference} is now {word.En}.", $"أصبحت الجلسة {reference} {word.Ar}."),
                "/account/sessions");
        }

        if (byStaff)
        {
            NotificationWriter.ToCoach(uow, coachId, NotificationKinds.SessionStatus,
                T($"Session {reference} is {word.En}", $"الجلسة {reference} {word.Ar}"),
                T($"JoFit staff set session {reference} to {word.En}.", $"حدّد فريق JoFit الجلسة {reference} كـ {word.Ar}."),
                "/coach/sessions");
        }
    }

    public static void SessionCancelledByClient(IUnitOfWork uow, long coachId, string reference)
        => NotificationWriter.ToCoach(uow, coachId, NotificationKinds.SessionStatus,
            T("Session cancelled by the client", "ألغى العميل الجلسة"),
            T($"The client cancelled session {reference}.", $"ألغى العميل الجلسة {reference}."),
            "/coach/sessions");
}

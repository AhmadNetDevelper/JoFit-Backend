using System.Security.Cryptography;
using System.Text;
using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Common.Services;
using JoFit.Application.Features.Auth;
using JoFit.Application.Features.Notifications;
using JoFit.Application.Features.Reviews;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Notifications;
using JoFit.Domain.Support;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Support;

public sealed record SupportMessageDto(long Id, string Sender, string AuthorName, string Body, DateTimeOffset SentAt);

public sealed record ConversationSummaryDto(
    long Id, string Reference, string Status, string Name, string Email, long? CustomerId, long? AccountId, long? AssigneeId,
    string? LastMessage, DateTimeOffset LastMessageAt, int Unread, DateTimeOffset CreatedAt);

/// <summary>What the visitor's chat window reads: their latest conversation (or none), its messages and what is unread.</summary>
public sealed record SupportThreadDto(ConversationSummaryDto? Conversation, IReadOnlyList<SupportMessageDto> Messages, int Unread);

public sealed record SupportSendResultDto(SupportThreadDto Thread, string? GuestToken);

/// <summary>Who is talking: a signed-in account, or a guest holding the token issued with their first message.</summary>
public sealed record SupportVisitor(long? UserId, string? GuestToken);

public sealed record GetSupportThreadQuery(SupportVisitor Visitor) : IRequest<Result<SupportThreadDto>>;

public sealed record SendSupportMessageCommand(SupportVisitor Visitor, string? Body, string? Name, string? Email) : IRequest<Result<SupportSendResultDto>>;

public sealed record MarkSupportReadCommand(SupportVisitor Visitor) : IRequest<Result<Deleted>>;

public sealed record GetSupportInboxQuery(int Page, int PageSize, string? Status, string? Search) : IRequest<Result<PagedResult<ConversationSummaryDto>>>;

public sealed record CountUnreadSupportQuery : IRequest<Result<CountDto>>;

public sealed record OpenSupportConversationQuery(long Id) : IRequest<Result<SupportThreadDto>>;

public sealed record ReplySupportCommand(long Id, long StaffUserId, string? Body) : IRequest<Result<SupportMessageDto>>;

public sealed record SetSupportStatusCommand(long Id, string? Status) : IRequest<Result<ConversationSummaryDto>>;

internal static class SupportSupport
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>The visitor's conversations: by account when signed in, by token when a guest. Never anyone else's.</summary>
    public static IQueryable<SupportConversation> Mine(IUnitOfWork unitOfWork, SupportVisitor visitor)
    {
        var all = unitOfWork.GetRepository<SupportConversation>().GetTableAsTracking().Include(c => c.Messages);
        if (visitor.UserId is long userId)
        {
            return all.Where(c => c.AccountUserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(visitor.GuestToken))
        {
            var hash = Hash(visitor.GuestToken);
            return all.Where(c => c.GuestTokenHash == hash);
        }

        return all.Where(c => false);
    }

    /// <summary>Visitor messages staff have not seen yet.</summary>
    public static int StaffUnread(SupportConversation c)
        => c.Messages.Count(m => m.Sender == SupportSenders.Visitor && (c.StaffReadAtUtc is null || m.SentAtUtc > c.StaffReadAtUtc));

    /// <summary>Staff messages the visitor has not seen yet.</summary>
    public static int VisitorUnread(SupportConversation c)
        => c.Messages.Count(m => m.Sender == SupportSenders.Staff && (c.VisitorReadAtUtc is null || m.SentAtUtc > c.VisitorReadAtUtc));

    public static ConversationSummaryDto Summary(SupportConversation c, int unread)
        => new(
            c.Id, c.Reference, c.Status, c.VisitorName, c.VisitorEmail, c.CustomerId, c.AccountUserId, c.AssigneeId,
            c.Messages.OrderByDescending(m => m.SentAtUtc).FirstOrDefault()?.Body, c.LastMessageAtUtc, unread, c.CreatedAtUtc);

    public static SupportMessageDto Message(SupportMessage m) => new(m.Id, m.Sender, m.AuthorName, m.Body, m.SentAtUtc);

    public static SupportThreadDto VisitorThread(SupportConversation? c)
        => c is null
            ? new SupportThreadDto(null, [], 0)
            : new SupportThreadDto(Summary(c, StaffUnread(c)), c.Messages.OrderBy(m => m.SentAtUtc).ThenBy(m => m.Id).Select(Message).ToList(), VisitorUnread(c));
}

public class GetSupportThreadQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSupportThreadQuery, Result<SupportThreadDto>>
{
    public async Task<Result<SupportThreadDto>> Handle(GetSupportThreadQuery request, CancellationToken ct)
    {
        // No account and no token answers an empty thread, never someone else's conversation.
        var latest = await SupportSupport.Mine(unitOfWork, request.Visitor).OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct);
        return SupportSupport.VisitorThread(latest);
    }
}

public class SendSupportMessageCommandHandler(IUnitOfWork unitOfWork, IAccountService accounts)
    : IRequestHandler<SendSupportMessageCommand, Result<SupportSendResultDto>>
{
    public async Task<Result<SupportSendResultDto>> Handle(SendSupportMessageCommand request, CancellationToken ct)
    {
        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length is < 2 or > 2000)
        {
            return SupportErrors.BodyLength;
        }

        var visitor = request.Visitor;
        var latest = await SupportSupport.Mine(unitOfWork, visitor).OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct);
        string? issuedToken = null;
        SupportConversation conversation;

        if (latest is not null && latest.Status == SupportStatuses.Open)
        {
            conversation = latest;
        }
        else
        {
            // A new conversation: when none exists yet, or the last one was closed.
            string name, email;
            long? customerId = null;
            string? tokenHash = null;

            if (visitor.UserId is long userId)
            {
                var session = await accounts.GetSessionAsync(userId, ct);
                if (session.IsError)
                {
                    return session.Errors;
                }

                name = session.Value.Account.Name;
                email = session.Value.Account.Email;
                customerId = session.Value.Account.CustomerId;
            }
            else if (latest is not null)
            {
                // A returning guest keeps the old token and is told nothing new.
                name = latest.VisitorName;
                email = latest.VisitorEmail;
                tokenHash = latest.GuestTokenHash;
            }
            else
            {
                name = AccountRules.CollapseWhitespace(request.Name);
                email = request.Email?.Trim() ?? string.Empty;
                if (name.Length < 2)
                {
                    return SupportErrors.GuestName;
                }

                if (!AccountRules.IsValidEmail(email))
                {
                    return SupportErrors.GuestEmail;
                }

                issuedToken = SupportSupport.NewToken();
                tokenHash = SupportSupport.Hash(issuedToken);
            }

            conversation = SupportConversation.Start(visitor.UserId, customerId, tokenHash, name, email);
            await unitOfWork.GetRepository<SupportConversation>().AddAsync(conversation, ct);
        }

        // One staff notification per conversation while it is unread.
        var alreadyUnread = conversation.Id != 0 && SupportSupport.StaffUnread(conversation) > 0;
        conversation.Add(SupportSenders.Visitor, null, conversation.VisitorName, body);
        await unitOfWork.SaveChangesAsync(ct);

        if (!alreadyUnread)
        {
            NotificationWriter.ToStaff(unitOfWork, NotificationKinds.SupportMessage,
                ("New support message", "رسالة دعم جديدة"),
                ($"{conversation.VisitorName} wrote in conversation {conversation.Reference}.", $"{conversation.VisitorName} كتب في المحادثة {conversation.Reference}."),
                $"/support/{conversation.Id}");
            await unitOfWork.SaveChangesAsync(ct);
        }

        return new SupportSendResultDto(SupportSupport.VisitorThread(conversation), issuedToken);
    }
}

public class MarkSupportReadCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<MarkSupportReadCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(MarkSupportReadCommand request, CancellationToken ct)
    {
        var latest = await SupportSupport.Mine(unitOfWork, request.Visitor).OrderByDescending(c => c.Id).FirstOrDefaultAsync(ct);
        if (latest is not null)
        {
            latest.MarkReadByVisitor();
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Deleted;
    }
}

public class GetSupportInboxQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSupportInboxQuery, Result<PagedResult<ConversationSummaryDto>>>
{
    public async Task<Result<PagedResult<ConversationSummaryDto>>> Handle(GetSupportInboxQuery request, CancellationToken ct)
    {
        var query = unitOfWork.GetRepository<SupportConversation>().GetTableNoTracking();

        // Any status other than open or closed means all.
        if (request.Status is SupportStatuses.Open or SupportStatuses.Closed)
        {
            var status = request.Status;
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var numeric = new string(term.Where(char.IsDigit).ToArray());
            var id = long.TryParse(numeric, out var parsed) ? parsed : -1;
            query = query.Where(c => c.Id == id || c.VisitorName.ToLower().Contains(term) || c.VisitorEmail.ToLower().Contains(term));
        }

        var page = await query.OrderByDescending(c => c.LastMessageAtUtc).ThenByDescending(c => c.Id).ToPagedResultAsync(request.Page, request.PageSize, ct);
        var ids = page.Items.Select(c => c.Id).ToList();
        var full = await unitOfWork.GetRepository<SupportConversation>().GetTableNoTracking().Include(c => c.Messages).Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        var items = ids.Select(id => full.First(c => c.Id == id)).Select(c => SupportSupport.Summary(c, SupportSupport.StaffUnread(c))).ToList();
        return new PagedResult<ConversationSummaryDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}

public class CountUnreadSupportQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<CountUnreadSupportQuery, Result<CountDto>>
{
    public async Task<Result<CountDto>> Handle(CountUnreadSupportQuery request, CancellationToken ct)
    {
        // Counts conversations with something unread, not messages.
        var all = await unitOfWork.GetRepository<SupportConversation>().GetTableNoTracking().Include(c => c.Messages).ToListAsync(ct);
        return new CountDto(all.Count(c => SupportSupport.StaffUnread(c) > 0));
    }
}

public class OpenSupportConversationQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<OpenSupportConversationQuery, Result<SupportThreadDto>>
{
    public async Task<Result<SupportThreadDto>> Handle(OpenSupportConversationQuery request, CancellationToken ct)
    {
        var conversation = await unitOfWork.GetRepository<SupportConversation>().GetTableAsTracking().Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (conversation is null)
        {
            return SupportErrors.NotFound;
        }

        // Opening a conversation marks it seen by staff.
        var unreadBefore = SupportSupport.StaffUnread(conversation);
        conversation.MarkReadByStaff();
        await unitOfWork.SaveChangesAsync(ct);
        return new SupportThreadDto(
            SupportSupport.Summary(conversation, 0),
            conversation.Messages.OrderBy(m => m.SentAtUtc).ThenBy(m => m.Id).Select(SupportSupport.Message).ToList(),
            unreadBefore);
    }
}

public class ReplySupportCommandHandler(IUnitOfWork unitOfWork, IStaffService staff)
    : IRequestHandler<ReplySupportCommand, Result<SupportMessageDto>>
{
    public async Task<Result<SupportMessageDto>> Handle(ReplySupportCommand request, CancellationToken ct)
    {
        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length is < 1 or > 2000)
        {
            return SupportErrors.ReplyLength;
        }

        var conversation = await unitOfWork.GetRepository<SupportConversation>().GetTableAsTracking().Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (conversation is null)
        {
            return SupportErrors.NotFound;
        }

        var author = await staff.GetSessionAsync(request.StaffUserId, ct);
        var message = conversation.Add(SupportSenders.Staff, request.StaffUserId, author.IsError ? "JoFit" : author.Value.User.Name, body);
        conversation.MarkReadByStaff();

        // A signed-in customer is told in their bell.
        if (conversation.CustomerId is long customerId)
        {
            NotificationWriter.ToCustomer(unitOfWork, customerId, NotificationKinds.SupportMessage,
                ("JoFit replied", "رد فريق JoFit"), ($"We replied to your message ({conversation.Reference}).", $"رددنا على رسالتك ({conversation.Reference})."),
                "/account");
        }

        await unitOfWork.SaveChangesAsync(ct);
        return SupportSupport.Message(message);
    }
}

public class SetSupportStatusCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SetSupportStatusCommand, Result<ConversationSummaryDto>>
{
    public async Task<Result<ConversationSummaryDto>> Handle(SetSupportStatusCommand request, CancellationToken ct)
    {
        var conversation = await unitOfWork.GetRepository<SupportConversation>().GetTableAsTracking().Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (conversation is null)
        {
            return SupportErrors.NotFound;
        }

        // Anything other than closed means open.
        conversation.SetStatus(request.Status ?? string.Empty);
        await unitOfWork.SaveChangesAsync(ct);
        return SupportSupport.Summary(conversation, SupportSupport.StaffUnread(conversation));
    }
}

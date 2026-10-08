using FluentValidation;
using JoFit.Application.Common.Extensions;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Common.Models;
using JoFit.Application.Features.Auth;
using JoFit.Domain.Common.Results;
using MediatR;

namespace JoFit.Application.Features.Staff;

public static class StaffRoleNames
{
    public static readonly string[] All = ["owner", "content-editor", "support", "viewer"];
}

public sealed record AdminSignInCommand(string Email, string Password) : IRequest<Result<StaffSignedInDto>>;

public sealed record AdminSignOutCommand(long? SessionId, string? RefreshToken) : IRequest<Result<Deleted>>;

public sealed record AdminRefreshCommand(string RefreshToken) : IRequest<Result<StaffSignedInDto>>;

public sealed record GetAdminSessionQuery(long UserId) : IRequest<Result<AdminSessionDto>>;

public sealed record GetStaffPagedQuery(int Page, int PageSize, string? Search, string? SortField, string? SortDir)
    : IRequest<Result<PagedResult<AdminUserDto>>>;

public sealed record GetStaffOptionsQuery : IRequest<Result<List<AdminUserDto>>>;

public sealed record CreateStaffCommand(string Name, string Email, string Role, string? Password)
    : IRequest<Result<AdminUserCreatedDto>>;

public sealed record UpdateStaffCommand(long Id, string Name, string Email, string Role)
    : IRequest<Result<AdminUserDto>>;

public sealed record DeleteStaffCommand(long Id, long? CurrentUserId) : IRequest<Result<Deleted>>;

public sealed class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.Name).Must(n => AccountRules.CollapseWhitespace(n).Length >= 2).WithMessage("Enter the person's name.");
        RuleFor(x => x.Email).Must(AccountRules.IsValidEmail).WithMessage("An email address we can reach them on.");
        RuleFor(x => x.Role).Must(r => StaffRoleNames.All.Contains(r)).WithMessage("Choose one of the staff roles.");
        RuleFor(x => x.Password).Must(p => string.IsNullOrEmpty(p) || p.Length >= AccountRules.MinPasswordLength)
            .WithMessage($"Use at least {AccountRules.MinPasswordLength} characters.");
    }
}

public sealed class UpdateStaffCommandValidator : AbstractValidator<UpdateStaffCommand>
{
    public UpdateStaffCommandValidator()
    {
        RuleFor(x => x.Name).Must(n => AccountRules.CollapseWhitespace(n).Length >= 2).WithMessage("Enter the person's name.");
        RuleFor(x => x.Email).Must(AccountRules.IsValidEmail).WithMessage("An email address we can reach them on.");
        RuleFor(x => x.Role).Must(r => StaffRoleNames.All.Contains(r)).WithMessage("Choose one of the staff roles.");
    }
}

public class AdminSignInCommandHandler(IStaffService staff) : IRequestHandler<AdminSignInCommand, Result<StaffSignedInDto>>
{
    public Task<Result<StaffSignedInDto>> Handle(AdminSignInCommand request, CancellationToken ct)
        => staff.SignInAsync(request.Email ?? string.Empty, request.Password ?? string.Empty, ct);
}


public class AdminSignOutCommandHandler(IStaffService staff) : IRequestHandler<AdminSignOutCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(AdminSignOutCommand request, CancellationToken ct)
    {
        await staff.SignOutAsync(request.SessionId, request.RefreshToken, ct);
        return Result.Deleted;
    }
}

public class AdminRefreshCommandHandler(IStaffService staff) : IRequestHandler<AdminRefreshCommand, Result<StaffSignedInDto>>
{
    public Task<Result<StaffSignedInDto>> Handle(AdminRefreshCommand request, CancellationToken ct)
        => staff.RefreshAsync(request.RefreshToken, ct);
}

public class GetAdminSessionQueryHandler(IStaffService staff) : IRequestHandler<GetAdminSessionQuery, Result<AdminSessionDto>>
{
    public Task<Result<AdminSessionDto>> Handle(GetAdminSessionQuery request, CancellationToken ct)
        => staff.GetSessionAsync(request.UserId, ct);
}

public class GetStaffPagedQueryHandler(IStaffService staff) : IRequestHandler<GetStaffPagedQuery, Result<PagedResult<AdminUserDto>>>
{
    public async Task<Result<PagedResult<AdminUserDto>>> Handle(GetStaffPagedQuery request, CancellationToken ct)
        => await staff.ListAsync(request.Page, request.PageSize, request.Search, request.SortField, request.SortDir, ct);
}

public class GetStaffOptionsQueryHandler(IStaffService staff) : IRequestHandler<GetStaffOptionsQuery, Result<List<AdminUserDto>>>
{
    public async Task<Result<List<AdminUserDto>>> Handle(GetStaffOptionsQuery request, CancellationToken ct)
        => await staff.OptionsAsync(ct);
}

public class CreateStaffCommandHandler(IStaffService staff) : IRequestHandler<CreateStaffCommand, Result<AdminUserCreatedDto>>
{
    public Task<Result<AdminUserCreatedDto>> Handle(CreateStaffCommand request, CancellationToken ct)
        => staff.CreateAsync(
            new StaffInput(AccountRules.CollapseWhitespace(request.Name), request.Email.Trim(), request.Role, request.Password), ct);
}

public class UpdateStaffCommandHandler(IStaffService staff) : IRequestHandler<UpdateStaffCommand, Result<AdminUserDto>>
{
    public Task<Result<AdminUserDto>> Handle(UpdateStaffCommand request, CancellationToken ct)
        => staff.UpdateAsync(
            request.Id, new StaffInput(AccountRules.CollapseWhitespace(request.Name), request.Email.Trim(), request.Role, null), ct);
}

public class DeleteStaffCommandHandler(IStaffService staff) : IRequestHandler<DeleteStaffCommand, Result<Deleted>>
{
    public Task<Result<Deleted>> Handle(DeleteStaffCommand request, CancellationToken ct)
        => staff.DeleteAsync(request.Id, request.CurrentUserId, ct);
}

using FluentValidation;
using JoFit.Application.Common.Interfaces;
using JoFit.Application.Features.Auth;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Customers;
using JoFit.Domain.Identity.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JoFit.Application.Features.Customers;

public sealed record CustomerProfileDto(long Id, string DisplayName, string Email, string? Phone, string? AvatarUrl, DateTimeOffset CreatedAt);

public sealed record CustomerPreferencesDto(string Locale, bool MarketingOptIn, string Currency = "JOD");

public static class CustomerErrors
{
    public static readonly Error NotFound = Error.NotFound("Customer.NotFound", "That customer does not exist.");
}

public sealed record GetCustomerProfileQuery(long UserId) : IRequest<Result<CustomerProfileDto>>;

public sealed record UpdateCustomerProfileCommand(long UserId, string Name, string? Phone) : IRequest<Result<CustomerProfileDto>>;

public sealed record GetCustomerPreferencesQuery(long UserId) : IRequest<Result<CustomerPreferencesDto>>;

public sealed record UpdateCustomerPreferencesCommand(long UserId, string Locale, bool MarketingOptIn) : IRequest<Result<Deleted>>;

public sealed class UpdateCustomerProfileCommandValidator : AbstractValidator<UpdateCustomerProfileCommand>
{
    public UpdateCustomerProfileCommandValidator()
    {
        RuleFor(x => x.Name).FullName();
        RuleFor(x => x.Phone).OptionalPhone();
    }
}

public class GetCustomerProfileQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCustomerProfileQuery, Result<CustomerProfileDto>>
{
    public async Task<Result<CustomerProfileDto>> Handle(GetCustomerProfileQuery request, CancellationToken ct)
    {
        var customer = await unitOfWork.GetRepository<Customer>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, ct);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        var email = await unitOfWork.GetDbContext().Set<User>().AsNoTracking()
            .Where(u => u.Id == request.UserId).Select(u => u.Email).FirstAsync(ct);
        return new CustomerProfileDto(customer.Id, customer.Name, email!, customer.Phone, null, customer.CreatedAtUtc);
    }
}

public class UpdateCustomerProfileCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerProfileCommand, Result<CustomerProfileDto>>
{
    public async Task<Result<CustomerProfileDto>> Handle(UpdateCustomerProfileCommand request, CancellationToken ct)
    {
        var customer = await unitOfWork.GetRepository<Customer>().GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, ct);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        customer.UpdateProfile(
            AccountRules.CollapseWhitespace(request.Name),
            string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim());
        await unitOfWork.SaveChangesAsync(ct);

        var email = await unitOfWork.GetDbContext().Set<User>().AsNoTracking()
            .Where(u => u.Id == request.UserId).Select(u => u.Email).FirstAsync(ct);
        return new CustomerProfileDto(customer.Id, customer.Name, email!, customer.Phone, null, customer.CreatedAtUtc);
    }
}

public class GetCustomerPreferencesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCustomerPreferencesQuery, Result<CustomerPreferencesDto>>
{
    public async Task<Result<CustomerPreferencesDto>> Handle(GetCustomerPreferencesQuery request, CancellationToken ct)
    {
        var customer = await unitOfWork.GetRepository<Customer>().GetTableNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, ct);
        return customer is null ? CustomerErrors.NotFound : new CustomerPreferencesDto(customer.Locale, customer.MarketingOptIn);
    }
}

public class UpdateCustomerPreferencesCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCustomerPreferencesCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(UpdateCustomerPreferencesCommand request, CancellationToken ct)
    {
        var customer = await unitOfWork.GetRepository<Customer>().GetTableAsTracking()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, ct);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        customer.UpdatePreferences(request.Locale, request.MarketingOptIn);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}

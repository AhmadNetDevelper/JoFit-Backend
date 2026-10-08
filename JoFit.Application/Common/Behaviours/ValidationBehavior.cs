using FluentValidation;
using JoFit.Domain.Common.Results;
using JoFit.Domain.Common.Results.Abstractions;
using MediatR;
using System.Reflection;

namespace JoFit.Application.Common.Behaviours;

public class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : IResult
{
    private readonly IValidator<TRequest>? _validator = validator;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (_validator is null)
        {
            return await next(ct);
        }

        var validationResult = await _validator.ValidateAsync(request, ct);

        if (validationResult.IsValid)
        {
            return await next(ct);
        }

        var errors = validationResult.Errors
            .ConvertAll(error => Error.Validation(
                code: error.PropertyName,
                description: error.ErrorMessage));

        // TResponse is Result<T> for some T. We need to create the correct
        // Result<T> via its private constructor that accepts List<Error>,
        // so the runtime type matches TResponse exactly.
        var resultInstance = Activator.CreateInstance(
            typeof(TResponse),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [errors],
            culture: null)!;

        return (TResponse)resultInstance;
    }
}

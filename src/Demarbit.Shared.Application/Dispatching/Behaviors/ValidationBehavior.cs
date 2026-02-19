using Demarbit.Shared.Application.Dispatching.Contracts;
using Demarbit.Shared.Application.Exceptions;
using Demarbit.Shared.Application.Models;

namespace Demarbit.Shared.Application.Dispatching.Behaviors;

/// <summary>
/// Pipeline behavior that executes all registered <see cref="IValidator{T}"/> instances
/// for the request. If any validators return errors, a <see cref="ValidationFailedException"/>
/// is thrown before the request reaches its handler.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var errors = new List<ValidationError>();

        foreach (var validator in validators)
        {
            var validationErrors = await validator.ValidateAsync(request, cancellationToken);
            errors.AddRange(validationErrors);
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(typeof(TRequest).Name, errors);
        }

        return await next(cancellationToken);
    }
}
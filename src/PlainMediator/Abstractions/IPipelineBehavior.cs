namespace PlainMediator.Abstractions;

/// <summary>
/// Represents the continuation of the request pipeline: the next behavior, or the handler itself.
/// </summary>
/// <typeparam name="TResponse">The type returned by the pipeline.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);

/// <summary>
/// Cross-cutting behavior wrapped around a request handler (logging, validation, transactions, ...).
/// Behaviors run in registration order: the first one registered is the outermost.
/// </summary>
/// <typeparam name="TRequest">The request type this behavior applies to.</typeparam>
/// <typeparam name="TResponse">The type returned by the pipeline.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Runs the behavior, calling <paramref name="next"/> to continue the pipeline.</summary>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

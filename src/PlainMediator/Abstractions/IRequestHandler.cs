namespace PlainMediator.Abstractions;

/// <summary>
/// Handles a single <typeparamref name="TRequest"/> and produces a <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The request type handled by this handler.</typeparam>
/// <typeparam name="TResponse">The type returned by this handler.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handles the request.</summary>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

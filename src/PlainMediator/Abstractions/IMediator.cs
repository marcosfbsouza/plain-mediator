namespace PlainMediator.Abstractions;

/// <summary>
/// Dispatches requests to a single handler and publishes notifications to every registered handler.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends <paramref name="request"/> through the pipeline to its <see cref="IRequestHandler{TRequest, TResponse}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">No handler is registered for the request type.</exception>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes <paramref name="notification"/> to every registered handler, sequentially, in registration order.
    /// Handlers are resolved against the notification's runtime type.
    /// </summary>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}

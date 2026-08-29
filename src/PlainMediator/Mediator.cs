using System.Collections.Concurrent;
using PlainMediator.Abstractions;

namespace PlainMediator;

/// <summary>
/// Default <see cref="IMediator"/> implementation. Resolves handlers and pipeline behaviors from
/// the <see cref="IServiceProvider"/> of the current scope.
/// </summary>
/// <param name="provider">The scoped service provider used to resolve handlers and behaviors.</param>
public sealed class Mediator(IServiceProvider provider) : IMediator
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), RequestWrapper> RequestWrappers = new();
    private static readonly ConcurrentDictionary<Type, NotificationWrapper> NotificationWrappers = new();

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = (request.GetType(), typeof(TResponse));

        var wrapper = (RequestWrapper<TResponse>)RequestWrappers.GetOrAdd(
            key,
            static k => (RequestWrapper)Activator.CreateInstance(
                typeof(RequestWrapperImpl<,>).MakeGenericType(k.Request, k.Response))!);

        return wrapper.Handle(request, provider, cancellationToken);
    }

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var wrapper = NotificationWrappers.GetOrAdd(
            notification.GetType(),
            static t => (NotificationWrapper)Activator.CreateInstance(
                typeof(NotificationWrapperImpl<>).MakeGenericType(t))!);

        return wrapper.Handle(notification, provider, cancellationToken);
    }
}

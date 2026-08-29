using Microsoft.Extensions.DependencyInjection;
using PlainMediator.Abstractions;

namespace PlainMediator;

/// <summary>
/// Base não genérica, para que wrappers de qualquer formato compartilhem um único dicionário de cache.
/// </summary>
internal abstract class RequestWrapper { }

internal abstract class RequestWrapper<TResponse> : RequestWrapper
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken);
}

/// <summary>
/// Fecha o par genérico request/response, o que permite resolver e invocar o handler e seus behaviors
/// pelas interfaces reais, em vez de reflection a cada chamada.
/// </summary>
internal sealed class RequestWrapperImpl<TRequest, TResponse> : RequestWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var handler = provider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for {typeof(TRequest).Name}. Expected an implementation of " +
                $"IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>.");

        var typed = (TRequest)request;

        RequestHandlerDelegate<TResponse> pipeline = ct => handler.Handle(typed, ct);

        // A cadeia é montada de dentro para fora, então o primeiro behavior registrado
        // acaba sendo o mais externo — mesma ordem de execução do MediatR.
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = pipeline;
            pipeline = ct => behavior.Handle(typed, next, ct);
        }

        return pipeline(cancellationToken);
    }
}

internal abstract class NotificationWrapper
{
    public abstract Task Handle(INotification notification, IServiceProvider provider, CancellationToken cancellationToken);
}

internal sealed class NotificationWrapperImpl<TNotification> : NotificationWrapper
    where TNotification : INotification
{
    public override async Task Handle(INotification notification, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var typed = (TNotification)notification;

        foreach (var handler in provider.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.Handle(typed, cancellationToken).ConfigureAwait(false);
        }
    }
}

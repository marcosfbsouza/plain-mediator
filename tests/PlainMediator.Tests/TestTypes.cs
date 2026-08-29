using PlainMediator.Abstractions;

namespace PlainMediator.Tests;

public sealed record Ping(string Message) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken) =>
        Task.FromResult($"pong:{request.Message}");
}

public sealed record Unhandled : IRequest<int>;

public sealed record CancellablePing : IRequest<CancellationToken>;

public sealed class CancellablePingHandler : IRequestHandler<CancellablePing, CancellationToken>
{
    public Task<CancellationToken> Handle(CancellablePing request, CancellationToken cancellationToken) =>
        Task.FromResult(cancellationToken);
}

/// <summary>Registra a ordem em que behaviors e handlers são executados.</summary>
public sealed class Trace
{
    public List<string> Steps { get; } = [];
}

public sealed class FirstBehavior<TRequest, TResponse>(Trace trace) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        trace.Steps.Add("first:before");
        var response = await next(cancellationToken);
        trace.Steps.Add("first:after");
        return response;
    }
}

public sealed class SecondBehavior<TRequest, TResponse>(Trace trace) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        trace.Steps.Add("second:before");
        var response = await next(cancellationToken);
        trace.Steps.Add("second:after");
        return response;
    }
}

public sealed class ShortCircuitBehavior(Trace trace) : IPipelineBehavior<Ping, string>
{
    public Task<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        trace.Steps.Add("short-circuit");
        return Task.FromResult("short-circuited");
    }
}

public sealed record Notified(string Message) : INotification;

public sealed class FirstNotificationHandler(Trace trace) : INotificationHandler<Notified>
{
    public Task Handle(Notified notification, CancellationToken cancellationToken)
    {
        trace.Steps.Add($"first:{notification.Message}");
        return Task.CompletedTask;
    }
}

public sealed class SecondNotificationHandler(Trace trace) : INotificationHandler<Notified>
{
    public Task Handle(Notified notification, CancellationToken cancellationToken)
    {
        trace.Steps.Add($"second:{notification.Message}");
        return Task.CompletedTask;
    }
}

public sealed record Unobserved : INotification;

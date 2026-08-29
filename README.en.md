# PlainMediator

*Leia em [português](https://github.com/marcosfbsouza/plain-mediator/blob/main/README.md).*

[![NuGet](https://img.shields.io/nuget/v/PlainMediator.svg)](https://www.nuget.org/packages/PlainMediator)
[![CI](https://github.com/marcosfbsouza/plain-mediator/actions/workflows/ci.yml/badge.svg)](https://github.com/marcosfbsouza/plain-mediator/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A small mediator for CQRS in .NET — `Send`/`Publish`, request handlers, notification handlers and
pipeline behaviors — wired through `Microsoft.Extensions.DependencyInjection`.

It has one dependency (`Microsoft.Extensions.DependencyInjection.Abstractions`), fits in a handful of
files, and is MIT licensed: no commercial license, no per-seat terms.

## Install

```bash
dotnet add package PlainMediator
```

Targets `net8.0` and `net10.0`.

## Register

```csharp
using PlainMediator;

// Scan every loaded assembly.
builder.Services.AddMediator();

// Or scan specific assemblies.
builder.Services.AddMediator(typeof(CreateOrder).Assembly);

// Or scan by assembly-name prefix — the cheapest option on large solutions.
builder.Services.AddMediator("MyApp.Application", "MyApp.Domain");
```

`AddMediator` registers `IMediator` and every `IRequestHandler<,>` / `INotificationHandler<>` it finds,
all as **scoped**. Pipeline behaviors are *not* discovered — you register them explicitly, so the order
stays under your control.

## Requests

```csharp
public sealed record GetOrderById(Guid Id) : IRequest<Order?>;

public sealed class GetOrderByIdHandler(AppDbContext db) : IRequestHandler<GetOrderById, Order?>
{
    public Task<Order?> Handle(GetOrderById request, CancellationToken cancellationToken) =>
        db.Orders.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
}
```

```csharp
var order = await mediator.Send(new GetOrderById(id), cancellationToken);
```

Exactly one handler answers a request. If none is registered, `Send` throws `InvalidOperationException`.

## Notifications

```csharp
public sealed record OrderPlaced(Guid OrderId) : INotification;

public sealed class SendConfirmationEmail : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) => /* ... */;
}
```

```csharp
await mediator.Publish(new OrderPlaced(order.Id), cancellationToken);
```

Every registered handler runs, sequentially, in registration order. Handlers are resolved from the
notification's **runtime** type, so publishing through a base-typed variable still reaches the right
handlers.

## Pipeline behaviors

Behaviors wrap the handler — logging, validation, transactions, and so on:

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next(cancellationToken);
        logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

```csharp
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

**Behaviors run in registration order: the first one registered is the outermost.** With the
registrations above, a request flows `Exception → Logging → Validation → handler`.

## Coming from MediatR

The contract mirrors MediatR's, so migration is mostly a namespace change:

| MediatR | PlainMediator |
| --- | --- |
| `IMediator.Send` / `Publish` | same |
| `IRequest<TResponse>` | same |
| `IRequestHandler<TRequest, TResponse>` | same |
| `INotification`, `INotificationHandler<T>` | same |
| `IPipelineBehavior<TRequest, TResponse>` | same |
| `RequestHandlerDelegate<TResponse>` | same, but takes a `CancellationToken` |
| `services.AddMediatR(cfg => ...)` | `services.AddMediator(...)` |

Not included: `IRequest` without a response, streaming requests, pre/post processors, `INotificationPublisher`
strategies, and `RequestExceptionHandler`.

## Performance notes

Handlers and behaviors are invoked through closed generic wrappers that are built once per
request type and cached, so there is no reflection on the hot path — only a dictionary lookup.

Because those wrappers are constructed with `MakeGenericType`, the library is not annotated as
trim- or AOT-safe.


## Development

```bash
dotnet build PlainMediator.slnx -c Release
dotnet test PlainMediator.slnx -c Release
dotnet pack src/PlainMediator/PlainMediator.csproj -c Release   # -> artifacts/packages
```

Running the `net8.0` test pass locally needs the .NET 8 runtime installed alongside the .NET 10 SDK;
otherwise use `dotnet test -f net10.0`. CI runs both.

## Releasing

The package version comes from the git tag, via [MinVer](https://github.com/adamralph/minver) — there is
no `<Version>` to bump by hand.

```bash
git tag v1.0.0
git push origin v1.0.0
```

That triggers `.github/workflows/release.yml`, which builds, tests, packs and pushes to nuget.org using
Trusted Publishing — nuget.org issues a short-lived key from the workflow's OIDC identity, with no
API key stored in the repository. Untagged builds produce `0.0.0-alpha.0`.

## License

MIT — see [LICENSE](LICENSE).

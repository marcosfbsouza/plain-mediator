# PlainMediator

*Read this in [English](https://github.com/marcosfbsouza/plain-mediator/blob/main/README.en.md).*

[![NuGet](https://img.shields.io/nuget/v/PlainMediator.svg)](https://www.nuget.org/packages/PlainMediator)
[![CI](https://github.com/marcosfbsouza/plain-mediator/actions/workflows/ci.yml/badge.svg)](https://github.com/marcosfbsouza/plain-mediator/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/marcosfbsouza/plain-mediator/blob/main/LICENSE)

Um mediator enxuto para CQRS em .NET — `Send`/`Publish`, handlers de request, handlers de notificação
e pipeline behaviors — integrado ao `Microsoft.Extensions.DependencyInjection`.

Tem uma única dependência (`Microsoft.Extensions.DependencyInjection.Abstractions`), cabe em um punhado
de arquivos e é licenciado sob MIT: sem licença comercial, sem cobrança por desenvolvedor.

## Instalação

```bash
dotnet add package PlainMediator
```

Suporta `net8.0` e `net10.0`.

## Registro

```csharp
using PlainMediator;

// Varre todos os assemblies carregados.
builder.Services.AddMediator();

// Ou varre assemblies específicos.
builder.Services.AddMediator(typeof(CreateOrder).Assembly);

// Ou varre por prefixo do nome do assembly — a opção mais barata em soluções grandes.
builder.Services.AddMediator("MyApp.Application", "MyApp.Domain");
```

`AddMediator` registra o `IMediator` e todos os `IRequestHandler<,>` / `INotificationHandler<>` que
encontrar, todos como **scoped**. Pipeline behaviors *não* são descobertos automaticamente — você os
registra explicitamente, então a ordem continua sob seu controle.

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

Exatamente um handler responde a cada request. Se nenhum estiver registrado, o `Send` lança
`InvalidOperationException`.

## Notificações

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

Todos os handlers registrados são executados, sequencialmente, na ordem de registro. Os handlers são
resolvidos pelo tipo **em tempo de execução** da notificação, então publicar através de uma variável
declarada com o tipo base ainda alcança os handlers certos.

## Pipeline behaviors

Behaviors envolvem o handler — log, validação, transação e afins:

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

**Os behaviors executam na ordem de registro: o primeiro registrado é o mais externo.** Com os
registros acima, uma request percorre `Exception → Logging → Validation → handler`.

## Vindo do MediatR

O contrato espelha o do MediatR, então a migração é basicamente uma troca de namespace:

| MediatR | PlainMediator |
| --- | --- |
| `IMediator.Send` / `Publish` | igual |
| `IRequest<TResponse>` | igual |
| `IRequestHandler<TRequest, TResponse>` | igual |
| `INotification`, `INotificationHandler<T>` | igual |
| `IPipelineBehavior<TRequest, TResponse>` | igual |
| `RequestHandlerDelegate<TResponse>` | igual, mas recebe um `CancellationToken` |
| `services.AddMediatR(cfg => ...)` | `services.AddMediator(...)` |

Não incluído: `IRequest` sem resposta, requests de streaming, pre/post processors, estratégias de
`INotificationPublisher` e `RequestExceptionHandler`.

## Notas de performance

Handlers e behaviors são invocados através de wrappers genéricos fechados, construídos uma vez por
tipo de request e mantidos em cache — não há reflection no caminho quente, apenas uma consulta a
dicionário.

Como esses wrappers são construídos com `MakeGenericType`, a biblioteca não é anotada como segura
para trimming ou AOT.

## Desenvolvimento

```bash
dotnet build PlainMediator.slnx -c Release
dotnet test PlainMediator.slnx -c Release
dotnet pack src/PlainMediator/PlainMediator.csproj -c Release   # -> artifacts/packages
```

Rodar os testes em `net8.0` localmente exige o runtime do .NET 8 instalado ao lado do SDK do .NET 10;
sem ele, use `dotnet test -f net10.0`. O CI roda os dois.

## Publicação

A versão do pacote vem da tag do git, via [MinVer](https://github.com/adamralph/minver) — não existe
`<Version>` para atualizar na mão.

```bash
git tag v1.0.0
git push origin v1.0.0
```

Isso dispara o `.github/workflows/release.yml`, que compila, testa, empacota e envia para o nuget.org
via Trusted Publishing — o nuget.org emite uma chave temporária a partir da identidade OIDC do
workflow, sem API key guardada no repositório. Builds sem tag produzem `0.0.0-alpha.0`.

## Licença

MIT — veja [LICENSE](https://github.com/marcosfbsouza/plain-mediator/blob/main/LICENSE).

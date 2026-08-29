# Changelog

Todas as mudanças relevantes deste projeto são registradas aqui.
O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/),
e o projeto adota o [Versionamento Semântico](https://semver.org/lang/pt-BR/spec/v2.0.0.html).

## [Não publicado]

## [1.0.0] - 2026-08-28

### Adicionado

- `IMediator` com `Send` e `Publish`.
- `IRequest<TResponse>` e `IRequestHandler<TRequest, TResponse>`.
- `INotification` e `INotificationHandler<TNotification>`.
- `IPipelineBehavior<TRequest, TResponse>` com `RequestHandlerDelegate<TResponse>`.
- `services.AddMediator(...)` com descoberta de handlers por assembly ou por prefixo do nome do assembly.
- Suporte a `net8.0` e `net10.0`, com XML docs, SourceLink e pacote de símbolos.

[Não publicado]: https://github.com/marcosfbsouza/plain-mediator/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/marcosfbsouza/plain-mediator/releases/tag/v1.0.0

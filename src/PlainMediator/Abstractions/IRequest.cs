namespace PlainMediator.Abstractions;

/// <summary>
/// Marks a type as a request that can be dispatched through <see cref="IMediator.Send{TResponse}"/>
/// and handled by a matching <see cref="IRequestHandler{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The type returned by the handler.</typeparam>
public interface IRequest<TResponse> { }

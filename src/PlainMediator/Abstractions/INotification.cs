namespace PlainMediator.Abstractions;

/// <summary>
/// Marks a type as a notification that can be published through <see cref="IMediator.Publish{TNotification}"/>
/// and observed by zero or more <see cref="INotificationHandler{TNotification}"/> implementations.
/// </summary>
public interface INotification { }

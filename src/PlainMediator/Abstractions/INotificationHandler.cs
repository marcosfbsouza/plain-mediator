namespace PlainMediator.Abstractions;

/// <summary>
/// Handles a published <typeparamref name="TNotification"/>. Several handlers may observe the same notification.
/// </summary>
/// <typeparam name="TNotification">The notification type observed by this handler.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>Handles the notification.</summary>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

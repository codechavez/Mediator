namespace CodeChavez.M3diator.Interfaces;

public interface INotification { }

public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

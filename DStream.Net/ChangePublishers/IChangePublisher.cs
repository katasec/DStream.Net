namespace DStream.Net.ChangePublishers;

public interface IChangePublisher
{
    Task SendAsync(MonitoringMessage message);
}

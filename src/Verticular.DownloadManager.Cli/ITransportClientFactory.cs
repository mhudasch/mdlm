namespace Verticular.DownloadManager.Cli;

public interface ITransportClientFactory
{
  Task<ITransportClient> CreateTransportClient(TransportClientConfiguration configuration, CancellationToken cancellationToken = default);
}

namespace Verticular.DownloadManager.Cli;

public sealed class DefaultTransportClientFactory : ITransportClientFactory
{
  public Task<ITransportClient> CreateTransportClient(TransportClientConfiguration configuration, CancellationToken cancellationToken = default)
  {
    return configuration.Protocol switch
    {
      "Https" => Task.FromResult<ITransportClient>(new HttpsTransportClient((HttpsTransportClientConfiguration)configuration)),
      _ => throw new NotSupportedException(configuration.Protocol)
    };
  }
}

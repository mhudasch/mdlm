namespace Verticular.DownloadManager.Cli;

public sealed class DefaultDownloadClientFactory : IDownloadClientFactory
{
  public Task<IDownloadClient> CreateClient(Uri uri,
    TransportSecurityConfiguration transportSecurityConfiguration,
    AccessSecurityConfiguration accessSecurityConfiguration,
    CancellationToken cancellationToken = default)
  {
    var scheme = uri.Scheme.ToLowerInvariant();
    return scheme switch
    {
      "https" => Task.FromResult<IDownloadClient>(new HttpsDownloadClient(new TransportLayerSecurity(transportSecurityConfiguration))),
      _ => throw new NotSupportedException(scheme),
    };
  }
}

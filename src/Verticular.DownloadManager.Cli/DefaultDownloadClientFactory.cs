namespace Verticular.DownloadManager.Cli;

public sealed class DefaultDownloadClientFactory : IDownloadClientFactory
{
  public Task<IDownloadClient> CreateClient(Uri uri, DownloadRequestOptions requestOptions,
    CancellationToken cancellationToken = default)
  {
    var scheme = uri.Scheme.ToLowerInvariant();
    return scheme switch
    {
      "https" => Task.FromResult<IDownloadClient>(new HttpDownloadClient(requestOptions.ProtocolVersion, new TransportLayerSecurity(requestOptions.TransportSecurityOptions))),
      _ => throw new NotSupportedException(scheme),
    };
  }
}

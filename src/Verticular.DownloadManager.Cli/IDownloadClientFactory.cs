namespace Verticular.DownloadManager.Cli;

public interface IDownloadClientFactory
{
  Task<IDownloadClient> CreateClient(Uri uri,
    TransportSecurityConfiguration transportSecurityConfiguration,
    AccessSecurityConfiguration accessSecurityConfiguration,

    CancellationToken cancellationToken = default);
}

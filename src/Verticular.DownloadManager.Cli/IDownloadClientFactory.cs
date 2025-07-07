namespace Verticular.DownloadManager.Cli;

public interface IDownloadClientFactory
{
  Task<IDownloadClient> CreateClient(Uri uri, DownloadRequestOptions requestOptions,

    CancellationToken cancellationToken = default);
}

namespace Verticular.DownloadManager.Cli;

public interface ITransportClient : IDisposable, IAsyncDisposable
{
  Task Download(string uri, CancellationToken cancellationToken = default);
}

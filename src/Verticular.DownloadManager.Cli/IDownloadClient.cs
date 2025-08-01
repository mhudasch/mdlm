namespace Verticular.DownloadManager.Cli;

public interface IDownloadClient : IDisposable, IAsyncDisposable
{
  Task<DownloadPreflightResult> Preflight(CancellationToken cancellationToken = default);
}

public class DownloadPreflightResult
{
  public required bool SupportsSegmentedDownloads { get; init; }
  public required bool NeedsAuthentication { get; init; }
  public required long? TotalDownloadSize{ get; init; }
}

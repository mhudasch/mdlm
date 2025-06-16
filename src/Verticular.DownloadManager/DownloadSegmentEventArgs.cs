namespace Verticular.DownloadManager;

internal class DownloadSegmentEventArgs(Downloader d, DownloadSegment segment) : DownloaderEventArgs(d)
{
    public DownloadSegment DownloadSegment { get; init; } = segment;
}

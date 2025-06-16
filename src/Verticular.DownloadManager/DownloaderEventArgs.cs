namespace Verticular.DownloadManager;

internal class DownloaderEventArgs(Downloader downloader, bool willStart = false) : EventArgs
{
    public Downloader Downloader { get; init; } = downloader;
    public bool WillStart { get; init; } = willStart;
}

namespace Verticular.DownloadManager;

internal enum DownloaderState
{
    NeedsToPrepare = 0,
    Preparing,
    WaitingForReconnect,
    Prepared,
    Working,
    Pausing,
    Paused,
    Ended,
    EndedWithError
}


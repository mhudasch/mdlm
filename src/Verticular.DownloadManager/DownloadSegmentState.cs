namespace Verticular.DownloadManager;

internal enum DownloadSegmentState
{
    None = 0,
    Idle,
    Connecting,
    Downloading,
    Paused,
    Finished,
    Error,
}

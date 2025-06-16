namespace Verticular.DownloadManager;

internal static class Constants
{
    public const ulong MIN_DOWNLOAD_SEGMENT_SIZE = 200000;

    public const uint MIN_DOWNLOAD_SEGMENT_COUNT = 5;

    public const uint MAX_DOWNLOAD_RETRY_COUNT = 5;

    public const uint MIN_SEGMENT_LEFT_TO_START_NEW_SEGMENT = 30;

    public static TimeSpan DEFAULT_RETRY_DELAY = TimeSpan.FromSeconds(20);
}

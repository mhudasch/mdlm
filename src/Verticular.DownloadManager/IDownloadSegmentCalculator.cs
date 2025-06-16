namespace Verticular.DownloadManager;

internal interface IDownloadSegmentCalculator
{
    CalculatedSegment[] Calculate(uint segmentCount, ulong fileSize);
}

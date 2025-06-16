namespace Verticular.DownloadManager;

internal sealed class DefaultDownloadSegmentCalculator : IDownloadSegmentCalculator
{
    public CalculatedSegment[] Calculate(uint segmentCount, ulong fileSize)
    {
        var minSegmentSize = Constants.MIN_DOWNLOAD_SEGMENT_SIZE;
        var selectedSegmentSize = (ulong)(fileSize / segmentCount);

        while (segmentCount > 1 && selectedSegmentSize < minSegmentSize)
        {
            segmentCount--;
            selectedSegmentSize = (ulong)(fileSize / segmentCount);
        }

        var startPosition = 0ul;

        List<CalculatedSegment> segments = [];

        for (int i = 0; i < segmentCount; i++)
        {
            if (segmentCount - 1 == i)
            {
                segments.Add(new (startPosition, fileSize));
            }
            else
            {
                segments.Add(new(startPosition, startPosition + selectedSegmentSize));
            }

            startPosition = segments[^1].End;
        }

        return [.. segments];
    }
}

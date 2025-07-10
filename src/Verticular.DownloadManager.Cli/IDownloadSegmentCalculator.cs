namespace Verticular.DownloadManager.Cli;

public interface ISegmentCalculator
{
  CalculatedSegment[] CalculateSegments(int segmentCount, long downloadSize);
}

public readonly struct CalculatedSegment
{
  public readonly long Start { get; }
  public readonly long End { get; }

  public CalculatedSegment(long start, long end)
  {
    this.Start = start;
    this.End = end;
  }
}

internal sealed class DefaultSegmentCalculator : ISegmentCalculator
{
  public CalculatedSegment[] CalculateSegments(int segmentCount, long fileSize)
  {
    var minSegmentSize = 200000L;
    var selectedSegmentSize = (long)(fileSize / segmentCount);

    while (segmentCount > 1 && selectedSegmentSize < minSegmentSize)
    {
      segmentCount--;
      selectedSegmentSize = (long)(fileSize / segmentCount);
    }

    var startPosition = 0L;

    List<CalculatedSegment> segments = [];

    for (var i = 0; i < segmentCount; i++)
    {
      if (segmentCount - 1 == i)
      {
        segments.Add(new(startPosition, fileSize));
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

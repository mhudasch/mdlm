namespace Verticular.DownloadManager;

internal readonly struct CalculatedSegment
{
    public readonly ulong Start { get; }
    public readonly ulong End { get; }

    public CalculatedSegment(ulong start, ulong end)
    {
        this.Start = start;
        this.End = end;
    }
}

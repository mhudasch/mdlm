namespace Verticular.DownloadManager;

internal readonly record struct RemoteFileInfo
{
    public bool AcceptRanges { get; }
    public ulong FileSize { get; }
    public DateTime LastModified { get; }
    public string MimeType{ get; }
    public RemoteFileInfo(string mimeType, ulong fileSize, DateTime lastModified, bool acceptRanges)
    {
        this.MimeType = mimeType;
        this.FileSize = fileSize;
        this.LastModified = lastModified;
        this.AcceptRanges = acceptRanges;
    }
}


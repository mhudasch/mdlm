namespace Verticular.DownloadManager;

internal sealed class SequentialMirrorSelector : IDownloadMirrorSelector
{
    private readonly ResourceLocation[] locations;
    private int locationIndex;
    public SequentialMirrorSelector(ResourceLocation[] locations)
    {
        this.locationIndex = 0;
        this.locations = locations;
    }
    public ResourceLocation GetNextResourceLocation()
    {
        if (locationIndex >= this.locations.Length)
        {
            this.locationIndex = 0;
        }
        return this.locations[this.locationIndex++];
    }
}
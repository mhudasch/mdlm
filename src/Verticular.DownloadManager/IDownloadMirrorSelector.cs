namespace Verticular.DownloadManager;

internal interface IDownloadMirrorSelector
{
    ResourceLocation GetNextResourceLocation();
}

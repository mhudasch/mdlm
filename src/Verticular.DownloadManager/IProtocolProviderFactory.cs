namespace Verticular.DownloadManager;

internal interface IProtocolProviderFactory
{
    IProtocolProvider? Create(ResourceLocation location);
}

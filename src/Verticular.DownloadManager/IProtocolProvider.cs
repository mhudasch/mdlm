namespace Verticular.DownloadManager;

internal interface IProtocolProvider
{
    Stream CreateStream(ResourceLocation resourceLocation, ulong initialPosition, ulong endPosition);
    RemoteFileInfo GetRemoteFileInfo(ResourceLocation resourceLocation, out Stream stream);
}


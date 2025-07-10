namespace Verticular.DownloadManager.Cli;

public class CycleMirrorSelector : IMirrorSelector
{
  private readonly ResourceLocation[] mirrors;
  private int currentMirrorIndex = 0;

  public CycleMirrorSelector(IReadOnlyCollection<ResourceLocation> mirrors)
  {
    this.mirrors = [.. mirrors];
  }

  public ResourceLocation GetNextLocation()
  {
    if (this.currentMirrorIndex >= this.mirrors.Length)
    {
      this.currentMirrorIndex = 0;
    }
    return this.mirrors[this.currentMirrorIndex++];
  }
}

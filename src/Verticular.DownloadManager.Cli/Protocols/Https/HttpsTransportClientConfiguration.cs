namespace Verticular.DownloadManager.Cli;

using System.Runtime.InteropServices;

public sealed class HttpsTransportClientConfiguration : TransportClientConfiguration
{
  public HttpsTransportClientConfiguration()
  {
    this.TryToLocateCASources();
  }

  private void TryToLocateCASources()
  {
    // complex logic to guestimate default values based on the current platform
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      // we do not check on windows because here the native CA logic should be used
      return;
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      // ubuntu (debian)
      if (File.Exists("/etc/ssl/certs/ca-certificate.crt") || Directory.Exists("/etc/ssl/certs"))
      {
        this.CAFile = "/etc/ssl/certs/ca-certificate.crt";
        this.CADirectory = "/etc/ssl/certs";
      }

    }
  }

  // TODO: wrap in own config
  public bool SkipSSLVerify { get; init; } = false;
  public string? CAFile { get; internal set; }
  public string? CADirectory { get; internal set; }
  public bool UseNativeCA { get; init; } = true;
}

namespace Verticular.DownloadManager.Cli;

using System.Runtime.InteropServices;

public sealed class TransportLayerSecurity
{
  public TransportLayerSecurity(TransportSecurityConfiguration configuration)
  {
    this.TryToLocateCASources();
    if (!string.IsNullOrWhiteSpace(configuration.CADirectory))
    {
      this.CADirectory = configuration.CADirectory;
    }
    if (!string.IsNullOrWhiteSpace(configuration.CAFile))
    {
      this.CAFile = configuration.CAFile;
    }
    this.SkipSSLVerify = configuration.SkipSSLVerify;
    this.UseNativeCA = configuration.UseNativeCA;
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
  public bool SkipSSLVerify { get; init; } = false;
  public string? CAFile { get; internal set; }
  public string? CADirectory { get; internal set; }
  public bool UseNativeCA { get; init; } = true;
}

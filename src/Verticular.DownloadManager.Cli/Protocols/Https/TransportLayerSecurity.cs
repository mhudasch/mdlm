namespace Verticular.DownloadManager.Cli;

using System;
using System.Runtime.InteropServices;
using System.Security.Authentication;

public sealed class TransportLayerSecurity
{
  public TransportLayerSecurity(TransportSecurityOptions configuration)
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
    this.SkipSslVerify = configuration.NoServerCertificateValidation;
    this.UseNativeCA = configuration.UseNativeCA;
    this.MinVersion = GetSslProtocols(ParseTlsVersion(configuration.MinVersion));
  }

  private static SslProtocols ParseTlsVersion(string? tlsVersion)
  {
    return tlsVersion?.ToLower() switch
    {
      "1.0" => SslProtocols.Tls,
      "1.1" => SslProtocols.Tls11,
      "1.2" => SslProtocols.Tls12,
      "1.3" => SslProtocols.Tls13,
      _ => SslProtocols.Tls12 // Default to TLS 1.2
    };
  }

  private static SslProtocols GetSslProtocols(SslProtocols minVersion)
  {
    return minVersion switch
    {
      SslProtocols.Tls => SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13,
      SslProtocols.Tls11 => SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13,
      SslProtocols.Tls12 => SslProtocols.Tls12 | SslProtocols.Tls13,
      SslProtocols.Tls13 => SslProtocols.Tls13,
      _ => SslProtocols.Tls12 | SslProtocols.Tls13
    };
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

  public SslProtocols MinVersion { get; init; } = SslProtocols.Tls12 | SslProtocols.Tls13;
  public bool SkipSslVerify { get; init; } = false;
  public string? CAFile { get; internal set; }
  public string? CADirectory { get; internal set; }
  public bool UseNativeCA { get; init; } = true;
}

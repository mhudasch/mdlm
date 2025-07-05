namespace Verticular.DownloadManager.Cli;

using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

public sealed class HttpsTransportClient : ITransportClient
{
  private readonly HttpsTransportClientConfiguration configuration;
  private readonly HttpClient client;

  public HttpsTransportClient(HttpsTransportClientConfiguration configuration)
  {
    this.configuration = configuration;
    this.client = this.CreateHttpClient();
  }

  public async Task Download(string uri, CancellationToken cancellationToken = default)
  {
    // todo: later return the stream here or raise events for download state changes
    await this.client.GetAsync(uri, cancellationToken);
  }

  private HttpClient CreateHttpClient()
  {
    var handler = new HttpClientHandler();

    // Handle SSL verification skip
    if (this.configuration.SkipSSLVerify)
    {
      handler.ServerCertificateCustomValidationCallback =
          (sender, certificate, chain, sslPolicyErrors) => true;
      Console.WriteLine("⚠️  SSL certificate verification disabled");
    }
    else if (!string.IsNullOrEmpty(this.configuration.CAFile) || !string.IsNullOrEmpty(this.configuration.CADirectory))
    {
      // Handle custom CA configuration
      var customCerts = LoadCustomCAs(this.configuration.CAFile, this.configuration.CADirectory);
      if (customCerts.Count > 0)
      {
        handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
              // todo: review this!
              if (certificate is null || chain is null)
              {
                return false;
              }
              return ValidateWithCustomCAs(certificate, chain, customCerts);
            };
        Console.WriteLine("✅ Custom CA certificates loaded");
      }
    }
    else if (this.configuration.UseNativeCA)
    {
      // Handle native CA store
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        // On Windows, .NET automatically uses the Windows certificate store by default
        // unless we override the validation callback
        if (handler.ServerCertificateCustomValidationCallback == null)
        {
          Console.WriteLine("✅ Using Windows certificate store");
        }
        else
        {
          Console.WriteLine("⚠️  Custom certificate validation in use, Windows store may not be primary");
        }
      }
      else
      {
        // todo: implement macos native ca
        Console.WriteLine($"⚠️  Native CA store requested but not on Windows (current OS: {RuntimeInformation.OSDescription})");
      }
    }

    var httpClient = new HttpClient(handler);
    return httpClient;
  }

  private static X509Certificate2Collection LoadCustomCAs(string? caPemFile, string? caDir)
  {
    var certificates = new X509Certificate2Collection();

    // Load from PEM file
    if (!string.IsNullOrEmpty(caPemFile))
    {
      if (File.Exists(caPemFile))
      {
        try
        {
          var pemContent = File.ReadAllText(caPemFile);
          var cert = X509Certificate2.CreateFromPem(pemContent);
          certificates.Add(cert);
          Console.WriteLine($"✅ Loaded CA certificate from: {caPemFile}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"⚠️  Failed to load CA file {caPemFile}: {ex.Message}");
        }
      }
      else
      {
        Console.WriteLine($"⚠️  CA file not found: {caPemFile}");
      }
    }

    // Load from directory
    if (!string.IsNullOrEmpty(caDir))
    {
      if (Directory.Exists(caDir))
      {
        try
        {
          string[] certFiles = [
            .. Directory.GetFiles(caDir, "*.pem"),
            .. Directory.GetFiles(caDir, "*.crt"),
            .. Directory.GetFiles(caDir, "*.cer")
          ];

          foreach (var file in certFiles)
          {
            try
            {
              var content = File.ReadAllText(file);
              var cert = X509Certificate2.CreateFromPem(content);
              certificates.Add(cert);
              Console.WriteLine($"✅ Loaded CA certificate from: {file}");
            }
            catch (Exception ex) when (ex is PathTooLongException
              or DirectoryNotFoundException
              or FileNotFoundException
              or IOException
              or UnauthorizedAccessException
              or System.Security.SecurityException
              or NotSupportedException)
            {
              Console.WriteLine($"⚠️  Failed to load CA file {file}: {ex.Message}");
            }
          }
        }
        catch (Exception ex) when (ex is PathTooLongException
          or DirectoryNotFoundException
          or FileNotFoundException
          or IOException
          or UnauthorizedAccessException
          or System.Security.SecurityException
          or NotSupportedException)
        {
          Console.WriteLine($"⚠️  Failed to read CA directory {caDir}: {ex.Message}");
        }
      }
      else
      {
        Console.WriteLine($"⚠️  CA directory not found: {caDir}");
      }
    }

    return certificates;
  }

  private static bool ValidateWithCustomCAs(X509Certificate2 certificate, X509Chain chain, X509Certificate2Collection customCAs)
  {
    // Add custom CAs to the chain
    chain.ChainPolicy.ExtraStore.AddRange(customCAs);
    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag; // ignore no verification step
    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online; //todo: maybe configurable as well

    // Build the chain
    var isValid = chain.Build(certificate);

    if (!isValid)
    {
      Console.WriteLine("⚠️  Certificate chain validation failed with custom CAs");
      foreach (var status in chain.ChainStatus)
      {
        Console.WriteLine($"   Chain status: {status.Status} - {status.StatusInformation}");
      }
    }

    return isValid;
  }

  public ValueTask DisposeAsync()
  {
    this.Dispose();
    return ValueTask.CompletedTask;
  }

  public void Dispose()
  {
    this.client?.Dispose();
  }
}

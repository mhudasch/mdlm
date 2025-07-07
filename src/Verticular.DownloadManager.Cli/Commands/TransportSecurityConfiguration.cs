public class TransportSecurityConfiguration
{
  public bool SkipSSLVerify { get; init; } = false;
  public string? CAFile { get; internal set; }
  public string? CADirectory { get; internal set; }
  public bool UseNativeCA { get; init; } = true;
}

public class TransportSecurityOptions
{
  public bool NoServerCertificateValidation { get; init; } = false;
  public string? CAFile { get; internal set; }
  public string? CADirectory { get; internal set; }
  public bool UseNativeCA { get; init; } = true;
  public string? MinVersion { get; init; }
}

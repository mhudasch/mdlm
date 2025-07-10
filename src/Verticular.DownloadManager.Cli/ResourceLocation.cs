public class ResourceLocation
{
  public ResourceLocation(Uri downloadUri)
  {
    this.ProtocolVersion = "1.1";
    this.Location = downloadUri;
    this.TransportSecurityOptions = new();
    this.AccessSecurityOptions = new();
  }

  public Uri Location { get; init; }
  public string ProtocolVersion { get; init; }
  public TransportSecurityOptions TransportSecurityOptions { get; init; }
  public AccessSecurityOptions AccessSecurityOptions { get; init; }
}

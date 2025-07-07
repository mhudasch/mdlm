using Verticular.DownloadManager.Cli;

public class BeginDownloadCommand : IDownloadCommand
{
  public required Uri Uri { get; init; }
  public required TransportSecurityConfiguration TransportSecurityConfiguration { get; init; }
  public required AccessSecurityConfiguration AccessSecurityConfiguration { get; init; }

  public async Task<IDownloadCommandResult> Handle(CancellationToken cancellationToken)
  {
    var commandId = Guid.NewGuid().ToString("N");
    var clientFactory = new DefaultDownloadClientFactory(); // from outside
    var downloadClient = await clientFactory.CreateClient(this.Uri, this.TransportSecurityConfiguration, this.AccessSecurityConfiguration, cancellationToken);
    var preflightResult = await downloadClient.Preflight(this.Uri, this.TransportSecurityConfiguration, this.AccessSecurityConfiguration, cancellationToken);

    // extract client from uri scheme
    // execute preflight
    return new BeginDownloadCommandResult
    {
      Id = commandId,
      State = DownloadCommandState.Ended
    };
  }
}

public class BeginDownloadCommandResult : IDownloadCommandResult
{
  public required string Id { get; init; }

  public required DownloadCommandState State { get; init; }
}

public interface IDownloadCommandResult
{
  string Id { get; }
  // segments
  DownloadCommandState State { get; }
}

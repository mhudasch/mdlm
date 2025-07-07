using Verticular.DownloadManager.Cli;

public class BeginDownloadCommand : IDownloadCommand
{
  public required Uri Uri { get; init; }
  public required DownloadRequestOptions RequestOptions { get; init; }
  public async Task<IDownloadCommandResult> Handle(CancellationToken cancellationToken)
  {
    var commandId = Guid.NewGuid().ToString("N");
    var clientFactory = new DefaultDownloadClientFactory(); // from outside
    var downloadClient = await clientFactory.CreateClient(this.Uri, this.RequestOptions, cancellationToken);
    var preflightResult = await downloadClient.Preflight(this.Uri, cancellationToken);

    // extract client from uri scheme
    // execute preflight
    return new BeginDownloadCommandResult
    {
      Id = commandId,
      State = DownloadCommandState.Ended
    };
  }
}

public class DownloadRequestOptions
{
  public required string? ProtocolVersion { get; init; }
  public required TransportSecurityOptions TransportSecurityOptions { get; init; }
  public required AccessSecurityOptions AccessSecurityOptions { get; init; }

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

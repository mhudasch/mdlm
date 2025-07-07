using Verticular.DownloadManager.Cli;

var uri = "https://raw.githubusercontent.com/mhudasch/Verticular.Extensions.RandomStrings/refs/heads/master/.vscode/settings.json";

// var clientFactory = new DefaultTransportClientFactory();
// var client = await clientFactory.CreateTransportClient(new HttpsTransportClientConfiguration());

// await client.Download(uri);


var cmd = new BeginDownloadCommand
{
  Uri = new Uri(uri),
  RequestOptions = new()
  {
    TransportSecurityOptions = new(),
    AccessSecurityOptions = new(),
    ProtocolVersion = null
  }
};
var proc = new DownloadCommandProcessor();
await proc.RunAsync([cmd]);

public sealed class DownloadCommandProcessor
{
  public async Task RunAsync(IReadOnlyCollection<IDownloadCommand> downloadCommands, CancellationToken cancellationToken = default)
  {
    foreach (var command in downloadCommands)
    {
      try
      {
        await command.Handle(cancellationToken);
      }
      catch (Exception ex) when (ex is TaskCanceledException or AggregateException)
      {
        // do something
      }
    }
  }
}

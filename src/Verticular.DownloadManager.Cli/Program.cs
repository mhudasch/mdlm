using Verticular.DownloadManager.Cli;

var uri = "https://raw.githubusercontent.com/mhudasch/Verticular.Extensions.RandomStrings/refs/heads/master/.vscode/settings.json";

var clientFactory = new DefaultTransportClientFactory();
var client = await clientFactory.CreateTransportClient(new HttpsTransportClientConfiguration());

await client.Download(uri);

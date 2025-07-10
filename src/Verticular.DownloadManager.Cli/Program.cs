using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Verticular.DownloadManager.Cli;

var uri = "https://raw.githubusercontent.com/mhudasch/Verticular.Extensions.RandomStrings/refs/heads/master/.vscode/settings.json";

// var cmd = new BeginDownloadCommand
// {
//   Uri = new Uri(uri),
//   RequestOptions = new()
//   {
//     TransportSecurityOptions = new(),
//     AccessSecurityOptions = new(),
//     ProtocolVersion = null
//   }
// };
// var proc = new DownloadManager();
// await proc.RunAsync([cmd]);

// public sealed class DownloadManager
// {
//   // implement start and wait scenario (service mode)
//   // - receive commands an act accordingly

//   // implement Download (list of uris in parallel)
//   // implement Download (job file)
//   // implement Download

//   // public async Task RunAsync(IReadOnlyCollection<IDownloadCommand> downloadCommands, CancellationToken cancellationToken = default)
//   // {
//   //   foreach (var command in downloadCommands)
//   //   {
//   //     try
//   //     {
//   //       await command.Handle(cancellationToken);
//   //     }
//   //     catch (Exception ex) when (ex is TaskCanceledException or AggregateException)
//   //     {
//   //       // do something
//   //     }
//   //   }
//   // }

//   private static ConcurrentQueue<Task> workers = [];
//   private static CancellationTokenSource? serviceCancellationTokenSource;

//   // start one or more downloads and wait for completion
//   public static void BeginDownload(IReadOnlyCollection<BeginDownloadCommand> downloadCommands)
//   {
//     foreach (var downloadCommand in downloadCommands)
//     {
//       workers.Enqueue(downloadCommand.Handle(serviceCancellationTokenSource?.Token ?? default));
//     }
//   }

//   public static void StartService()
//   {
//     serviceCancellationTokenSource = new();
//     Console.CancelKeyPress += (_, e) =>
//     {
//       Console.WriteLine("Got service stop signal. Stopping gracefully...");
//       e.Cancel = true;
//       StopService();
//     };
//     Console.WriteLine("Service started. Waiting for Ctrl+C or other signal to end...");
//   }

//   public static void StopService()
//   {
//     serviceCancellationTokenSource?.Cancel(true);
//     Console.WriteLine("Got service stop signal. Stopping gracefully...");
//   }

//   public static bool ProcessDownloads(TimeSpan timeout)
//   {
//     serviceCancellationTokenSource = new();

//     try
//     {
//       Task.WaitAll([.. workers], serviceCancellationTokenSource.Token);
//       return true;
//     }
//     catch (OperationCanceledException)
//     {
//       return false;
//     }
//   }

//   public static bool ProcessDownloads()
//   => ProcessDownloads(Timeout.InfiniteTimeSpan);

//   // this is here somehow
//   // public async Task<IDownloadCommandResult> Handle(CancellationToken cancellationToken)
//   // {
//   //   var commandId = Guid.NewGuid().ToString("N");
//   //   var clientFactory = new DefaultDownloadClientFactory(); // from outside
//   //   var downloadClient = await clientFactory.CreateClient(this.Uri, this.RequestOptions, cancellationToken);
//   //   var preflightResult = await downloadClient.Preflight(this.Uri, cancellationToken);

//   //   // extract client from uri scheme
//   //   // execute preflight
//   //   return new BeginDownloadCommandResult
//   //   {
//   //     Id = commandId,
//   //     State = DownloadCommandState.Ended
//   //   };
//   // }
// }

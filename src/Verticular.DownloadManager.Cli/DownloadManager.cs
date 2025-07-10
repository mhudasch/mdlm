
namespace Verticular.DownloadManager.Cli;

using System.Collections.Concurrent;
using System.Security;

public class DownloadManager
{
  private static readonly ConcurrentQueue<Func<Task<(DownloadCommandResult? Result, DownloadManagerError? Error)>>> commandQueue;
  private static readonly ConcurrentBag<Task<(DownloadCommandResult? Result, DownloadManagerError? Error)>> commandTasks;
  private static readonly Task commandProcessorTask;
  private static readonly CancellationTokenSource managerTokenSource;
  private static readonly SemaphoreSlim commandArrivalSemaphore;
  private static bool isRunningAsService;

  public static DownloadManager()
  {
    isRunningAsService = false;
    managerTokenSource = new();
    commandQueue = new();
    commandArrivalSemaphore = new(0);
    commandProcessorTask = Task.Run(ProcessCommands, managerTokenSource.Token);
  }


  public static (DownloadCommandResult? Result, DownloadManagerError? Error) Execute(BeginDownloadCommand command)
  {
    // dispatch the command
    commandQueue.Enqueue(() => BeginDownload(command));

    // announce arrival of a new command
    commandArrivalSemaphore.Release();

    // wait for the command to finish and return the result
    return WaitForAllCommandsToEnd().GetAwaiter().GetResult().Single();
  }

  public static async Task<(DownloadCommandResult? Result, DownloadManagerError? Error)> Execute(BeginDownloadCommand command, CancellationToken cancellationToken)
  {
    // dispatch the command
    commandQueue.Enqueue(() => BeginDownload(command));

    // announce arrival of a new command
    commandArrivalSemaphore.Release();

    // wait for the command to finish and return the result
    return (await WaitForAllCommandsToEnd(cancellationToken)).Single();
  }

  private static async Task ProcessCommands()
  {
    // command proccessing loop
    while (!managerTokenSource.Token.IsCancellationRequested)
    {
      try
      {
        // we passively wait for a new command in the command queue
        // but when cancellation is triggered also cancel waiting for commands
        await commandArrivalSemaphore.WaitAsync(managerTokenSource.Token);

        if (commandQueue.TryDequeue(out var command))
        {
          // do not execute the command here only create it and push it down the road
          commandTasks.Add(command());
        }
      }
      catch (OperationCanceledException)
      {
        // this is expected when command proccessing should end
        break;
      }
    }
  }

  private static async Task<(DownloadCommandResult? Result, DownloadManagerError? Error)[]> WaitForAllCommandsToEnd(TimeSpan? timeout = null)
  {

    if (timeout.HasValue)
    {
      // wait for all of them to finish in specific time
      using var cts = new CancellationTokenSource(timeout.Value);
      return await WaitForAllCommandsToEnd(cts.Token);
    }

    // wait forever
    return await WaitForAllCommandsToEnd(new CancellationToken());
  }

  private static async Task<(DownloadCommandResult? Result, DownloadManagerError? Error)[]> WaitForAllCommandsToEnd(CancellationToken cancellationToken)
  {
    // snapshot the executions
    var tasks = commandTasks.ToArray();
    return await Task.WhenAll(tasks).WaitAsync(cancellationToken);
  }

  private static Task<(DownloadCommandResult? Result, DownloadManagerError? Error)> BeginDownload(BeginDownloadCommand command)
  {
    return Task.Run(async () =>
    {
      var cmd = command; // closure capture
      var commandId = Guid.NewGuid().ToString("N");
      IDownloadClientFactory clientFactory = new DefaultDownloadClientFactory(); // from outside
      ISegmentCalculator segmentCalculator = new DefaultSegmentCalculator();

      if (cmd.AlternativeLocations is null)
      {
        var location = cmd.Location;
        IDownloadClient client = await clientFactory.CreateClient(location);
        var preflightResult = client.Preflight();
        
      }
      else
      {
        IMirrorSelector mirrorSelector = new CycleMirrorSelector([cmd.Location, .. (cmd.AlternativeLocations ?? [])]);
        var location = mirrorSelector.GetNextLocation();
      }


    });
  }
}

public class DownloadManagerError
{

}

public class DownloadCommandResult
{

}

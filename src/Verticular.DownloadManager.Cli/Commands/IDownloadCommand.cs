public interface IDownloadCommand
{
  Task<IDownloadCommandResult> Handle(CancellationToken cancellationToken);
}

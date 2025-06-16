namespace Verticular.DownloadManager;

internal sealed class Downloader
{
    private readonly string destinationFilePath;
    private readonly uint requestedSegmentCount;
    private readonly IDownloadSegmentCalculator segmentCalculator;
    private readonly IDownloadMirrorSelector mirrorSelector;
    private readonly IProtocolProvider? protocolProvider;
    private readonly List<Thread> threads = [];
    private readonly List<DownloadSegment> segments = [];
    private readonly ResourceLocation resourceLocation;
    private readonly List<ResourceLocation> mirrors;

    private DownloaderState state;
    private Thread? mainThread;

    public event EventHandler StateChanged;
    public event EventHandler InfoReceived;
    public event EventHandler Ending;

    public event EventHandler<DownloadSegmentEventArgs> RestartingSegment;

    public event EventHandler<DownloadSegmentEventArgs> SegmentStopped;

    public event EventHandler<DownloadSegmentEventArgs> SegmentStarting;

    public event EventHandler<DownloadSegmentEventArgs> SegmentStarted;

    public event EventHandler<DownloadSegmentEventArgs> SegmentFailed;

    private Downloader(ResourceLocation toDownload, string destinationFilePath,
        uint segmentCount,
        IProtocolProvider? protocolProvider,
        IDownloadMirrorSelector mirrorSelector,
        IDownloadSegmentCalculator segementCalculator)
    {
        this.resourceLocation = toDownload;
        this.destinationFilePath = destinationFilePath;
        this.requestedSegmentCount = segmentCount;
        this.mirrorSelector = mirrorSelector;
        this.segmentCalculator = segementCalculator;
        this.protocolProvider = protocolProvider;
    }

    public Downloader(ResourceLocation toDownload, string destinationFilePath,
        uint? segmentCount = null,
        ResourceLocation[]? downloadAlternatives = null,
        IProtocolProviderFactory? protocolProviderFactory = null,
        IDownloadMirrorSelector? mirrorSelector = null,
        IDownloadSegmentCalculator? segementCalculator = null) :
        this(toDownload, destinationFilePath,
        segmentCount ?? Constants.MIN_DOWNLOAD_SEGMENT_COUNT,
        (protocolProviderFactory ?? new DefaultProtocolProviderFactory()).Create(toDownload),
        mirrorSelector ?? new SequentialMirrorSelector([toDownload, .. (downloadAlternatives ?? [])]),
        segementCalculator ?? new DefaultDownloadSegmentCalculator())
    {
        this.mirrors = [.. downloadAlternatives ?? []];
        SetState(DownloaderState.NeedsToPrepare);
    }

    public void Start()
    {
        if (state is DownloaderState.NeedsToPrepare)
        {
            SetState(DownloaderState.Preparing);
            StartToPrepare();
        }
        else if (state is not DownloaderState.Preparing
            and not DownloaderState.Pausing
            and not DownloaderState.Working
            and not DownloaderState.WaitingForReconnect)
        {
            SetState(DownloaderState.Preparing);
            StartPrepared();
        }
    }

    private void StartToPrepare()
    {
        this.mainThread = new(new ParameterizedThreadStart((s) => this.StartDownload((uint)s!)));
        this.mainThread.Start(this.requestedSegmentCount);
    }


    private void StartPrepared()
    {
        this.mainThread = new Thread(this.RestartDownload);
        this.mainThread.Start();
    }

    private void StartDownload(uint segmentCount)
    {
        this.SetState(DownloaderState.Preparing);
        var location = this.mirrorSelector.GetNextResourceLocation();
        if (this.protocolProvider is null)
        {
            throw new InvalidOperationException($"No handler found that could handle requests for scheme: '{location.ResourceIdentifier.Scheme}'");
        }

        var selectedSegmentCount = Math.Min(segmentCount, Constants.MIN_DOWNLOAD_SEGMENT_COUNT);
        Stream? downloadContentStream = null;
        var currentTry = 0;
        Exception? lastError = null;
        RemoteFileInfo remoteFileInfo;

        do
        {
            lastError = null;

            if (state == DownloaderState.Pausing)
            {
                SetState(DownloaderState.NeedsToPrepare);
                return;
            }

            SetState(DownloaderState.Preparing);
            currentTry++;
            try
            {

                remoteFileInfo = this.protocolProvider.GetRemoteFileInfo(location, out downloadContentStream);
                break;
            }
            catch (TaskCanceledException)
            {
                SetState(DownloaderState.NeedsToPrepare);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                if (currentTry < Constants.MAX_DOWNLOAD_RETRY_COUNT)
                {
                    SetState(DownloaderState.WaitingForReconnect);
                    Task.Delay(Constants.DEFAULT_RETRY_DELAY).GetAwaiter().GetResult();
                }
                else
                {
                    SetState(DownloaderState.NeedsToPrepare);
                    return;
                }
            }
        }
        while (true);

        try
        {
            lastError = null;
            StartSegments(segmentCount, downloadContentStream, remoteFileInfo);
        }
        catch (ThreadAbortException)
        {
            throw;
        }
        catch (Exception ex)
        {
            lastError = ex;
            SetState(DownloaderState.EndedWithError);
        }
    }

    private void StartSegments(uint segmentCount, Stream downloadContentStream, RemoteFileInfo downloadContentInfo)
    {
        // notifies
        this.InfoReceived?.Invoke(this, EventArgs.Empty);

        // allocs the file on disk
        var downloadContentLength = downloadContentInfo.FileSize;
        var destinationFilePath = AllocLocalFile(downloadContentLength);

        CalculatedSegment[] calculatedSegments;

        if (!downloadContentInfo.AcceptRanges)
        {
            calculatedSegments = [new CalculatedSegment(0, downloadContentLength)];
        }
        else
        {
            calculatedSegments = this.segmentCalculator.Calculate(segmentCount, downloadContentLength);
        }

        for (int i = 0; i < calculatedSegments.Length; i++)
        {
            var segment = new DownloadSegment();
            if (i == 0)
            {
                segment.InputStream = downloadContentStream;
            }

            segment.Index = i;
            segment.InitialStartPosition = calculatedSegments[i].Start;
            segment.StartPosition = calculatedSegments[i].Start;
            segment.EndPosition = calculatedSegments[i].End;

            segments.Add(segment);
        }

        RunSegments(destinationFilePath, downloadContentInfo);
    }

    private void RunSegments(string destinationFilePath, RemoteFileInfo downloadContentInfo)
    {
        SetState(DownloaderState.Working);

        using (var fs = new FileStream(destinationFilePath, FileMode.Open, FileAccess.Write))
        {
            for (int i = 0; i < this.segments.Count; i++)
            {
                this.segments[i].OutputStream = fs;
                StartSegment(this.segments[i], downloadContentInfo);
            }

            do
            {
                while (!AllWorkersStopped(1000)) ;
            }
            while (RestartFailedSegments(downloadContentInfo));
        }

        for (int i = 0; i < this.segments.Count; i++)
        {
            if (this.segments[i].State == DownloadSegmentState.Error)
            {
                SetState(DownloaderState.EndedWithError);
                return;
            }
        }

        if (this.state != DownloaderState.Pausing)
        {
            this.Ending?.Invoke(this, EventArgs.Empty);
        }

        SetState(DownloaderState.Ended);
    }

    private void RestartDownload()
    {
        throw new NotImplementedException();
    }

    private void SetState(DownloaderState state)
    {
        this.state = state;
        this.StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private string AllocLocalFile(ulong projectedFileSize)
    {
        var destinationFilePath = this.destinationFilePath;
        var fileInfo = new FileInfo(this.destinationFilePath);
        var directoryName = fileInfo.DirectoryName!;
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        if (fileInfo.Exists)
        {
            // auto rename the file...
            int count = 1;

            string fileName = Path.GetFileNameWithoutExtension(destinationFilePath);
            string ext = Path.GetExtension(destinationFilePath);
            string newFileName;

            do
            {
                newFileName = Path.Combine(directoryName, $"{fileName}({count++}).{ext}");
            }
            while (File.Exists(newFileName));
            destinationFilePath = newFileName;
        }

        using (var fs = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
        {
            fs.SetLength((long)Math.Max(projectedFileSize, 0ul));
        }
        return destinationFilePath;
    }
    private bool RestartFailedSegments(RemoteFileInfo downloadContentInfo)
    {
        bool hasErrors = false;
        double delay = 0;

        for (int i = 0; i < this.segments.Count; i++)
        {
            if (this.segments[i].State == DownloadSegmentState.Error &&
                this.segments[i].LastErrorDateTime != DateTime.MinValue &&
                (Constants.MAX_DOWNLOAD_RETRY_COUNT == 0 ||
                this.segments[i].CurrentTry < Constants.MAX_DOWNLOAD_RETRY_COUNT))
            {
                hasErrors = true;
                TimeSpan ts = DateTime.Now - this.segments[i].LastErrorDateTime;

                if (ts >= Constants.DEFAULT_RETRY_DELAY)
                {
                    this.segments[i].CurrentTry++;
                    StartSegment(this.segments[i], downloadContentInfo);
                    this.RestartingSegment?.Invoke(this, new(this, this.segments[i]));
                }
                else
                {
                    delay = Math.Max(delay, (Constants.DEFAULT_RETRY_DELAY - ts).TotalMilliseconds);
                }
            }
        }

        Thread.Sleep((int)delay);

        return hasErrors;
    }

    private void StartSegment(DownloadSegment newSegment, RemoteFileInfo downloadContentInfo)
    {
        Thread segmentThread = new Thread(new ParameterizedThreadStart(this.SegmentThreadProc));
        segmentThread.Start(Tuple.Create(newSegment, downloadContentInfo));

        lock (threads)
        {
            threads.Add(segmentThread);
        }
    }

    private bool AllWorkersStopped(int timeOut)
    {
        bool allFinished = true;

        Thread[] workers;

        lock (threads)
        {
            workers = threads.ToArray();
        }

        foreach (Thread t in workers)
        {
            bool finished = t.Join(timeOut);
            allFinished = allFinished & finished;

            if (finished)
            {
                lock (threads)
                {
                    threads.Remove(t);
                }
            }
        }

        return allFinished;
    }

    private void SegmentThreadProc(object? obj)
    {
        var tuple = (Tuple<DownloadSegment, RemoteFileInfo>)obj!;
        DownloadSegment segment = tuple.Item1;
        RemoteFileInfo remoteFileInfo = tuple.Item2;

        try
        {
            if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
            {
                segment.State = DownloadSegmentState.Finished;

                // raise the event
                this.SegmentStopped?.Invoke(this, new(this, segment));

                return;
            }

            int buffSize = 8192;
            byte[] buffer = new byte[buffSize];

            segment.State = DownloadSegmentState.Connecting;

            // raise the event 
            this.SegmentStarting?.Invoke(this, new(this, segment));

            if (segment.InputStream == null)
            {
                // get the next URL (It can be the the main url or some mirror)
                ResourceLocation location = this.mirrorSelector.GetNextResourceLocation();
                // get the protocol provider for that mirror
                IProtocolProvider provider = this.protocolProvider!;

                while (location != this.resourceLocation)
                {
                    Stream tempStream;

                    // get the remote file info on mirror
                    RemoteFileInfo tempRemoteInfo = provider.GetRemoteFileInfo(location, out tempStream);
                    tempStream?.Dispose();

                    // check if the file on mirror is the same
                    if (tempRemoteInfo.FileSize == remoteFileInfo.FileSize &&
                        tempRemoteInfo.AcceptRanges == remoteFileInfo.AcceptRanges)
                    {
                        // if yes, stop looking for the mirror
                        break;
                    }

                    lock (mirrors)
                    {
                        // the file on the mirror is not the same, so remove from the mirror list
                        mirrors.Remove(location);
                    }

                    // the file on the mirror is different
                    // so get other mirror to use in the segment
                    location = this.mirrorSelector.GetNextResourceLocation();
                    //provider = location.BindProtocolProviderInstance(this);
                }

                // get the input stream from start position
                segment.InputStream = provider.CreateStream(location, segment.StartPosition, segment.EndPosition);

                // change the segment URL to the mirror URL
                segment.CurrentURL = location.ResourceIdentifier;
            }
            else
            {
                //  change the segment URL to the main URL
                segment.CurrentURL = this.resourceLocation.ResourceIdentifier;
            }

            using (segment.InputStream)
            {
                // raise the event
                this.SegmentStarted?.Invoke(this, new(this, segment));

                // change the segment state
                segment.State = DownloadSegmentState.Downloading;
                segment.CurrentTry = 0;

                ulong readSize;

                do
                {
                    // reads the buffer from input stream
                    readSize = (ulong)segment.InputStream.Read(buffer, 0, buffSize);

                    // check if the segment has reached the end
                    if (segment.EndPosition > 0 &&
                        segment.StartPosition + readSize > segment.EndPosition)
                    {
                        // adjust the 'readSize' to write only necessary bytes
                        readSize = (segment.EndPosition - segment.StartPosition);
                        if (readSize <= 0)
                        {
                            segment.StartPosition = segment.EndPosition;
                            break;
                        }
                    }

                    // locks the stream to avoid that other threads changes
                    // the position of stream while this thread is writing into the stream
                    lock (segment.OutputStream)
                    {
                        segment.OutputStream.Position = (long)segment.StartPosition;
                        segment.OutputStream.Write(buffer, 0, (int)readSize);
                    }

                    // increse the start position of the segment and also calculates the rate
                    segment.IncreaseStartPosition(readSize);

                    // check if the stream has reached its end
                    if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                    {
                        segment.StartPosition = segment.EndPosition;
                        break;
                    }

                    // check if the user have requested to pause the download
                    if (state == DownloaderState.Pausing)
                    {
                        segment.State = DownloadSegmentState.Paused;
                        break;
                    }

                    //Thread.Sleep(1500);
                }
                while (readSize > 0);

                if (segment.State == DownloadSegmentState.Downloading)
                {
                    segment.State = DownloadSegmentState.Finished;

                    // try to create other segment, 
                    // spliting the missing bytes from one existing segment
                    AddNewSegmentIfNeeded(remoteFileInfo);
                }
            }

            // raise the event
            this.SegmentStopped?.Invoke(this, new(this, segment));
        }
        catch (Exception ex)
        {
            // store the error information
            segment.State = DownloadSegmentState.Error;
            segment.LastError = ex;

            //Debug.WriteLine(ex.ToString());

            // raise the event
            this.SegmentFailed?.Invoke(this, new(this, segment));
        }
        finally
        {
            // clean up the segment
            segment.InputStream = null;
        }
    }

    private void AddNewSegmentIfNeeded(RemoteFileInfo downloadContentInfo)
    {
        lock (this.segments)
        {
            for (int i = 0; i < this.segments.Count; i++)
            {
                DownloadSegment oldSegment = this.segments[i];
                if (oldSegment.State == DownloadSegmentState.Downloading &&
                    oldSegment.Left.TotalSeconds > Constants.MIN_SEGMENT_LEFT_TO_START_NEW_SEGMENT &&
                    oldSegment.MissingTransfer / 2 >= Constants.MIN_DOWNLOAD_SEGMENT_SIZE)
                {
                    // get the half of missing size of oldSegment
                    ulong newSize = oldSegment.MissingTransfer / 2;

                    // create a new segment allocation the half old segment
                    DownloadSegment newSegment = new DownloadSegment();
                    newSegment.Index = this.segments.Count;
                    newSegment.StartPosition = oldSegment.StartPosition + newSize;
                    newSegment.InitialStartPosition = newSegment.StartPosition;
                    newSegment.EndPosition = oldSegment.EndPosition;
                    newSegment.OutputStream = oldSegment.OutputStream;

                    // removes bytes from old segments
                    oldSegment.EndPosition = oldSegment.EndPosition - newSize;

                    // add the new segment to the list
                    segments.Add(newSegment);

                    StartSegment(newSegment, downloadContentInfo);

                    break;
                }
            }
        }
    }
}

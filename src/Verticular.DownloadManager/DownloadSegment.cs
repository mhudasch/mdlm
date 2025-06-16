namespace Verticular.DownloadManager;

internal sealed class DownloadSegment
{
    private ulong initialStartPosition;
    private ulong startPosition;
    private ulong endPosition;
    private int index;
    private Uri currentURL;
    private Stream outputStream;
    private Stream inputStream;
    private Exception lastError;
    private DownloadSegmentState state;
    private bool started = false;
    private DateTime lastReception = DateTime.MinValue;
    private DateTime lastErrorDateTime = DateTime.MinValue;
    private double rate;
    private ulong start;
    private TimeSpan left = TimeSpan.Zero;
    private int currentTry;

    public int CurrentTry
    {
        get { return currentTry; }
        set { currentTry = value; }
    }

    public DownloadSegmentState State
    {
        get
        {
            return state;
        }
        set
        {
            state = value;

            switch (state)
            {
                case DownloadSegmentState.Downloading:
                    BeginWork();
                    break;

                case DownloadSegmentState.Connecting:
                case DownloadSegmentState.Paused:
                case DownloadSegmentState.Finished:
                case DownloadSegmentState.Error:
                    rate = 0.0;
                    left = TimeSpan.Zero;
                    break;
            }
        }
    }

    public DateTime LastErrorDateTime
    {
        get
        {
            return lastErrorDateTime;
        }
    }

    public Exception LastError
    {
        get
        {
            return lastError;
        }
        set
        {
            if (value != null)
            {
                lastErrorDateTime = DateTime.Now;
            }
            else
            {
                lastErrorDateTime = DateTime.MinValue;
            }
            lastError = value;
        }
    }

    public int Index
    {
        get
        {
            return index;
        }
        set
        {
            index = value;
        }
    }

    public ulong InitialStartPosition
    {
        get
        {
            return initialStartPosition;
        }
        set
        {
            initialStartPosition = value;
        }
    }

    public ulong StartPosition
    {
        get
        {
            return startPosition;
        }
        set
        {
            startPosition = value;
        }
    }

    public ulong Transfered
    {
        get
        {
            return this.StartPosition - this.InitialStartPosition;
        }
    }

    public ulong TotalToTransfer
    {
        get
        {
            return (this.EndPosition <= 0 ? 0 : this.EndPosition - this.InitialStartPosition);
        }
    }

    public ulong MissingTransfer
    {
        get
        {
            return (this.EndPosition <= 0 ? 0 : this.EndPosition - this.StartPosition);
        }
    }


    public double Progress
    {
        get
        {
            return (this.EndPosition <= 0 ? 0 : ((double)Transfered / (double)TotalToTransfer * 100.0f));
        }
    }

    public ulong EndPosition
    {
        get
        {
            return endPosition;
        }
        set
        {
            endPosition = value;
        }
    }

    public Stream OutputStream
    {
        get
        {
            return outputStream;
        }
        set
        {
            outputStream = value;
        }
    }

    public Stream InputStream
    {
        get
        {
            return inputStream;
        }
        set
        {
            inputStream = value;
        }
    }

    public Uri CurrentURL
    {
        get
        {
            return currentURL;
        }
        set
        {
            currentURL = value;
        }
    }

    public double Rate
    {
        get
        {
            if (this.State == DownloadSegmentState.Downloading)
            {
                IncreaseStartPosition(0);
                return rate;
            }
            else
            {
                return 0;
            }
        }
    }

    public TimeSpan Left
    {
        get
        {
            return left;
        }
    }

    public void BeginWork()
    {
        start = startPosition;
        lastReception = DateTime.Now;
        started = true;
    }

    public void IncreaseStartPosition(ulong size)
    {
        lock (this)
        {
            DateTime now = DateTime.Now;

            startPosition += size;

            if (started)
            {
                TimeSpan ts = (now - lastReception);
                if (ts.TotalSeconds == 0)
                {
                    return;
                }

                // bytes per seconds
                rate = ((double)(startPosition - start)) / ts.TotalSeconds;

                if (rate > 0.0)
                {
                    left = TimeSpan.FromSeconds(MissingTransfer / rate);
                }
                else
                {
                    left = TimeSpan.MaxValue;
                }
            }
            else
            {
                start = startPosition;
                lastReception = now;
                started = true;
            }
        }
    }
}

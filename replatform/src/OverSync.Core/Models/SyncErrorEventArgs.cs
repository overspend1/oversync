namespace OverSync.Core.Models;

public sealed class SyncErrorEventArgs : EventArgs
{
    public SyncErrorEventArgs(string message, Exception exception)
    {
        Message = message;
        Exception = exception;
    }

    public string Message { get; }

    public Exception Exception { get; }
}

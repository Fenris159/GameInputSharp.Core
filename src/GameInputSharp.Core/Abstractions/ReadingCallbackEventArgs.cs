namespace GameInputSharp.Abstractions;

/// <summary>Arguments for reading callbacks. Dispose <see cref="Reading"/> when done to release the native reading.</summary>
public sealed class ReadingCallbackEventArgs
{
    /// <summary>Handle to the new reading. Use manager APIs or interop with Reading.UnsafePointer; then call Reading.Dispose().</summary>
    public GameInputReadingHandle Reading { get; }

    /// <summary>True if the dispatcher fell behind and some readings were skipped.</summary>
    public bool HasOverrunOccurred { get; }

    internal ReadingCallbackEventArgs(GameInputReadingHandle reading, bool hasOverrunOccurred)
    {
        Reading = reading ?? throw new ArgumentNullException(nameof(reading));
        HasOverrunOccurred = hasOverrunOccurred;
    }
}

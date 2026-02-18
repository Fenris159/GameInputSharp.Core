// Wraps a reading pointer from RegisterReadingCallback or GetCurrentReading. Call Dispose to release.

namespace GameInputSharp.Abstractions;

/// <summary>Holds a native reading pointer. Dispose to release the reading (required to avoid leaks).</summary>
/// <remarks>Use with <see cref="ReadingCallbackEventArgs"/> or when using GetNextReading/GetPreviousReading. Obtain state via <see cref="GameInputManager"/> or interop; then call <see cref="Dispose"/>.</remarks>
public sealed class GameInputReadingHandle : IDisposable
{
    private IntPtr _ptr;
    private Action<IntPtr>? _release;

    /// <summary>Creates a handle that will call the given release action when disposed.</summary>
    /// <param name="ptr">Native reading pointer.</param>
    /// <param name="release">Called with the pointer when Dispose is called (e.g. to release the reading).</param>
    public GameInputReadingHandle(IntPtr ptr, Action<IntPtr> release)
    {
        _ptr = ptr;
        _release = release ?? throw new ArgumentNullException(nameof(release));
    }

    /// <summary>Native reading pointer for use with interop or manager APIs that accept a reading.</summary>
    public IntPtr UnsafePointer => _ptr;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_release != null)
        {
            _release(_ptr);
            _release = null;
        }
        _ptr = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }
}

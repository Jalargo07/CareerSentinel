using System.Text;

namespace CareerSentinel.Services;

/// <summary>
/// Buffered async file logger. Batches writes in memory and flushes periodically
/// or when the buffer exceeds a threshold, so callers never block on disk I/O.
/// </summary>
public sealed class AsyncFileLogger : IDisposable
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Timer _flushTimer;
    private readonly StringBuilder _buffer = new();
    private readonly int _flushThresholdBytes;

    private StreamWriter? _writer;
    private bool _disposed;

    /// <summary>
    /// Creates an async file logger that appends to <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath">Full path to the log file.</param>
    /// <param name="flushInterval">How often to flush buffered content to disk.</param>
    /// <param name="flushThresholdBytes">Buffer size that triggers an immediate flush.</param>
    public AsyncFileLogger(
        string filePath,
        TimeSpan? flushInterval = null,
        int flushThresholdBytes = 4096)
    {
        _filePath = filePath;
        _flushThresholdBytes = flushThresholdBytes;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _flushTimer = new Timer(
            _ => FlushAsync().ConfigureAwait(false),
            null,
            flushInterval ?? TimeSpan.FromSeconds(3),
            flushInterval ?? TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// Appends <paramref name="entry"/> to the in-memory buffer.
    /// The content is written to disk by the background flush timer or when the buffer exceeds its threshold.
    /// </summary>
    public async Task AppendAsync(string entry, CancellationToken ct = default)
    {
        if (_disposed) return;

        await _semaphore.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _buffer.AppendLine(entry);

            if (_buffer.Length >= _flushThresholdBytes)
            {
                await FlushBufferAsync(ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Forces any buffered content to be flushed to disk immediately.
    /// </summary>
    public async Task FlushAsync()
    {
        if (_disposed) return;

        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await FlushBufferAsync().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task FlushBufferAsync(CancellationToken ct = default)
    {
        if (_buffer.Length == 0) return;

        try
        {
            _writer ??= CreateWriter();
            await _writer.WriteAsync(_buffer.ToString()).ConfigureAwait(false);
            await _writer.FlushAsync(ct).ConfigureAwait(false);
            _buffer.Clear();
        }
        catch (Exception)
        {
            // If the writer gets into a bad state, discard it and recreate on next flush.
            _writer?.Dispose();
            _writer = null;
        }
    }

    private StreamWriter CreateWriter()
    {
        var stream = new FileStream(
            _filePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        return new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer.Dispose();

        // Best-effort final flush (synchronous, called from Dispose).
        _semaphore.Wait();
        try
        {
            if (_buffer.Length > 0 && _writer is not null)
            {
                _writer.Write(_buffer.ToString());
                _writer.Flush();
                _buffer.Clear();
            }
        }
        finally
        {
            _writer?.Dispose();
            _writer = null;
            _semaphore.Release();
        }

        _semaphore.Dispose();
    }
}

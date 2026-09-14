using System.Collections.Concurrent;

namespace ContosoDashboard.Services;

public sealed class LocalScanQueue : IScanQueue
{
    private readonly ConcurrentQueue<ScanQueueMessage> _messages = new();

    public Task EnqueueAsync(ScanQueueMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public bool TryDequeue(out ScanQueueMessage? message) => _messages.TryDequeue(out message);
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ChannelsExample
{
    public class DummyChannel<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        
        public void Write(T value)
        {
            _queue.Enqueue(value);
            _semaphore.Release();
        }
        
        public async ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            _queue.TryDequeue(out var item);
            return item;
        }
    }
}
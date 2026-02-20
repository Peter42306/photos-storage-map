using PhotosStorageMap.Application.Interfaces;
using System.Threading.Channels;

namespace PhotosStorageMap.Infrastructure.BackgroundProcessing
{
    public sealed class InMemoryPhotoProcessingQueue : IPhotoProcessingQueue
    {
        //private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

        private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
            new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        public ValueTask EnqueueAsync(Guid photoId, CancellationToken ct = default)
        {
            return _channel.Writer.WriteAsync(photoId, ct);
        }

        public ValueTask<Guid> DequeueAsync(CancellationToken ct = default)
        {
            return _channel.Reader.ReadAsync(ct);
        }        
    }
}

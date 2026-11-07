using PhotosStorageMap.Application.DTOs;
using PhotosStorageMap.Application.Interfaces;
using System.Threading.Channels;

namespace PhotosStorageMap.Infrastructure.Services
{
    public sealed class ZipJobQueue : IZipJobQueue
    {
        private readonly Channel<ZipJobRequest> _channel;

        public ZipJobQueue()
        {
            var options = new BoundedChannelOptions(5)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<ZipJobRequest>(options);
        }

        public bool TryEnqueue(ZipJobRequest request)
        {
            return _channel.Writer.TryWrite(request);
        }

        public ValueTask<ZipJobRequest> DequeueAsync(CancellationToken ct)
        {
            return _channel.Reader.ReadAsync(ct);
        }
    }
}

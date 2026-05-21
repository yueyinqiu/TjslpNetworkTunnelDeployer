using CliFx.Infrastructure;
using CliWrap;
using CliWrap.Buffered;

namespace TjslpNetworkTunnelDeployer.Extensions;

internal static class StreamExtensions
{
    extension(Stream stream)
    {
        public async Task ToFileUnbufferedAsync(
            string destination, CancellationToken cancellationToken = default)
        {
            using var file = new FileStream(
                destination,
                FileMode.Create, FileAccess.Write, FileShare.Read,
                4096, FileOptions.WriteThrough);

            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                await file.FlushAsync(cancellationToken);
            }
        }
    }
}

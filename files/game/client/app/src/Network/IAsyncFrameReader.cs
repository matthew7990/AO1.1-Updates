using System.Threading;
using System.Threading.Tasks;

namespace Argentum.Client.Network;

public interface IAsyncFrameReader
{
    Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken = default);
}

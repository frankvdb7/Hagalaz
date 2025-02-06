using System.Threading;
using System.Threading.Tasks;
using Raido.Common.Protocol;

namespace Raido.Server
{
    public interface IRaidoClientProxy
    {
        Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : RaidoMessage;
    }
}
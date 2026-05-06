using System.Threading.Channels;
using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.State;

public interface IWindroseStateStore
{
    WindroseServerState GetState();
    void SetLogAvailable(bool available, string? error = null);
    void Apply(WindroseEvent evt);
    void UpdateSaveMetadata(SaveMetadata save);
    ChannelReader<WindroseEvent> Subscribe(CancellationToken cancellationToken);
}

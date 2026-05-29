using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Services;

public interface IWindroseStateHubPublisher
{
    Task PublishAsync(WindroseStateChange change, CancellationToken cancellationToken);
}

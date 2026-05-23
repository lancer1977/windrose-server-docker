using Windrose.StateWeb.Domain;

namespace Windrose.StateWeb.Services;

public interface IWindroseLivePushPublisher
{
    Task PublishAsync(WindroseStateChange change, CancellationToken cancellationToken);
}

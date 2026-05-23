using Windrose.StateWeb.Core.Contracts;

namespace Windrose.StateWeb.Core.Abstractions;

public interface IWindroseHistorySource
{
    IReadOnlyList<WindroseTimelineEntry> RecentHistory { get; }
}

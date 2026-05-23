using Windrose.StateWeb.Options;
using Windrose.StateWeb.Parsing;
using Windrose.StateWeb.State;
using Microsoft.Extensions.Options;

namespace Windrose.StateWeb.Services;

public sealed class WindroseLogTailer(
    IOptions<WindroseStateOptions> options,
    IWindroseLogParser parser,
    IWindroseStateStore stateStore,
    ILogger<WindroseLogTailer> logger) : BackgroundService
{
    private readonly WindroseStateOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var position = 0L;
        var lastLength = 0L;
        var lastWriteTimeUtc = DateTimeOffset.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!File.Exists(_options.LogPath))
                {
                    stateStore.SetLogAvailable(false, $"Log file not found at {_options.LogPath}");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                var info = new FileInfo(_options.LogPath);
                var currentWriteTimeUtc = info.LastWriteTimeUtc;
                if (_options.TailFromEnd && position == 0)
                {
                    position = info.Length;
                }
                else if (info.Length < lastLength || (currentWriteTimeUtc != lastWriteTimeUtc && info.Length <= lastLength))
                {
                    position = 0;
                }

                lastLength = info.Length;
                lastWriteTimeUtc = currentWriteTimeUtc;
                stateStore.SetLogAvailable(true);

                using var stream = new FileStream(_options.LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                stream.Seek(position, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync(stoppingToken)) is not null)
                {
                    position = stream.Position;
                    var evt = parser.ParseLine(line);
                    if (evt is not null)
                    {
                        stateStore.Apply(evt);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed while tailing Windrose log {LogPath}", _options.LogPath);
                stateStore.SetLogAvailable(false, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}

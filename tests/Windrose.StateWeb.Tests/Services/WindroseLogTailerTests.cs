using System.Threading.Channels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Windrose.StateWeb.Domain;
using Windrose.StateWeb.Options;
using Windrose.StateWeb.Parsing;
using Windrose.StateWeb.Services;
using Windrose.StateWeb.State;

namespace Windrose.StateWeb.Tests.Services;

public sealed class WindroseLogTailerTests
{
    [Fact]
    public async Task MarksLogUnavailableWhenTheFileDoesNotExist()
    {
        var root = Path.Combine(Path.GetTempPath(), "windrose-state-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            ServerFilesPath = root,
            LogRelativePath = "R5/Saved/Logs/R5.log"
        });

        var stateStore = new CapturingStateStore();
        var tailer = new WindroseLogTailer(options, new NullWindroseLogParser(), stateStore, NullLogger<WindroseLogTailer>.Instance);

        await tailer.StartAsync(CancellationToken.None);
        await stateStore.FirstAvailability.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(stateStore.LastAvailability);
        Assert.Contains("Log file not found", stateStore.LastError ?? string.Empty);

        await tailer.StopAsync(CancellationToken.None);
        Directory.Delete(root, true);
    }

    [Fact]
    public async Task RewindsAfterLogRotation()
    {
        var root = Path.Combine(Path.GetTempPath(), "windrose-state-tests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(root, "R5", "Saved", "Logs", "R5.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath, "line-one\n");

        var parser = new RotationAwareParser();
        var stateStore = new CapturingStateStore();
        var options = Microsoft.Extensions.Options.Options.Create(new WindroseStateOptions
        {
            ServerFilesPath = root,
            LogRelativePath = "R5/Saved/Logs/R5.log"
        });
        var tailer = new WindroseLogTailer(options, parser, stateStore, NullLogger<WindroseLogTailer>.Instance);

        await tailer.StartAsync(CancellationToken.None);
        await parser.FirstLine.Task.WaitAsync(TimeSpan.FromSeconds(2));

        File.WriteAllText(logPath, "line-two\n");
        await parser.SecondLine.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Equal("line-one", parser.FirstSeenLine);
        Assert.Equal("line-two", parser.SecondSeenLine);

        await tailer.StopAsync(CancellationToken.None);
        Directory.Delete(root, true);
    }

    private sealed class NullWindroseLogParser : IWindroseLogParser
    {
        public WindroseEvent? ParseLine(string line) => null;
    }

    private sealed class RotationAwareParser : IWindroseLogParser
    {
        public TaskCompletionSource<string> FirstLine { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<string> SecondLine { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? FirstSeenLine { get; private set; }
        public string? SecondSeenLine { get; private set; }
        private int _count;

        public WindroseEvent? ParseLine(string line)
        {
            var nextCount = Interlocked.Increment(ref _count);
            if (nextCount == 1)
            {
                FirstSeenLine = line;
                FirstLine.TrySetResult(line);
            }
            else if (nextCount == 2)
            {
                SecondSeenLine = line;
                SecondLine.TrySetResult(line);
            }

            return new WindroseEvent(DateTimeOffset.UtcNow, "Observed", "Information", line);
        }
    }

    private sealed class CapturingStateStore : IWindroseStateStore
    {
        public TaskCompletionSource<bool> FirstAvailability { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool? LastAvailability { get; private set; }
        public string? LastError { get; private set; }

        public WindroseServerState GetState() => new();

        public void SetLogAvailable(bool available, string? error = null)
        {
            LastAvailability = available;
            LastError = error;
            FirstAvailability.TrySetResult(available);
        }

        public void Apply(WindroseEvent evt)
        {
        }

        public void UpdateSaveMetadata(SaveMetadata save)
        {
        }

        public ChannelReader<WindroseEvent> Subscribe(CancellationToken cancellationToken) => throw new NotSupportedException();

        public ChannelReader<WindroseStateChange> SubscribeStateChanges(CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

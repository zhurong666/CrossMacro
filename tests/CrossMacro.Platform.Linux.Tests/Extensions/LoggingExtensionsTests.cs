using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CrossMacro.Platform.Linux.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace CrossMacro.Platform.Linux.Tests.Extensions;

public class LoggingExtensionsTests
{
    private class TestSink : ILogEventSink
    {
        public ConcurrentBag<LogEvent> Events { get; } = new();

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }

    [Fact]
    public void LogOnce_ShouldLogOnlyOnce_WhenCalledMultipleTimesWithSameKey()
    {
        // Arrange
        var sink = new TestSink();
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var key = Guid.NewGuid().ToString();
        var message = "Test message {0}";
        var arg = "Arg";

        // Act
        LoggingExtensions.LogOnce(key, message, arg);
        LoggingExtensions.LogOnce(key, message, arg);
        LoggingExtensions.LogOnce(key, message, arg);

        // Assert
        Assert.Single(sink.Events, e => e.MessageTemplate.Text == message);
    }

    [Fact]
    public void LogOnce_ShouldLogMultipleTimes_WhenCalledWithDifferentKeys()
    {
        // Arrange
        var sink = new TestSink();
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var key1 = Guid.NewGuid().ToString();
        var key2 = Guid.NewGuid().ToString();
        var message = "Test message {0}";
        var arg = "Arg";

        // Act
        LoggingExtensions.LogOnce(key1, message, arg);
        LoggingExtensions.LogOnce(key2, message, arg);

        // Assert
        Assert.Equal(2, sink.Events.Count);
    }
}

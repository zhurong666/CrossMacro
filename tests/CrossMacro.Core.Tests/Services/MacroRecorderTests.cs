namespace CrossMacro.Core.Tests.Services;

using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using FluentAssertions;
using NSubstitute;

/// <summary>
/// Tests for MacroRecorder focusing on initialization and error handling
/// </summary>
public class MacroRecorderTests
{
    private readonly IMousePositionProvider _positionProvider;

    public MacroRecorderTests()
    {
        _positionProvider = Substitute.For<IMousePositionProvider>();
        _positionProvider.IsSupported.Returns(true);
    }

    [Fact]
    public void IsRecording_Initially_IsFalse()
    {
        // Arrange
        var recorder = new MacroRecorder(_positionProvider);

        // Assert
        recorder.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task StartRecordingAsync_NoMouseNoKeyboard_ThrowsArgumentException()
    {
        // Arrange
        var recorder = new MacroRecorder(_positionProvider);

        // Act
        var act = async () => await recorder.StartRecordingAsync(
            recordMouse: false, 
            recordKeyboard: false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least one*");
    }

    [Fact]
    public void StopRecording_WhenNotRecording_ThrowsInvalidOperationException()
    {
        // Arrange
        var recorder = new MacroRecorder(_positionProvider);

        // Act
        var act = () => recorder.StopRecording();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Not currently recording*");
    }

    [Fact]
    public void GetCurrentRecording_WhenNotRecording_ReturnsNull()
    {
        // Arrange
        var recorder = new MacroRecorder(_positionProvider);

        // Act
        var result = recorder.GetCurrentRecording();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var recorder = new MacroRecorder(_positionProvider);

        // Act
        var act = () =>
        {
            recorder.Dispose();
            recorder.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithUnsupportedPositionProvider_DoesNotThrow()
    {
        // Arrange
        var unsupportedProvider = Substitute.For<IMousePositionProvider>();
        unsupportedProvider.IsSupported.Returns(false);

        // Act
        var act = () => new MacroRecorder(unsupportedProvider);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task StartRecordingAsync_CapturesEvents_WhenInputReceived()
    {
        // Arrange
        var capture = Substitute.For<IInputCapture>();
        capture.GetAvailableDevices().Returns(new List<InputDeviceInfo>
        {
            new InputDeviceInfo { IsMouse = true, Name = "Mouse" },
            new InputDeviceInfo { IsKeyboard = true, Name = "Keyboard" }
        });

        // Use a factory that returns our mock
        var recorder = new MacroRecorder(_positionProvider, inputCaptureFactory: () => capture);
        _positionProvider.GetAbsolutePositionAsync().Returns(Task.FromResult<(int X, int Y)?>( (0, 0) ));
        
        var receivedEvents = new List<MacroEvent>();
        recorder.EventRecorded += (s, e) => receivedEvents.Add(e);

        // Act
        await recorder.StartRecordingAsync(true, true);
        
        // Simulate input
        capture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(
            this, 
            new InputCaptureEventArgs { Type = InputEventType.Key, Code = 30, Value = 1 }); // Key Press A

        capture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(
            this, 
            new InputCaptureEventArgs { Type = InputEventType.Key, Code = 30, Value = 0 }); // Key Release A

        recorder.StopRecording();

        // Assert
        receivedEvents.Should().HaveCount(2);
        receivedEvents[0].Type.Should().Be(EventType.KeyPress);
        receivedEvents[0].KeyCode.Should().Be(30);
        receivedEvents[1].Type.Should().Be(EventType.KeyRelease);
        
        var recording = recorder.GetCurrentRecording();
        recording.Should().NotBeNull();
        recording!.Events.Should().HaveCount(2);
    }

    [Fact]
    public async Task StartRecordingAsync_IgnoresKeys_WhenSpecified()
    {
        // Arrange
        var capture = Substitute.For<IInputCapture>();
        capture.GetAvailableDevices().Returns(new List<InputDeviceInfo>
        {
            new InputDeviceInfo { IsMouse = true, Name = "Mouse" },
            new InputDeviceInfo { IsKeyboard = true, Name = "Keyboard" }
        });

        var recorder = new MacroRecorder(_positionProvider, inputCaptureFactory: () => capture);
        _positionProvider.GetAbsolutePositionAsync().Returns(Task.FromResult<(int X, int Y)?>( (0, 0) ));
        
        var receivedEvents = new List<MacroEvent>();
        recorder.EventRecorded += (s, e) => receivedEvents.Add(e);

        // Act
        // Ignore key code 30 (A)
        await recorder.StartRecordingAsync(true, true, ignoredKeys: new[] { 30 });
        
        // Simulate ignored key
        capture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(
            this, new InputCaptureEventArgs { Type = InputEventType.Key, Code = 30, Value = 1 });

        // Simulate allowed key (31 = S)
        capture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(
            this, new InputCaptureEventArgs { Type = InputEventType.Key, Code = 31, Value = 1 });

        recorder.StopRecording();

        // Assert
        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].KeyCode.Should().Be(31);
    }
}

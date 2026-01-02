using System;
using System.Threading;
using System.Threading.Tasks;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using CrossMacro.Core.Services.TextExpansion;
using CrossMacro.Infrastructure.Services;
using NSubstitute;
using Xunit;

namespace CrossMacro.Infrastructure.Tests.Services;

public class TextExpansionServiceTests
{
    private readonly ISettingsService _settingsService;
    private readonly ITextExpansionStorageService _storageService;
    private readonly IInputCapture _inputCapture;
    
    // New Mocks
    private readonly IInputProcessor _inputProcessor;
    private readonly ITextBufferState _bufferState;
    private readonly ITextExpansionExecutor _executor;
    
    private readonly TextExpansionService _service;

    public TextExpansionServiceTests()
    {
        _settingsService = Substitute.For<ISettingsService>();
        _settingsService.Current.Returns(new AppSettings { EnableTextExpansion = true });

        _storageService = Substitute.For<ITextExpansionStorageService>();
        _inputCapture = Substitute.For<IInputCapture>();
        
        _inputProcessor = Substitute.For<IInputProcessor>();
        _bufferState = Substitute.For<ITextBufferState>();
        _executor = Substitute.For<ITextExpansionExecutor>();

        _service = new TextExpansionService(
            _settingsService,
            _storageService,
            () => _inputCapture,
            _inputProcessor,
            _bufferState,
            _executor);
    }

    [Fact]
    public async Task Start_WhenEnabled_StartsInputCaptureAndResetsState()
    {
        // Act
        _service.Start();

        // Assert
        Assert.True(_service.IsRunning);
        _inputCapture.Received(1).Configure(false, true);
        await _inputCapture.Received(1).StartAsync(Arg.Any<CancellationToken>());
        
        _inputProcessor.Received(1).Reset();
        _bufferState.Received(1).Clear();
    }

    [Fact]
    public async Task Start_WhenDisabled_DoesNotStart()
    {
        // Arrange
        _settingsService.Current.Returns(new AppSettings { EnableTextExpansion = false });

        // Act
        _service.Start();

        // Assert
        Assert.False(_service.IsRunning);
        await _inputCapture.DidNotReceive().StartAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Stop_StopsInputCapture()
    {
        // Arrange
        _service.Start();

        // Act
        _service.Stop();

        // Assert
        _inputCapture.Received(1).Stop();
        _inputCapture.Received(1).Dispose();
    }
    
    [Fact]
    public void OnInputReceived_DelegatesToProcessor()
    {
        // Arrange
        _service.Start();
        var eventArgs = new InputCaptureEventArgs { Type = InputEventType.Key, Code = 30, Value = 1 };

        // Act
        _inputCapture.InputReceived += Raise.Event<EventHandler<InputCaptureEventArgs>>(this, eventArgs);

        // Assert
        _inputProcessor.Received(1).ProcessEvent(eventArgs);
    }
}

using CodeChavez.M3diator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeChavez.Mediator.Tests;

public class M3diatorTests
{
    private readonly IServiceCollection _services;

    public M3diatorTests()
    {
        _services = new ServiceCollection();
    }

    [Fact]
    public async Task Handle_WithResponse_ReturnsExpectedResult()
    {
        // Arrange
        var request = new TestQuery();
        var expectedResponse = "Hello Mundo!";
        var handlerMock = new Mock<IRequestHandler<TestQuery, string>>();
        handlerMock.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResponse);

        _services.AddScoped(_ => handlerMock.Object);
        var provider = _services.BuildServiceProvider();
        var mediator = new CodeChavez.M3diator.M3diator(provider);

        // Act
        var result = await mediator.Handle<string>(request);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task Handle_VoidCommand_CallsHandler()
    {
        // Arrange
        var request = new TestCommand();
        var handlerMock = new Mock<IRequestHandler<TestCommand>>();
        handlerMock.Setup(h => h.Handle(request, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask)
                   .Verifiable();

        _services.AddScoped(_ => handlerMock.Object);
        var provider = _services.BuildServiceProvider();
        var mediator = new CodeChavez.M3diator.M3diator(provider);

        // Act
        await mediator.Handle(request);

        // Assert
        handlerMock.Verify();
    }

    [Fact]
    public async Task Publish_Notification_CallsAllHandlers()
    {
        // Arrange
        var notification = new TestNotification();

        var handlerMock1 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock1.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

        var handlerMock2 = new Mock<INotificationHandler<TestNotification>>();
        handlerMock2.Setup(h => h.Handle(notification, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

        _services.AddSingleton(handlerMock1.Object);
        _services.AddSingleton(handlerMock2.Object);
        var provider = _services.BuildServiceProvider();
        var mediator = new CodeChavez.M3diator.M3diator(provider);

        // Act
        await mediator.Publish(notification);

        // Assert
        handlerMock1.Verify();
        handlerMock2.Verify();
    }

    // === Mocks ===

    public class TestQuery : IRequest<string> { }

    public class TestCommand : IRequest { }

    public class TestNotification : INotification { }
}
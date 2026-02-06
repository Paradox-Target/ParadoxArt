using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Initialization;

namespace Hoi4BlueprintBuilder.UnitTests.Services;

[TestFixture(TestOf = typeof(NavigationService))]
public class NavigationServiceTests
{
    private MockServiceProvider _serviceProviderMock;
    private NavigationService _navigationService;

    [SetUp]
    public void Setup()
    {
        _serviceProviderMock = new MockServiceProvider();
        _navigationService = new NavigationService(_serviceProviderMock);
    }

    [AvaloniaTest]
    public void NavigateTo_ShouldUpdateCurrentView_AndTriggerEvent()
    {
        // Arrange
        object expectedView = new();
        _serviceProviderMock.Register(typeof(MainWelcomeView), expectedView);

        object? eventReceivedView = null;
        _navigationService.ViewChanged += view => eventReceivedView = view;

        // Act
        _navigationService.NavigateTo<MainWelcomeView>();

        // Wait for Dispatcher jobs to process (NavigateTo uses Dispatcher.UIThread.Post)
        Dispatcher.UIThread.RunJobs();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_navigationService.CurrentView, Is.EqualTo(expectedView));
            Assert.That(eventReceivedView, Is.EqualTo(expectedView));
        }
    }

    private sealed class MockServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register(Type type, object service) => _services[type] = service;

        public object? GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service;
            }

            // For debugging purposes, throw if missing to catch misconfiguration in tests
            throw new InvalidOperationException(
                $"Service of type {serviceType.Name} was requested but not registered in MockServiceProvider."
            );
        }
    }
}

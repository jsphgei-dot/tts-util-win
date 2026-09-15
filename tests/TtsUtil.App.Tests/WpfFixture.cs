using System.Windows.Threading;
using Xunit;

namespace TtsUtil.App.Tests;

/// <summary>Owns one STA thread with a dispatcher and a WPF Application for all UI tests.</summary>
public sealed class WpfFixture : IDisposable
{
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private Dispatcher? _dispatcher;
    private Exception? _startupFailure;

    public WpfFixture()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "WpfTestThread" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        if (!_ready.Wait(TimeSpan.FromSeconds(60)))
        {
            throw new TimeoutException("The WPF test thread did not start.");
        }

        if (_startupFailure is not null)
        {
            throw new InvalidOperationException("The WPF test thread failed to start.", _startupFailure);
        }
    }

    /// <summary>Runs the action on the UI thread and rethrows anything it throws.</summary>
    public void Invoke(Action action) => _dispatcher!.Invoke(action);

    public T Invoke<T>(Func<T> function) => _dispatcher!.Invoke(function);

    public void Dispose()
    {
        _dispatcher?.InvokeShutdown();
        _thread.Join(TimeSpan.FromSeconds(10));
        _ready.Dispose();
    }

    private void Run()
    {
        try
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            if (System.Windows.Application.Current is null)
            {
                var application = new global::TtsUtil.App.App();
                application.InitializeComponent();
            }
        }
        catch (Exception ex)
        {
            _startupFailure = ex;
            _ready.Set();
            return;
        }

        _ready.Set();
        Dispatcher.Run();
    }
}

[CollectionDefinition("wpf")]
public sealed class WpfCollection : ICollectionFixture<WpfFixture>
{
}

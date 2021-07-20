using System;
using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Live.Avalonia
{
    public sealed class LiveViewHost : Window, IDisposable
    {
        private readonly LiveFileWatcher _assemblyWatcher;
        private readonly LiveSourceWatcher _sourceWatcher;
        private readonly IDisposable _subscription;
        private readonly Action<string> _logger;
        private readonly string _assemblyPath;

        public LiveViewHost(ILiveView view, Action<string> logger)
        {
            _logger = logger;
            _sourceWatcher = new LiveSourceWatcher(logger);
            _assemblyWatcher = new LiveFileWatcher(logger);
            _assemblyPath = view.GetType().Assembly.Location;
            
            var loader = new LiveControlLoader(logger);
            _subscription = _assemblyWatcher
                .FileChanged
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(path => loader.LoadControl(path, this));

            Console.CancelKeyPress += (sender, args) => Clean("Console Ctrl+C key press.", false);
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => Clean("Process termination.", false);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => Clean(args.ExceptionObject.ToString(), true);
        }

        public void StartWatchingSourceFilesForHotReloading()
        {
            _logger("Starting source and assembly file watchers...");
            var (liveAssemblyDir, liveAssemblyFile) = _sourceWatcher.StartRebuildingAssemblySources(_assemblyPath);
            _assemblyWatcher.StartWatchingFileCreation(liveAssemblyDir, liveAssemblyFile);
        }

        public void Dispose()
        {
            _logger("Disposing LiveViewHost internals...");
            _sourceWatcher.Dispose();
            _assemblyWatcher.Dispose();
            _subscription.Dispose();
            _logger("Successfully disposed LiveViewHost internals.");
        }

        private void Clean(string reason, bool exception)
        {
            _logger($"Closing live reloading window due to: {reason}");
            if (exception)
                _logger("\nNote: To prevent your app from crashing, properly handle all exceptions causing a crash.\n" +
                        "If you are using ReactiveUI and ReactiveCommand<,>s, make sure you subscribe to " +
                        "RxApp.DefaultExceptionHandler: https://reactiveui.net/docs/handbook/default-exception-handler\n" +
                        "If you are using another framework, refer to its docs considering global exception handling.\n");
            Dispose();
            Process.GetCurrentProcess().Kill();
        }
    }
}
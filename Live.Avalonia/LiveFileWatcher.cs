using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

namespace Live.Avalonia
{
    internal sealed class LiveFileWatcher : IDisposable
    {
        readonly Subject<string> _fileChanged = new Subject<string>();
        readonly Action<string> _logger;

        FileSystemWatcher watcher = new FileSystemWatcher()
        {
            EnableRaisingEvents = false
        };


        public LiveFileWatcher(Action<string> logger) => _logger = logger;

        public IObservable<string> FileChanged => _fileChanged.Throttle(TimeSpan.FromSeconds(0.5));

        public void StartWatchingFileCreation(string dir, string filePath)
        {
            try
            {
                _logger($"Registering observable file system watcher for file at: {filePath}");
                watcher.Path = dir;
                watcher.Filter = Path.GetFileName(filePath);
                watcher.Changed += OnChanged;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        void OnChanged(object sender, FileSystemEventArgs args)
        {
            _fileChanged.OnNext(args.FullPath);
        }

        public void Dispose()
        {
            _logger("Stopping the file creation watcher timer...");
            _fileChanged.Dispose();
            watcher.Changed -= OnChanged;
            watcher.Dispose();
        }
    }
}
using System;
using System.Diagnostics;
using System.IO;

namespace Live.Avalonia
{
    internal class LiveSourceWatcher : IDisposable
    {
        private readonly Action<string> _logger;
        private Process _dotnetWatchBuildProcess;

        public LiveSourceWatcher(Action<string> logger) => _logger = logger;

        public string StartRebuildingAssemblySources(string assemblyPath)
        {
            _logger("Attempting to run 'dotnet watch' command for assembly sources...");
            var binDirectoryPath = FindAscendantDirectory(assemblyPath, "bin");
            var projectDirectory = Path.GetDirectoryName(binDirectoryPath);
            if (projectDirectory == null)
                throw new IOException($"Unable to parent directory of {binDirectoryPath}");

            var dotnetWatchBuildPath = Path.Combine(projectDirectory, ".live-bin") + Path.DirectorySeparatorChar;
            _logger($"Executing 'dotnet watch' command from {projectDirectory}, building into {dotnetWatchBuildPath}");
            _dotnetWatchBuildProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"watch msbuild /p:BaseOutputPath={dotnetWatchBuildPath}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    WorkingDirectory = projectDirectory
                }
            };
            
            _dotnetWatchBuildProcess.Start();
            _logger($"Successfully managed to start 'dotnet watch' process with id {_dotnetWatchBuildProcess.Id}");
            var separator = Path.DirectorySeparatorChar;
            return assemblyPath.Replace($"{separator}bin{separator}", $"{separator}.live-bin{separator}");
        }
        
        public void Dispose()
        {
            if (_dotnetWatchBuildProcess == null) return;
            _logger($"Killing 'dotnet watch' process {_dotnetWatchBuildProcess.Id} and dependent processes...");
            _dotnetWatchBuildProcess.Kill(true);
        }
        
        private static string FindAscendantDirectory(string filePath, string directoryName)
        {
            var currentPath = filePath;
            while (true)
            {
                currentPath = Path.GetDirectoryName(currentPath);
                if (currentPath == null)
                    throw new IOException($"Unable to get parent directory of {filePath} named {directoryName}");
                    
                var directoryInfo = new DirectoryInfo(currentPath);
                if (directoryName == directoryInfo.Name)
                    return currentPath;
            }
        }
    }
}
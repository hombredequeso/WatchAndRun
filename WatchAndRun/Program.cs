using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

namespace WatchAndRun
{
    internal class Program
    {
        private const int TimerInterval = 2000;
        private const int CommandExecutionDelayAfterChanges = 2000;

        private const string Usage =
            @"Watches a directory and its subdirectries for changes. When a change is detected, it runs a command.
usage: WatchAndRun.exe [path] [command]
    [path]      path (recursively) being watched
    [command]   command to execute when change is detected in path
";

        private static DateTime? _lastChange;

        private static void Main(string[] args)
        {
            if (args.Count() != 2)
            {
                PrintUsage();
            }
            else
            {
                string path = args[0];
                string commandToExecute = args[1];
                Console.WriteLine("Watching path: " + path);
                Console.WriteLine("On Change executing: " + commandToExecute);
                InitializeWatchTimer(path, commandToExecute);
            }
            Console.ReadKey(false);
        }

        private static void PrintUsage()
        {
            Console.WriteLine(Usage);
        }

        private static void InitializeWatchTimer(string path, string command)
        {
            var watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true };

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            watcher.EnableRaisingEvents = true;

            var timer = new Timer(TimerInterval);
            timer.Elapsed += new ElapsedEventHandler(CreateOneTimedEvent(command, timer));
            timer.Enabled = true;
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            _lastChange = DateTime.Now;
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            _lastChange = DateTime.Now;
        }

        private static Action<object, ElapsedEventArgs> CreateOneTimedEvent(string command, Timer timer)
        {
            Action<object, ElapsedEventArgs> timedEvent = (o, a) =>
            {
                if (_lastChange.HasValue)
                {
                    TimeSpan timeSinceLastChange = DateTime.Now - _lastChange.Value;
                    if (timeSinceLastChange.TotalMilliseconds > CommandExecutionDelayAfterChanges)
                    {
                        timer.Stop();
                        ExecuteCommand(command);
                        _lastChange = null;
                        timer.Start();
                    }
                }
            };
            return timedEvent;
        }

        private static void ExecuteCommand(string command)
        {
            var p = new Process
            {
                StartInfo = { UseShellExecute = false, RedirectStandardOutput = true, FileName = command }
            };
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            Console.Write(output);
        }
    }
}

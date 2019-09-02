using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Discord;

namespace Raven.Services
{
    public static class Logger
    {
        private static string LogDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Logs");
        private static string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");
        private static Queue<Action> queue = new Queue<Action>();
        private static AutoResetEvent hasNewItems = new AutoResetEvent(false);
        private static volatile bool waiting = false;
        private static Thread loggingThread = new Thread(new ThreadStart(ProcessQueue)) { IsBackground = true };

        internal static void StartLogger() => loggingThread.Start();
        private static void ProcessQueue()
        {
            while (true)
            {
                waiting = true;
                hasNewItems.WaitOne(10000, true);
                waiting = false;

                Queue<Action> queueCopy;
                lock (queue)
                {
                    queueCopy = new Queue<Action>(queue);
                    queue.Clear();
                }

                foreach (var log in queueCopy)
                {
                    log();
                }
            }
        }


        public static async Task OnLogAsync(LogMessage msg)
        {
            if (!Directory.Exists(LogDirectory))     // Create the log directory if it doesn't exist
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile))               // Create today's log file if it doesn't exist
                File.Create(LogFile).Dispose();

            lock (queue)
            {
                queue.Enqueue(async () =>
                {
                    string logText = $"{DateTime.UtcNow:hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
                    Console.ResetColor();

                    switch (msg.Severity)
                    {
                        case LogSeverity.Warning:
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            break;
                        case LogSeverity.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case LogSeverity.Debug:
                        case LogSeverity.Info:
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            break;
                        case LogSeverity.Critical:
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            break;
                        case LogSeverity.Verbose:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                    }

                    Console.Out.WriteLine(logText); // Write the log text to the console
                    File.AppendAllText(LogFile, logText + "\n"); // Write the log text to a file
                });
            }
            hasNewItems.Set();
        }

        /// <summary>Write to log file and console.</summary>
        /// <param name="message">What will be printed to the console and log.</param>
        /// <param name="source">Where did the error come from? Format: {Source}: {Message}</param>
        /// <param name="severity">Denotes the log type and colour. Default is verbose, which is white.</param>
        /// <param name="error">If we have an exception message, pass it in here.</param>
        public static void Log(string message, string source, LogSeverity severity = LogSeverity.Verbose, string error = null)
        {
            LogMessage msg = new LogMessage(severity, source, message, error != null ? new Exception(error) : null);
            Task.Run((() => OnLogAsync(msg)));
        }

        public static async Task AbortAfterLog(string message, string source, LogSeverity severity = LogSeverity.Error, string error = null)
        {
            LogMessage msg = new LogMessage(severity, source, message, error != null ? new Exception(error) : null);
            await Task.Run(() => OnLogAsync(msg));
            msg = new LogMessage(LogSeverity.Error, source, "The program cannot continue and must be closed. Please see the log file for details.", null);
            await Task.Run((() => OnLogAsync(msg)));
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DbMon.NET;
using NLog;

namespace DbgViewNLogRedirector
{
    class OdsLogger
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<LogLevel,Regex> logLevelPatterns = new Dictionary<LogLevel, Regex>();
        private readonly Dictionary<int, string> processNameByPid = new Dictionary<int, string>();
        private readonly LogLevel defaultLogLevel = LogLevel.Info;
        private readonly string rootLoggerName = string.Empty;

        public OdsLogger()
        {
            // loads the regex pattern for various log levels
            string pattern = ConfigurationManager.AppSettings["TraceLogPattern"];
            if (!string.IsNullOrEmpty(pattern))
                logLevelPatterns.Add(LogLevel.Trace, new Regex(pattern));

            pattern = ConfigurationManager.AppSettings["DebugLogPattern"];
            if (!string.IsNullOrEmpty(pattern))
                logLevelPatterns.Add(LogLevel.Debug, new Regex(pattern));

            pattern = ConfigurationManager.AppSettings["InfoLogPattern"];
            if (!string.IsNullOrEmpty(pattern))
                logLevelPatterns.Add(LogLevel.Info, new Regex(pattern));

            pattern = ConfigurationManager.AppSettings["WarningLogPattern"];
            if (!string.IsNullOrEmpty(pattern))
                logLevelPatterns.Add(LogLevel.Warn, new Regex(pattern));

            pattern = ConfigurationManager.AppSettings["ErrorLogPattern"];
            if (!string.IsNullOrEmpty(pattern))
                logLevelPatterns.Add(LogLevel.Error, new Regex(pattern));

            pattern = ConfigurationManager.AppSettings["FatalLogPattern"];
            if (!string.IsNullOrEmpty(pattern))
                logLevelPatterns.Add(LogLevel.Fatal, new Regex(pattern));

            // default log level to use if the log doesn't match with any of the above pattern
            string defaultLogLevelString = ConfigurationManager.AppSettings["DefaultLogLevel"];
            if (!string.IsNullOrEmpty(defaultLogLevelString))
            {
                switch (defaultLogLevelString.ToLower())
                {
                    case "trace":
                        defaultLogLevel = LogLevel.Trace;
                        break;

                    case "debug":
                        defaultLogLevel = LogLevel.Debug;
                        break;

                    case "info":
                        defaultLogLevel = LogLevel.Info;
                        break;

                    case "warning":
                        defaultLogLevel = LogLevel.Warn;
                        break;

                    case "error":
                        defaultLogLevel = LogLevel.Error;
                        break;

                    case "fatal":
                        defaultLogLevel = LogLevel.Fatal;
                        break;
                }
            }

            // determine the logger name under which we group all the loggers(i.e. process names)
            rootLoggerName = ConfigurationManager.AppSettings["RootLoggerName"] ?? string.Empty;
            if (rootLoggerName != string.Empty)
                rootLoggerName += ".";
        }

        /// <summary>
        /// Callback method which will be called by DebugMonitor
        /// </summary>
        /// <param name="pid">The PID of the process which is logging OutputDebugString message</param>
        /// <param name="text">The OutputDebugString message</param>
        void Log(int pid, string text)
        {
            Console.WriteLine(text);

            foreach (var logLevel in logLevelPatterns.Keys)
            {
                if (logger.IsEnabled(logLevel) && logLevelPatterns[logLevel].IsMatch(text))
                {
                    string message = logLevelPatterns[logLevel].Replace(text, string.Empty);
                    logger.Log(new LogEventInfo(logLevel, _GetLoggerName(pid), message));
                    return;
                }
            }

            logger.Log(new LogEventInfo(defaultLogLevel, _GetLoggerName(pid), text));
        }

        /// <summary>
        /// Returns a logger name by for given process PID.
        /// </summary>
        /// <param name="pid">PID of a process</param>
        /// <returns>Logger name</returns>
        private string _GetLoggerName(int pid)
        {
            return rootLoggerName + _GetProcessName(pid);
        }

        /// <summary>
        /// Returns the process name for given process PID
        /// </summary>
        /// <param name="pid">PID of a process</param>
        /// <returns>Process name</returns>
        private string _GetProcessName(int pid)
        {
            if (!processNameByPid.ContainsKey(pid))
                processNameByPid.Add(pid, Process.GetProcessById(pid).ProcessName);

            return processNameByPid[pid];
        }

        static void Main(string[] args)
        {
            bool keepRunning = true;

            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs eventArgs)
            {
                Console.Write("Terminating...");
                keepRunning = false;
                eventArgs.Cancel = true;
            };

            OdsLogger odsLogger = new OdsLogger();

            List<DebugMonitor> debugMonitors = new List<DebugMonitor>();

            if (string.Equals(ConfigurationManager.AppSettings["CaptureGlobal"], "true", StringComparison.OrdinalIgnoreCase))
                debugMonitors.Add(new DebugMonitor(true));
            
            if (string.Equals(ConfigurationManager.AppSettings["CaptureLocal"], "true", StringComparison.OrdinalIgnoreCase))
                debugMonitors.Add(new DebugMonitor(false));

            foreach (var debugMonitor in debugMonitors)
            {
                debugMonitor.OnOutputDebugString += odsLogger.Log;
                debugMonitor.Start();                
            }

            Console.WriteLine("Redirecting OutputDebugString to NLog target(s)...");
            while (keepRunning)
                System.Threading.Thread.Sleep(500);

            foreach (var debugMonitor in debugMonitors)
                debugMonitor.Stop();
        }
    }
}

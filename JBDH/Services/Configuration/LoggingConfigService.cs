using System;
using System.Collections.Generic;
using NLog;

namespace JBDH.Services.Configuration
{ 
    public static class LoggingConfigService
        {
            private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
            public static void ConfigLogging()
            {
                var config = new NLog.Config.LoggingConfiguration();

                // Console target (shows in terminal and Azure Log Stream)
                var logConsole = new NLog.Targets.ConsoleTarget
                {
                    Name = "logConsole",
                    Layout = "${longdate} ${level:uppercase=true} ${message}"
                };

                // File target (shows in Azure LogFiles blade)
                var logFile = new NLog.Targets.FileTarget
                {
                    Name = "logFile",
                    FileName = "D:/home/LogFiles/app.log", // Azure App Service log path
                    Layout = "${longdate} ${level:uppercase=true} ${message}"
                };

                config.AddTarget("logConsole", logConsole);
                config.AddTarget("logFile", logFile);

                // Log everything to both targets
                config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logConsole);
                config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logFile);

                NLog.LogManager.Configuration = config;
            }
    
            public static void ReturnInfo(string info)
            {
                Logger.Info(info);
            }
    
            public static void ReturnWarning(string info)
            {
                Logger.Warn(info);
            }
    
            public static void ReturnError(string info)
            {
                Logger.Error(info);
            }
    
            public static void ReturnCritical(string info)
            {
                Logger.Fatal(info);
            }
    
            public static void ReturnDebug(string info)
            {
                Logger.Debug(info);
            }
    
            public static void ReturnException(string info)
            {
                Logger.Error(info);
            }
    
            public static void ReturnFatal(string info)
            {
                Logger.Fatal(info);
            }
    
            public static int InitCounter()
            {
                return 0;
            }
    
            public static int CounterPlusPlus(int counter)
            {
                return counter + 1;
            }
    
            public static List<List<int>> CounterGenerator(int number)
            {
                var counters = new List<List<int>>();
                for (int i = 0; i < number; i++)
                {
                    counters.Add(new List<int> { 0 });
                }
                return counters;
            }
    
            public static void PrintCounters(List<List<int>> counters)
            {
                if (counters == null)
                {
                    return;
                }
                
                foreach (var counter in counters)
                {
                    ReturnInfo(string.Join(", ", counter));
                }
            }
            public static void main(string[] args)
            {
                ConfigLogging();
                ReturnInfo("Logging configured successfully.");
            } 
        }
}
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
    
                var logConsole = new NLog.Targets.ConsoleTarget
                {
                    Name = "logConsole",
                    Layout = "${longdate} ${level:uppercase=true} ${message}"
                };

                // Add the target to the configuration
                config.AddTarget("logConsole", logConsole);

                // Define a logging rule for all levels
                var rule = new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, logConsole);
                config.LoggingRules.Add(rule);

                // Apply the configuration
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
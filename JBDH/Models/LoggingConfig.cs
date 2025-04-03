using System;
using System.Collections.Generic;
 
namespace JBDH.Models;
{
    public static class LoggingConfig
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
 
        public static void ConfigLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();
 
            var logConsole = new NLog.Targets.ConsoleTarget("logConsole")
            {
                Layout = "${longdate} ${level:uppercase=true} ${message}"
            };
 
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logConsole);
 
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
 
        public static void ReturnException(string info, Exception ex)
        {
            Logger.Error(ex, info);
        }
 
        public static void ReturnFatal(string info, Exception ex)
        {
            Logger.Fatal(ex, info);
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
                Console.WriteLine(string.Join(", ", counter));
            }
        }
    }
}
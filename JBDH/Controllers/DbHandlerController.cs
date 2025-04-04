using System;
using Microsoft.VisualBasic; // Assuming this is the namespace for your DbConfig class
using JBDH.Models; // Assuming this is the namespace for your models

namespace JBDH.Controllers; // Assuming this is the namespace for your controller
class DbHandler
{
    private static readonly string ConnectionString = DbConfig.DB_CONFIG;
    private const string JsonFile = "jobs.json";
 
    public static void Main(string[] args)
    {
        LoggingConfig.ConfigLogging();
        LoggingConfig.ReturnInfo("Starting up Job database handler...\nSetting up connection...\nTesting connection...");
        Settings.TestConnection();
        LoggingConfig.ReturnInfo("Connection established...");
        EtlPipeline.RunEtlPipeline(JsonFile);
        LoggingConfig.ReturnInfo("Starting sorting queries...");
        // InternalSorting.TestInternalSorting(); // Placeholder for sorting queries
        LoggingConfig.ReturnInfo("Closing application...");
        // Add API call functionality if needed
        // InternalSorting.InternalSorting(); // Placeholder for API call
    }
}
 
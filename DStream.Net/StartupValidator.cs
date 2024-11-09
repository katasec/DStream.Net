using System;
using System.Collections.Generic;
using DStream.Net.Config;
using Microsoft.Extensions.Logging;

namespace DStream.Net;

public class StartupValidator
{
    private readonly AppConfig _appConfig;
    private readonly ILogger<StartupValidator> _logger;

    public StartupValidator(AppConfig appConfig, ILogger<StartupValidator> logger)
    {
        _appConfig = appConfig;
        _logger = logger;
    }

    public bool ValidateConfiguration()
    {
        var missingFields = new List<string>();

        // Check for required fields in AppConfig
        if (string.IsNullOrEmpty(_appConfig.DbType))
            missingFields.Add("DbType");

        if (string.IsNullOrEmpty(_appConfig.DbConnectionString))
            missingFields.Add("DbConnectionString");

        if (string.IsNullOrEmpty(_appConfig.AzureEventHubConnectionString))
            missingFields.Add("AzureEventHubConnectionString");

        if (string.IsNullOrEmpty(_appConfig.AzureEventHubName))
            missingFields.Add("AzureEventHubName");

        if (_appConfig.Tables == null || _appConfig.Tables.Count == 0)
            missingFields.Add("Tables");

        // Log missing fields if any
        if (missingFields.Count > 0)
        {
            _logger.LogError("Missing required configuration items:");
            foreach (var field in missingFields)
            {
                _logger.LogError($"- {field}");
            }
            _logger.LogError("Please update the configuration file and restart the application.");

            return false;  // Validation failed
        }

        _logger.LogInformation("Configuration validated successfully.");
        return true;  // Validation succeeded
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Cinegy.Telemetry
{
    public static class LogSetup
    {
        public static void ConfigureLogger(string appId, string orgId, string descriptorTags, string telemetryUrl, bool enableTelemetry, bool enableConsole = false, string productName = null)
        {
            ConfigureLogger(appId, orgId, descriptorTags, telemetryUrl, enableTelemetry, LogLevel.Info, new LoggingConfiguration(), enableConsole,  productName);
        }

        public static void ConfigureLogger(string appId, string orgId, string descriptorTags, string telemetryUrl,
            bool enableTelemetry, LogLevel telemetryLogLevel, LoggingConfiguration config, bool enableConsole, string productName)
        {
            if (enableConsole)
            {
                var consoleTarget = new ColoredConsoleTarget();
                config.AddTarget("console", consoleTarget);
                consoleTarget.Layout = @"${date:format=HH\:mm\:ss} ${message} (${logger})";
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, consoleTarget));
            }

            if (enableTelemetry)
            {
                var bufferedEsTarget = ConfigureEsLog(appId, orgId, descriptorTags, telemetryUrl, productName);
                config.AddTarget("elasticsearch", bufferedEsTarget);
                config.LoggingRules.Add(new LoggingRule("*", telemetryLogLevel, bufferedEsTarget));
            }
            
            LogManager.Configuration = config;
        }

        private static BufferingTargetWrapper ConfigureEsLog(string appId, string orgId, string descriptorTags, string telemetryUrl, string productName)
        {
            var indexNameParts = new List<string> { appId, "${date:universalTime=true:format=yyyy.MM.dd}" };

            if (!string.IsNullOrEmpty(orgId))
            {
                indexNameParts = new List<string> { $"{appId}-{orgId.ToLowerInvariant()}", "${date:universalTime=true:format=yyyy.MM.dd}" };
            }

            var renderedIndex = Layout.FromString(string.Join("-", indexNameParts));

            //check to see if an environment variable is set to override telemetry targets
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OVERRIDE_CINEGY_TELEMETRY_TARGET")))
            {
                telemetryUrl = Environment.GetEnvironmentVariable("OVERRIDE_CINEGY_TELEMETRY_TARGET");
            }

            var elasticSearchTarget = new ElasticSearchTarget
            {
                Layout = new TelemetryLayout(descriptorTags?.Split(',').Enumerate().ToArray()),
                Uri = telemetryUrl,
                Index = renderedIndex
            };

            if (!string.IsNullOrEmpty(productName))
                ((TelemetryLayout)elasticSearchTarget.Layout).ProductName = productName;

            var bufferingTarget = new BufferingTargetWrapper(elasticSearchTarget)
            {
                FlushTimeout = 5000
            };

            return bufferingTarget;
        }
    }
}

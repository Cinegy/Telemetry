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
        public static void ConfigureLogger(string appId, string orgId, string descriptorTags, string telemetryUrl, bool enableTelemetry, bool enableConsole, string productName, string productVersion)
        {
            ConfigureLogger(appId, orgId, descriptorTags, telemetryUrl, enableTelemetry, LogLevel.Info, new LoggingConfiguration(), enableConsole,  productName, productVersion);
        }

        public static void ConfigureLogger(string appId, string orgId, string descriptorTags, string telemetryUrl,
            bool enableTelemetry, LogLevel telemetryLogLevel, LoggingConfiguration config, bool enableConsole, string productName, string productVersion)
        {
            if (enableConsole)
            {
                var consoleTarget = new ColoredConsoleTarget();
                config.AddTarget("console", consoleTarget);
                consoleTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));
            }

            if (enableTelemetry)
            {
                var bufferedEsTarget = ConfigureEsLog(appId, orgId, descriptorTags, telemetryUrl, productName, productVersion);
                config.AddTarget("elasticsearch", bufferedEsTarget);
                config.LoggingRules.Add(new TelemetryLoggingRule("*", telemetryLogLevel, bufferedEsTarget));
            }

            var signalRTarget = new SignalRTarget();
            config.AddTarget("signalr",signalRTarget);
            signalRTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";
            config.LoggingRules.Add(new LoggingRule("*",LogLevel.Debug,signalRTarget));


            LogManager.Configuration = config;
        }

        private static BufferingTargetWrapper ConfigureEsLog(string appId, string orgId, string descriptorTags, string telemetryUrl, string productName, string productVersion)
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
                Layout = new TelemetryLayout(descriptorTags?.Split(',').Enumerate().ToArray())
                {
                    ProductName = productName,
                    ProductVersion = productVersion
                }
                ,
                Uri = telemetryUrl,
                Index = renderedIndex
            };

            var bufferingTarget = new BufferingTargetWrapper(elasticSearchTarget)
            {
                FlushTimeout = 5000
            };

            return bufferingTarget;
        }
    }
}

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
        public static void ConfigureLogger(string appId, string orgId, string descriptorTags, string telemetryUrl, bool enableTelemetry)
        {
            var config = new LoggingConfiguration();

            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            consoleTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, consoleTarget));

            if (enableTelemetry)
            {
                var bufferedEsTarget = ConfigureEsLog(appId, orgId, descriptorTags, telemetryUrl);
                config.AddTarget("elasticsearch", bufferedEsTarget);
                config.LoggingRules.Add(new TelemetryLoggingRule("*", LogLevel.Info, bufferedEsTarget));
            }

            LogManager.Configuration = config;

        }

        private static BufferingTargetWrapper ConfigureEsLog(string appId, string orgId, string descriptorTags, string telemetryUrl)
        {
            var indexNameParts = new List<string> { appId, "${date:format=yyyy.MM.dd}" };

            if (!string.IsNullOrEmpty(orgId))
            {
                indexNameParts = new List<string> { $"{appId}-{orgId}", "${date:format=yyyy.MM.dd}" };
            }

            var renderedIndex = Layout.FromString(string.Join("-", indexNameParts));

            var elasticSearchTarget = new ElasticSearchTarget
            {
                Layout = new TelemetryLayout(descriptorTags?.Split(',').Enumerate().ToArray()),
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

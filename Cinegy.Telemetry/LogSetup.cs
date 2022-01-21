using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers.Wrappers;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Cinegy.Telemetry
{
    public static class LogSetup
    {
        private static readonly string WorkingDirectory;
        
        static LogSetup()
        {
            WorkingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? string.Empty;
        }
        
        public static ILogger InitializeLogger(TelemetrySettings settings = null, string programDataDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(programDataDirectory) && File.Exists(Path.Combine(programDataDirectory, "nlog.appConfig")))
            {
                LogManager.LoadConfiguration(Path.Combine(programDataDirectory, "nlog.appConfig"));
            }
            else if (File.Exists(Path.Combine(WorkingDirectory, "nlog.appConfig")))
            {
                LogManager.LoadConfiguration(Path.Combine(WorkingDirectory, "nlog.appConfig"));
            }
            
            if (LogManager.Configuration == null)
            {
                //if no explicit nlog.appConfig file has been used, add a standard console logger
                LogManager.Configuration = new LoggingConfiguration();
                ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("pad",
                    typeof(PaddingLayoutRendererWrapper));

                var layout = new SimpleLayout
                {
                    Text = "${longdate} ${pad:padding=-10:inner=(${level:upperCase=true})} " +
                           "${pad:padding=-20:fixedLength=true:inner=${logger:shortName=true}} " +
                           "${message} ${exception:format=tostring}"
                };

                var consoleTarget = new ColoredConsoleTarget
                {
                    UseDefaultRowHighlightingRules = true,
                    DetectConsoleAvailable = true,
                    Layout = layout
                };
                
                LogManager.Configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
            }
            
            if (settings?.Enabled == true && Environment.GetEnvironmentVariable("OVERRIDE_CINEGY_TELEMETRY_TO_DISABLED") == null)
            {
                var bufferedEsTarget = GetElasticsearchBufferingTargetWrapper(settings.ApplicationId, settings.OrganizationId,
                    settings.RecordTags, settings.TelemetryUrl, settings.ProductName);
                
                LogManager.Configuration.AddRule(LogLevel.FromString(settings.LogLevel), LogLevel.Fatal, bufferedEsTarget);
            }

            LogManager.ReconfigExistingLoggers();
            return LogManager.GetCurrentClassLogger();
        }
        
        public static BufferingTargetWrapper GetElasticsearchBufferingTargetWrapper(string appId, string orgId, string descriptorTags, string telemetryUrl, string productName)
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

using System;
using System.Reflection;
using System.Threading;
using NLog;
using NUnit.Framework;

namespace Cinegy.Telemetry.Tests
{
    [TestFixture]
    public class LogSetupTests
    {
        private static ILogger _logger;

        [TestCase]
        public void LogSetupTest()
        {
            try
            {
                var buildVersion = Assembly.GetEntryAssembly()?.GetName().Version!.ToString();

                var settings = new TelemetrySettings
                {
                    ApplicationId = "telemetryunittest",
                    OrganizationId = "Cinegy",
                    ProductName = "UnitTesting",
                    TelemetryUrl = "https://telemetry.cinegy.com",
                    Enabled = true
                };

                settings.TelemetryUrl = "https://telemetry-cinegycloud.cinegy.com";

                _logger = LogSetup.InitializeLogger(settings);
               
                _logger.Info($"Cinegy Telemetry Unit Test Running at {DateTime.UtcNow:O}");
                _logger.Info($"Build version: {buildVersion}");
            }
            catch (Exception ex){
                Assert.Fail($"Failed to setup and log test message: {ex.Message}");
            }
            finally{
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
         
        }
    }
}

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
        private static Logger _logger;

        [TestCase]
        public void LogSetupTest()
        {
            try
            {
                _logger = LogManager.GetCurrentClassLogger();

                LogSetup.ConfigureLogger("telemetryunittest","Cinegy","UnitTesting",
                    "http://telemetry.cinegy.com",enableTelemetry: true);
               
                _logger.Info($"Cinegy Telemetry Unit Test Running at {DateTime.UtcNow:O}");
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

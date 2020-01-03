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

                var buildVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();

                LogSetup.ConfigureLogger("telemetryunittest","Cinegy","UnitTesting",
                    "http://telemetry.cinegy.com",enableTelemetry: true,enableConsole:false,"TelemetryLibTest", buildVersion);
               
                _logger.Info($"Cinegy Telemetry Unit Test Running at {DateTime.UtcNow:O}");

                //allow a long sleep, to permit the logger to create any daily indices and to flush logs
                Thread.Sleep(60000);

            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to setup and log test message: {ex.Message}");
            }
         
        }
    }
}

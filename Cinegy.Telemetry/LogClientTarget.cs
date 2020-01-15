using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog.Common;
using NLog.Targets;

namespace Cinegy.Telemetry
{
    [Target("LogClient")]
    public class LogClientTarget : Target
    {
        #region Constructors

        public LogClientTarget()
        {
            Name = "Generic ILogClient";
            
            try
            {
                var idPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), "Cinegy\\Telemetry\\envid.txt");

                if (File.Exists(idPath))
                {
                    EnvironmentId = File.ReadAllText(idPath);
                }
                else
                {
                    EnvironmentId = Guid.NewGuid().ToString();
                    if (!Directory.Exists(Path.GetDirectoryName(idPath))) Directory.CreateDirectory(Path.GetDirectoryName(idPath) ?? throw new InvalidOperationException());
                    File.WriteAllText(idPath, EnvironmentId);
                }

            }
            catch (Exception ex)
            {
                InternalLogger.Error($"IO error working with Environment ID file: {ex.Message}");
            }
        }

        #endregion

        #region Properties
        public IEnumerable<string> Tags { get; set; }

        public string MachineName { get; set; } = Environment.MachineName;

        public string ProductName { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name;

        public string ProductVersion => Assembly.GetEntryAssembly()?.GetName().Version.ToString();

        public string EnvironmentId { get; }

        #endregion

        #region Properties

        /// <summary>
        /// Object implementing the LogClient interface, which can be used to bind into other mechanisms (e.g. SignalR)
        /// </summary>
        public ILogClient LogClient { get; set; }

        #endregion

        #region Override members
        protected override void Write(AsyncLogEventInfo logEvent)
        {
            Write(new List<AsyncLogEventInfo>(1) { logEvent });
        }

        [Obsolete]
        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            SendBatch(logEvents);
        }

        protected override void Write(IList<AsyncLogEventInfo> logEvents)
        {
            SendBatch(logEvents);
        }

        #endregion

        #region Members
        
        private void SendBatch(IEnumerable<AsyncLogEventInfo> events)
        {
            if (LogClient == null) return;
            try
            {
                var logEvents = events.Select(e => e.LogEvent);

                foreach (var info in logEvents)
                {
                    var dynamicObject = new
                    {
                        Level = info.Level.ToString(),
                        Time = info.TimeStamp.ToUniversalTime().ToString("o"),
                        Tags,
                        Host = MachineName,
                        EnvironmentId,
                        Logger = info.LoggerName,
                        info.Message,
                        Product = new
                        {
                           Name = ProductName,
                           Version = ProductVersion
                        }
                    };

                    LogClient?.SendMessage(dynamicObject);
                }
            }
            catch (Exception ex)
            {
                InternalLogger.Error("Error while sending log messages to ILogClient: message=\"{0}\"", ex.Message);
            }
        }

        #endregion
    }
}

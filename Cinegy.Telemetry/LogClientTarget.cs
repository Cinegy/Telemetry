using System;
using System.Collections.Generic;
using System.Linq;
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
        }

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
            if (LogClient == null) return;
            SendBatch(logEvents);
        }

        #endregion

        #region Members

        private void SendBatch(IEnumerable<AsyncLogEventInfo> events)
        {
            try
            {
                var logEvents = events.Select(e => e.LogEvent);

                foreach (var logEventInfo in logEvents)
                {
                    LogClient?.SendMessage(logEventInfo.Message);
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

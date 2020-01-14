using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elasticsearch.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Cinegy.Telemetry
{
    [Target("SignalR")]
    public class SignalRTarget : TargetWithLayout
    {
       
        #region Constructors

        public SignalRTarget()
        {
            Name = "SignalR";
            DocumentType = "logevent";
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the document type for the elasticsearch index.
        /// </summary>
        [RequiredParameter]
        public Layout DocumentType { get; set; }

        
        #endregion

        #region Override members

        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            
        }

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

        private List<object> FormPayload(IEnumerable<LogEventInfo> logEvents)
        {
            var payload = new List<object>();

            foreach (var logEvent in logEvents)
            {
                var rendered = Layout.Render(logEvent);
                //var type = DocumentType.Render(logEvent);
                payload.Add(rendered);
                //var parsedObject = Parse((JObject)JsonConvert.DeserializeObject(rendered));
                //payload.Add(parsedObject);
            }

            return payload;
        }

        Dictionary<string, object> Parse(JObject obj)
        {
            var dictionary = obj.ToObject<Dictionary<string, object>>();

            foreach (var key in dictionary.Keys.ToArray())
            {
                var value = dictionary[key];
                var jObject = value as JObject;
                if (jObject != null) dictionary[key] = Parse(jObject);
            }

            return dictionary;
        }

        private void SendBatch(IEnumerable<AsyncLogEventInfo> events)
        {
            try
            {
                var logEvents = events.Select(e => e.LogEvent);

                var payload = FormPayload(logEvents);
                
                Debug.WriteLine($"HACKING: {payload}");
                
            }
            catch (Exception ex)
            {
                InternalLogger.Error("Error while sending log messages to signalrhub: message=\"{0}\"", ex.Message);
            }
        }

        #endregion
    }
}

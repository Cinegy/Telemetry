using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NLog;
using NLog.Layouts;

namespace Cinegy.Telemetry
{

    public class TelemetryLayout : Layout
    {
        private readonly string[] _tags;

        #region Constructors

        public TelemetryLayout(params string[] tags)
        {
            _tags = tags.Enumerate().ToArray();
            JsonSerializerSettings = new JsonSerializerSettings();
        }

        #endregion

        #region Properties

        private JsonSerializerSettings JsonSerializerSettings { get; }

        //TODO: Try and set these back to automatic - original way of working did not work with NET Standard 1.3
        public string MachineName { get; set; }

        public string ProductName { get; set; }

        public string ProductVersion { get; set; }

        #endregion

        #region Override members

        /// <summary>
        ///     Renders the layout for the specified logging event by invoking layout renderers.
        /// </summary>
        /// <param name="info">The logging event.</param>
        /// <returns>
        ///     The rendered layout.
        /// </returns>
        protected override string GetFormattedMessage(LogEventInfo info)
        {
            object complexPayloadObject = null;
            var headerTable = new Dictionary<string, object>
            {
                { "Level", info.Level.ToString() },
                { "Time", info.TimeStamp.ToUniversalTime().ToString("o") },
                { "Tags", string.Join(",", _tags) },
                { "Host", MachineName },
                { "Logger", info.LoggerName },
                {
                    "Product", new
                    {
                        Name = ProductName,
                        Version = ProductVersion
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(info.Message)) headerTable.Add("Message", info.Message);
            
            var telemetryInfo = info as TelemetryLogEventInfo;
            if (telemetryInfo != null)
            {
                headerTable.Add("Key", telemetryInfo.Key ?? "Generic");

                if (telemetryInfo.TelemetryObject != null)
                {
                    var type = telemetryInfo.TelemetryObject.GetType();
                    
                    if (type.GetTypeInfo().IsClass && type != typeof(string)) complexPayloadObject = telemetryInfo.TelemetryObject;
                    else headerTable.Add("Payload", telemetryInfo.TelemetryObject);
                }
            }
            else
            {
                headerTable.Add("Key", "Message");
            }

            var header = JsonConvert.SerializeObject(headerTable, JsonSerializerSettings);

            if (complexPayloadObject != null)
            {
                var complexPayload = JsonConvert.SerializeObject(complexPayloadObject, JsonSerializerSettings);
                return string.Join(",",
                                   header.Substring(0, header.Length - 1),
                                   complexPayload.Substring(1));
            }

            return header;
        }

        #endregion
    }

    public static class CollectionExtensions
    {
        public static IEnumerable<T> Enumerate<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }
    }

}

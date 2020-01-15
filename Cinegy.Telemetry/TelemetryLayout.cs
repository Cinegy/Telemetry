using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using NLog.Common;
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
                    if(!Directory.Exists(Path.GetDirectoryName(idPath))) Directory.CreateDirectory(Path.GetDirectoryName(idPath) ?? throw new InvalidOperationException());
                    File.WriteAllText(idPath, EnvironmentId);
                }
                
            }
            catch (Exception ex)
            {
                InternalLogger.Error($"IO error working with Environment ID file: {ex.Message}");
            }

            JsonSerializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

        #endregion

        #region Properties

        private JsonSerializerSettings JsonSerializerSettings { get; }

        public string MachineName { get; set; } = Environment.MachineName;

        public string ProductName { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name;

        public string ProductVersion => Assembly.GetEntryAssembly()?.GetName().Version.ToString();

        public string EnvironmentId { get; }

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
            var eventObjectDictionary = new Dictionary<string, object>
            {
                { "Level", info.Level.ToString() },
                { "Time", info.TimeStamp.ToUniversalTime().ToString("o") },
                { "Tags", string.Join(",", _tags) },
                { "Host", MachineName },
                { "EnvironmentId", EnvironmentId },
                { "Logger", info.LoggerName },
                {
                    "Product", new
                    {
                        Name = ProductName,
                        Version = ProductVersion
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(info.Message)) eventObjectDictionary.Add("Message", info.Message);

            if (info is TelemetryLogEventInfo telemetryInfo)
            {
                eventObjectDictionary.Add("Key", telemetryInfo.Key ?? "Generic");

                if (telemetryInfo.TelemetryObject != null)
                {
                    var type = telemetryInfo.TelemetryObject.GetType();
                    
                    if (type.GetTypeInfo().IsClass && type != typeof(string)) complexPayloadObject = telemetryInfo.TelemetryObject;
                    else eventObjectDictionary.Add("Payload", telemetryInfo.TelemetryObject);
                }
            }
            else
            {
                eventObjectDictionary.Add("Key", "Message");
            }

            var eventJson = JsonConvert.SerializeObject(eventObjectDictionary, JsonSerializerSettings);

            if (complexPayloadObject == null) return eventJson;

            var complexPayload = JsonConvert.SerializeObject(complexPayloadObject, JsonSerializerSettings);
            return string.Join(",",
                eventJson.Substring(0, eventJson.Length - 1),
                complexPayload.Substring(1));

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

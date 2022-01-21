namespace Cinegy.Telemetry
{
    public class TelemetrySettings
    {
        #region Properties

        public static string ConfigurationName => "Telemetry";
         
        public bool Enabled { get; set; } = false;

        public string ProductName { get; set; } = "Telemetry";

        public string ApplicationId { get; set; } = "telemetry";

        public string OrganizationId { get; set; } = "cinegy";

        public string LogLevel { get; set; } = "Information";

        public string RecordTags { get; set; }

        public string TelemetryUrl { get; set; } = "https://telemetry.cinegy.com";
        

        #endregion
    }
}
namespace DataImport.Configuration
{
    /// <summary>
    /// Binds to the "ImportSettings" section of appsettings.json.
    /// </summary>
    public class ImportSettings
    {
        /// <summary>
        /// Folder where each day's downloaded XML is cached, one subfolder
        /// per date (yyyy-MM-dd). Relative paths are resolved against the
        /// app's base directory.
        /// </summary>
        public string RootFolder { get; set; } = "Imports";

        /// <summary>
        /// URL to download the OFAC SDN.XML file from. Configurable so a URL
        /// change (OFAC has moved/renamed these before) doesn't require a
        /// redeploy — just an appsettings.json update.
        /// </summary>
        public string SdnXmlUrl { get; set; } = "https://www.treasury.gov/ofac/downloads/sdn.xml";

        /// <summary>
        /// How many days' worth of cached sdn.xml folders (under RootFolder)
        /// to keep. Older date-folders are deleted at the start of each run.
        /// Set to 0 or a negative value to disable cleanup entirely.
        /// </summary>
        public int CacheRetentionDays { get; set; } = 14;
    }
}
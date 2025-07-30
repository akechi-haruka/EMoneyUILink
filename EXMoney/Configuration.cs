using Microsoft.Extensions.Configuration;

namespace Haruka.Arcade.EXMoney {
    /// <summary>
    /// Main class handling library configuration (for now, mainly logging)
    /// </summary>
    public static class Configuration {
        internal static IConfigurationRoot Current { get; private set; }

        /// <summary>
        /// Initializes the configuration from the default files.
        /// </summary>
        /// <returns>An accessor to configuration data.</returns>
        public static IConfigurationRoot Initialize() {
            Current = new ConfigurationBuilder()
                .AddJsonFile("exmoney.json", false, true)
                .AddJsonFile("exmoney.debug.json", true)
                .Build();
            return Current;
        }

        private static string Get(string section, string value) {
            return Current.GetSection(section)?.GetSection(value)?.Value;
        }

        private static string Get(string section, string subsection, string value) {
            return Current.GetSection(section)?.GetSection(subsection)?.GetSection(value)?.Value;
        }

        private static int GetInt(string section, string value) {
            return (Current.GetSection(section)?.GetValue<int>(value)).Value;
        }

        private static bool GetBool(string section, string value) {
            return (Current.GetSection(section)?.GetValue<bool>(value)).Value;
        }
    }
}
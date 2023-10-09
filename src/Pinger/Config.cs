using TShockAPI.Configuration;

namespace Pinger
{
    /// <summary>
    /// The general config layout as well
    /// as the default configs.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Whether or not the plugin is enabled
        /// </summary>
        public bool Enabled = true;

        /// <summary>
        /// The way its going to check for if it should
        /// ping the players currently alive or not.
        /// </summary>
        public string PingRequirementType = "PlayersAlive";

        /// <summary>
        /// The value associated with `PingRequirementType`
        /// </summary>
        public int PingRequirementValue = 5;

        /// <summary>
        /// The time interval in seconds that the plugin
        /// shall ping the players.
        /// </summary>
        public int PingIntervalSec = 5;

    }

    /// <summary>
    /// the class to implement the layout and
    /// default configs.
    /// </summary>
    public class PingerConfigs : ConfigFile<Config> { }
}

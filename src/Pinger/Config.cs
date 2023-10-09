using TShockAPI.Configuration;

namespace Pinger
{
    /// <summary>
    /// The general config layout as well
    /// as its default configs.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Whether or not the plugin is enabled
        /// </summary>
        public bool Enabled = true;

        /// <summary>
        /// The way the plugin is going to check for if it should
        /// ping the players currently alive or not.
        /// </summary>
        public string PingRequirementType = "PlayersAlive";

        /// <summary>
        /// The value associated with `PingRequirementType`
        /// </summary>
        public int PingRequirementValue = 5;

        /// <summary>
        /// The time interval in seconds at which the plugin
        /// shall ping the players.
        /// </summary>
        public int PingIntervalSec = 5;

    }

    /// <summary>
    /// the class to implement the layout / default configs.
    /// </summary>
    public class PingerConfigs : ConfigFile<Config> { }
    /*                           ^ the config file class from `TShockAPI.Configuration` simplifies
     *                             the process of reading, writing and implementing our default configurations
     */

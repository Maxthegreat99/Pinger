using TShockAPI.Configuration;

namespace Pinger
{
    public class Config
    {
        public bool Enabled = true;

        public string PingRequirementType = "PlayersAlive";

        public int PingRequirementValue = 5;

        public int PingIntervalSec = 5;

    }

    public class PingerConfigs : ConfigFile<Config> { }
}

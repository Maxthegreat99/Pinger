using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Pinger
{

    [ApiVersion(2, 1)]
    public class Pinger : TerrariaPlugin
    {
        /***** Plugin Properties *****/
        public override string Author => "Maxthegreat99";

        public override string Description => "A TShock plugin conceived for HC minigames, that pings" +
                               " the server's players once a number of them are left alive.";

        public override string Name => "Pinger";

        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        /***** Plugin Variables *****/

        private static string configPath = Path.Combine(TShock.SavePath, "PingerPlugin.json");

        private static PingerConfigs Configs { get; set; } = new PingerConfigs();
        
        private const string messageTag = "[Pinger] ";


        private enum PingRequirementTypes
        {
            PLAYERS_ALIVE,
            PLAYERS_DEAD,
            PERCENTAGE_ALIVE
        }

        private static PingRequirementTypes pingRequirementType;

        private static int pingRequirementValue;

        private static System.Timers.Timer pingTimer { get; set; }

        public Pinger(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);

            Commands.ChatCommands.Add(new Command(
                permissions: "pinger.enable",
                cmd: EnableCommand,
                "pingerenable", "pingenable", "enableping"));

            Commands.ChatCommands.Add(new Command(
                permissions: "pinger.reload",
                cmd: ReloadConfigs,
                "pingerreload", "pingreload", "reloadping"));
        }

        /***** Plugin Hooks *****/

        private static void OnInitialize(EventArgs args)
        {
            LoadConfigs();
        }

        private static void LoadConfigs()
        {
            bool writeConfig = true;
            if (File.Exists(configPath))
                Configs.Read(configPath, out writeConfig);

            if (writeConfig)
                Configs.Write(configPath);

            if (!Configs.Settings.Enabled)
                return;

            switch (Configs.Settings.PingRequirementType)
            {
                case "PlayersAlive":
                    pingRequirementType = PingRequirementTypes.PLAYERS_ALIVE;
                    break;
                case "PlayersDead":
                    pingRequirementType = PingRequirementTypes.PLAYERS_DEAD;
                    break;
                case "PercentageAlive":
                    pingRequirementType = PingRequirementTypes.PERCENTAGE_ALIVE;

                    if (Configs.Settings.PingRequirementValue > 1 &&
                       Configs.Settings.PingRequirementValue <= 10)
                        break;

                    var originalForecolor = Console.ForegroundColor;

                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(messageTag + "ERROR: The plugin's config field 'PingRequirementValue'" +
                                      " is less than 1 or greater than 10, The plugin shall disable itself. " +
                                      "please read the documentation to discover how the plugin" +
                                      "checks for the percentage of players alive: https://github.com/Maxthegreat99/Pinger");

                    Console.ForegroundColor = originalForecolor;

                    Configs.Settings.Enabled = false;

                    break;

                default:
                    var originalForecolor2 = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(messageTag + "ERROR: The plugin's config field 'PingRequirementType'" +
                                      " is not set to a recognizable value, The plugin shall disable itself. " +
                                      "please read the list of valid values here: https://github.com/Maxthegreat99/Pinger");

                    Console.ForegroundColor = originalForecolor2;

                    Configs.Settings.Enabled = false;

                    break;
            }

            if (Configs.Settings.Enabled)
            {
                pingRequirementValue = Configs.Settings.PingRequirementValue;

                pingTimer = new(Configs.Settings.PingIntervalSec * 1000);
                pingTimer.Elapsed += PingTimer_Elapsed;
                pingTimer.Start();
            }
        }

        private static void PingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (!Configs.Settings.Enabled
                || !CanPluginPing()) 
                return;

            foreach (TSPlayer player in TShock.Players.Where(i => i != null
                                                                 && i.Active
                                                                 && !i.TPlayer.ghost)
                   )
            {
                Main.TriggerPing(player.LastNetPosition);
                
                Main.Pings.Add

                Main.TriggerPing(new Microsoft.Xna.Framework.Vector2(player.LastNetPosition.X * 16, player.LastNetPosition.Y * 16));

                player.SendSuccessMessage("uhg");

                var packet = new PacketFactory()
                        .SetType(82)
                        .PackUInt16(2)
                        .PackSingle(player.LastNetPosition.X * 16)
                        .PackSingle(player.LastNetPosition.Y * 16)
                        .GetByteData();

                var packet2 = new PacketFactory()
                        .SetType(82)
                        .PackUInt16(2)
                        .PackSingle(player.LastNetPosition.X )
                        .PackSingle(player.LastNetPosition.Y )
                        .GetByteData();

                TSPlayer.All.SendRawData(packet);
                TSPlayer.All.SendRawData(packet2);
            }
        }

        private static bool CanPluginPing()
        {
            switch (pingRequirementType)
            {
                case PingRequirementTypes.PLAYERS_ALIVE:
                    if (TShock.Players.Count(i => i != null 
                        && i.Active && !i.TPlayer.ghost) <=
                        pingRequirementValue)
                        return true;
                    break;

                case PingRequirementTypes.PLAYERS_DEAD:
                    if (TShock.Players.Count(i => i != null
                         && i.Active && (i.TPlayer.ghost || i.TPlayer.dead)) >=
                        pingRequirementValue)
                        return true;
                    break;

                case PingRequirementTypes.PERCENTAGE_ALIVE:
                    float percentageAlive = (TShock.Players.Count(i => i != null
                                             && i.Active && !i.TPlayer.ghost) /
                                             TShock.Utils.GetActivePlayerCount()) * 10;
                    if (percentageAlive <= pingRequirementValue)
                        return true;
                    break;
            }

            return false;
        }


        /***** Commands *****/

        private static void EnableCommand(CommandArgs args)
        {
            bool oldEnabledField = Configs.Settings.Enabled;

            Configs.Settings.Enabled = !Configs.Settings.Enabled;

            Configs.Write(configPath);

            LoadConfigs();

            if (Configs.Settings.Enabled != oldEnabledField)
                args.Player.SendSuccessMessage(messageTag + "Successfully " +
                                              ((Configs.Settings.Enabled) ? "En" : "Dis") + "abled the plugin!");

            else
                args.Player.SendErrorMessage(messageTag + "An error occured please check the console.");

        }

        private static void ReloadConfigs(CommandArgs args)
        {
            LoadConfigs();

            args.Player.SendInfoMessage(messageTag + "Reloaded the configs.");
       
        }
    }
}

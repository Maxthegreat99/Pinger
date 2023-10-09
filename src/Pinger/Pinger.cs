using System.Reflection;
using System.Timers;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.Net;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Pinger
{
    /* Pinger Plugin *
     Description: A plugin designed for HC mini-games or similar types of mini-games.
     The plugin is well-documented to assist beginners in learning TShock.
     If you would like to contribute to the documentation or suggest improvements,
     please submit a PR at https://github.com/Maxthegreat99/Pinger
     or contact the current project maintainer. */

    /* Plugin TSAPI Version Tag *
     This tag specifies the version of TSAPI that the plugin uses.
     It is required for the plugin to function properly. */
    [ApiVersion(2, 1)]
    public class Pinger : TerrariaPlugin
    {
        /***** Plugin Properties *****/

        /// <summary>
        /// This is displayed on startup and includes the name(s)
        /// of the author(s) maintaining the project.
        /// </summary>
        public override string Author => "Maxthegreat99";

        /// <summary>
        /// Brief description of the plugin's functionality.
        /// </summary>
        public override string Description => "A plugin conceived for HC minigames, that pings" +
                               " players once a number of them are left alive.";

        /// <summary>
        /// The name of your plugin, which is also displayed on startup.
        /// </summary>
        public override string Name => "Pinger";

        /// <summary>
        /// The current state of the plugin.
        /// It is recommended to follow a versioning convention when updating plugins,
        /// such as using Semantic Versioning (https://semver.org).
        /// </summary>
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version; /* you can also use new Version(X.Y.Z) */

        /***** Plugin Variables *****/

        private static string configPath = Path.Combine(TShock.SavePath, "PingerPlugin.json");
        
        /// <summary>
        /// The plugin's configurations. Please refer to the Config.cs file for more information.
        /// </summary>
        private static PingerConfigs Configs { get; set; } = new PingerConfigs();

        /// <summary>
        /// The tag that is included at the start of every message sent by the plugin to specify its source.
        /// </summary>
        private const string messageTag = "[Pinger] ";

        /// <summary>
        /// Enum types that define the criteria for the plugin to determine whether it should perform a ping or not.
        /// </summary>
        private enum PingRequirementTypes
        {
            PLAYERS_ALIVE,
            PLAYERS_DEAD,
            PERCENTAGE_ALIVE
        }

        /// <summary>
        /// The variable that represents the type of ping requirement and the associated value
        /// for when the players should be pinged.
        /// </summary>
        private static PingRequirementTypes pingRequirementType;

        private static int pingRequirementValue;

        /// <summary>
        /// The timer used to periodically ping players. 
        /// It should be disposed when the plugin shuts down.
        /// </summary>
        private static System.Timers.Timer pingTimer { get; set; }

        public Pinger(Main game) : base(game)
        {
            // You may optionally define the 
            // load order of the plugin here.
        }

        /// <summary>
        /// The plugin's permissions
        /// </summary>
        private static class Permissions
        {
            public const string RELOAD_PERM = "pinger.reload";

            public const string ENABLE_PERM = "pinger.enable";
        }

        /// <summary>
        /// Performs initialization tasks such as registering hooks,
        /// adding commands, and loading configuration
        /// </summary>
        public override void Initialize()
        {
            /* Hooks:
             * You can make your plugins listen to hooks from `ServerApi.Hooks`
             * and `TShockAPI.Hooks` to execute code when the hooks are fired.
             * Note that `TShockAPI.Hooks uses` events for hooks instead of registering/deregistering.
             * Read more: https://tshock.readme.io/docs/hooks, 
             *            https://github.com/TShockResources/ServerHooksExample 
             */

            /* Register the `OnInitialize` method to execute after 
            all other plugins and TShock finish loading (GameInitialize) */
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);

            // Using GeneralHooks from `TShockAPI.Hooks`, events are utilized here.
            GeneralHooks.ReloadEvent += OnReload;

            /* Commands:
             * Here we are adding our commands into TShock's ChatCommands
             * list, we are creating new commands with its permissions
             * and its names / aliases, optionally we can add a list
             * of permissions instead of one or define the plugin's
             * HelpText as well as other properties by adding
             * an object initializer. 
             * Read more: https://github.com/TShockResources/LavaSucks         
             */

            Commands.ChatCommands.Add(new Command(
                permissions: Permissions.ENABLE_PERM,
                cmd: EnableCommand,
                "pingerenable", "pingenable", "enableping"));

            Commands.ChatCommands.Add(new Command(
                permissions: Permissions.RELOAD_PERM,
                cmd: ReloadConfigs,
                "pingerreload", "pingreload", "reloadping"));
        }
        /// <summary>
        /// Called when the plugin is destroyed or when the server is shut down.
        /// Used to dispose the plugin's resources and deregister hooks.
        /// </summary>
        /// <param name="disposing"></param>

        protected override void Dispose(bool disposing)
        {
         /* If the plugin isn't disposing, we let `base.Dispose()` handle the work.
            Otherwise, we perform our job and let `base.Dispose()` finish. */
            if (!disposing)
            {
                base.Dispose(disposing);
                return;
            }

            ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            
            GeneralHooks.ReloadEvent -= OnReload;

            if (Configs.Settings.Enabled)
            {
                pingTimer.Stop();
                pingTimer.Elapsed -= PingTimer_Elapsed;
                pingTimer.Dispose();
            }

            base.Dispose(disposing);

        }

        /***** Plugin Hooks *****/
        /// <summary>
        /// Called once TShock / Every plugin initialized,
        /// executes config loading logic.
        /// </summary>
        /// <param name="args"></param>
        private static void OnInitialize(/* note the EventArgs parameter --> */EventArgs args)
        {
            LoadConfigs();
        }
        /// <summary>
        /// Called when TShock reloads,
        /// here we are simply re-loading the configs.
        /// </summary>
        /// <param name="args"></param>
        private static void OnReload(/* --> */ReloadEventArgs args)
        {
            LoadConfigs();
        }

        /// <summary>
        /// Config loading logic, the configs use TShockAPI.Configuration
        /// to facilitate Read&Write / loading default configs.
        /// </summary>
        private static void LoadConfigs()
        {
            /* if the file doesn't exist or is missing 
             * configuration we are adding them / creating a new config file */

            bool writeConfig = true;
            if (File.Exists(configPath))
                Configs.Read(configPath, out writeConfig);

            if (writeConfig)
                Configs.Write(configPath);


            /* theres no need to continue if the plugin is disabled */
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

                    TShock.Log.ConsoleError(messageTag + "ERROR: The plugin's config field 'PingRequirementValue'" +
                                      " is less than 1 or greater than 10, The plugin shall disable itself. " +
                                      "please read the documentation to discover how the plugin" +
                                      " checks for the percentage of players alive: https://github.com/Maxthegreat99/Pinger");

                    Configs.Settings.Enabled = false;

                    break;

                default:

                    TShock.Log.ConsoleError(messageTag + "ERROR: The plugin's config field 'PingRequirementType'" +
                                      " is not set to a recognizable value, The plugin shall disable itself. " +
                                      "please read the list of valid values here: https://github.com/Maxthegreat99/Pinger");

                    Configs.Settings.Enabled = false;

                    break;
            }

            /* initialize the timer and start it if the plugin is enabled */
            if (Configs.Settings.Enabled)
            {
                pingRequirementValue = Configs.Settings.PingRequirementValue;

                pingTimer = new(Configs.Settings.PingIntervalSec * 1000);
                pingTimer.Elapsed += PingTimer_Elapsed;
                pingTimer.Start();
            }
        }

        /// <summary>
        /// Logic executed when the timer is elapsed.
        /// Creates a ping at the position of each currently alive player
        /// and notifies everyone.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PingTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (!Configs.Settings.Enabled
                || !CanPluginPing()) 
                return;

            foreach (var position in TShock.Players.Where(i => i != null
                                                                 && i.Active
                                                                 && !i.TPlayer.ghost
                                                                 && !i.TPlayer.dead)
                                                                 .Select(i => i.TPlayer.position)
                    )
            {

                /* Packets:
                 * Packets are pieces of data the clients or the server send between each other to sync data 
                 * or notify change(list of packets: https://tshock.readme.io/docs/multiplayer-packet-structure),
                 * here we want to notify the clients about a new ping getting created.
                 * We can achieve this by using a special type of packet that can be created with `Terraria.GameContent.NetModules`.
                 * The packet can then be sent to everyone with `NetManager.Instance.Broadcast(packet)`.
                 * Other ways to send packets include `NetMessage.SendData()`, `TSPlayer.SendData()`,
                 * and PacketFactories + `TSPlayer.SendRawData()`(https://github.com/Maxthegreat99/PacketFactory). 
                 * Read more: https://tshock.readme.io/docs/multiplayer-packet-structure,
                 *            https://github.com/TShockResources/LavaSucks/blob/master/LavaSucks/LavaSucks.cs,
                 * repo using `SendData()` Example: https://github.com/Maxthegreat99/Ghost2
                 * repo using `GetData()` Example:  https://github.com/Maxthegreat99/MapTeleport
                 */

                /* initiate the change on the server first */
                Main.Pings.Add(new Microsoft.Xna.Framework.Vector2(position.X / 16, position.Y / 16));

                /* create a packet using Terraria.GameContent.NetModules.NetPingModule. 
                 * The method only requires the position at which we want the
                 * ping to appear, the PacketType, Size, etc... are handled by the method here.
                 * Note: we are deviding the player's position by 16 as it is currently in pixels,
                 * while the packet requires the ping position in tiles. */
                var packet = NetPingModule.Serialize(new(position.X / 16, position.Y / 16 ));
                /*                         ^ The method transforms the Vector2 into data that can be sent
                 *                           easily via network(byte[]) then creates a packet that can be
                 *                           understood by the receivers */
             
                /* Send the packet */
                NetManager.Instance.Broadcast(packet);
            }
        }

        /// <summary>
        /// Checks if the plugin can ping the players depending on the configs.
        /// </summary>
        /// <returns></returns>
        private static bool CanPluginPing()
        {
            switch (pingRequirementType)
            {
                case PingRequirementTypes.PLAYERS_ALIVE:
                    if (TShock.Players.Count(i => i != null 
                        && i.Active && !i.TPlayer.ghost && !i.TPlayer.dead) <=
                        pingRequirementValue)
                        return true;
                    break;

                case PingRequirementTypes.PLAYERS_DEAD:
                    if (TShock.Players.Count(i => i != null
                        && i.Active && (i.TPlayer.ghost || i.TPlayer.dead)) 
                        >= pingRequirementValue)
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


        /* Commands:
         * TShock provides the sender and the parameters as context (args) for your commands.
         * This gives to create sub-commands or even further nested commands (macro-sub commands)
         * for your plugin. However, it is important to handle the input of each parameter to ensure
         * that the input is in the correct type for manipulation. It is even possible to make your commands
         * send other commands using `Commands.HandleCommand(player, text)`.
         * Example Plugin: https://github.com/Maxthegreat99/CustomItems
         */

        /// <summary>
        /// Command to enable/disable the plugin.
        /// </summary>
        /// <param name="args"></param>
        private static void EnableCommand(CommandArgs args)
        {
            // Prepare the timer to be re-initialized
            if (Configs.Settings.Enabled)
            {
                pingTimer.Stop();
                pingTimer.Elapsed -= PingTimer_Elapsed;
                pingTimer.Dispose();
            }

            // save the old enable field
            bool oldEnabledField = Configs.Settings.Enabled;

            Configs.Settings.Enabled = !Configs.Settings.Enabled;

            Configs.Write(configPath);

            // reload the configs
            LoadConfigs();

            // inform the success if the field was properly changed
            if (Configs.Settings.Enabled != oldEnabledField)
                args.Player.SendSuccessMessage(messageTag + "Successfully " +
                                              ((Configs.Settings.Enabled) ? "en" : "dis") + "abled the plugin!");

            else
                args.Player.SendErrorMessage(messageTag + "An error occurred, please check the console.");

        }

        /// <summary>
        /// Command to reload the configs
        /// </summary>
        /// <param name="args"></param>
        private static void ReloadConfigs(CommandArgs args)
        {
            if (Configs.Settings.Enabled)
            {
                pingTimer.Stop();
                pingTimer.Elapsed -= PingTimer_Elapsed;
                pingTimer.Dispose();
            }
            LoadConfigs();

            args.Player.SendInfoMessage(messageTag + "Reloaded the configs.");
       
        }

        /* If you have any questions you can message me on discord(fireball_2000) or
         * ask on the official Pyraxis discord(https://discord.gg/Cav9nYX)
         * More Resources: https://tshock.readme.io/docs/getting-started
         *                 https://github.com/SignatureBeef/Open-Terraria-API/wiki/%5Bupcoming%5D-1.-About
         *                 https://github.com/pryaxis/tshock
         *                 https://github.com/TShockResources
         *                 https://www.youtube.com/watch?v=GgghXJbue70&t=213s
         *                 https://www.youtube.com/watch?v=g2t6LMHhIrQ
         *                 https://www.youtube.com/watch?v=jHStRTAda78&t=21s
         *                 https://www.youtube.com/watch?v=40i1zdQRYmY
         *                 https://discord.com/channels/479657350043664384/662607497441574922/913584702416384040          
         */
    }
}

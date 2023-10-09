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
    /*                          * The Pinger Plugin *
     * Description: A plugin made for HC mini-games or other mini-games of the type.
     * The plugin has also been explicitly documented in order to help beginners
     * learn how to use TShock. If you want to help with the documentation
     * or add to the plugin feel free to PR at https://github.com/Maxthegreat99/Pinger
     * or to whoever is currently maintaining the project
     */

    /* Tag specifying the version
     * of TSAPI, required for the plugin to work. 
     */
    [ApiVersion(2, 1)]
    public class Pinger : TerrariaPlugin
    {
        /***** Plugin Properties *****/

        /// <summary>
        /// This appears on startup, contains the author(s) 
        /// maintaining project.
        /// </summary>
        public override string Author => "Maxthegreat99";

        /// <summary>
        /// A short description of what the plugin does.
        /// </summary>
        public override string Description => "A plugin conceived for HC minigames, that pings" +
                               " players once a number of them are left alive.";

        /// <summary>
        /// The name of your plugin (also appears on startup).
        /// </summary>
        public override string Name => "Pinger";

         /// <summary>
         /// the current state of the plugin, its recommended
         /// to follow a convention when updating your plugins, 
         /// example: https://semver.org
         /// </summary>
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version; /* you can also use new Version(X.Y.Z) */

        /***** Plugin Variables *****/

        private static string configPath = Path.Combine(TShock.SavePath, "PingerPlugin.json");
        
        /// <summary>
        /// The plugin's configurations, see Config.cs 
        /// </summary>
        private static PingerConfigs Configs { get; set; } = new PingerConfigs();

        /// <summary>
        /// Included before each message the plugin sends
        /// to specify from where the message is from.
        /// </summary>
        private const string messageTag = "[Pinger] ";

        /// <summary>
        /// Enum types defining how the plugin checks for if it should ping or not.
        /// </summary>
        private enum PingRequirementTypes
        {
            PLAYERS_ALIVE,
            PLAYERS_DEAD,
            PERCENTAGE_ALIVE
        }

        /// <summary>
        /// The type variable itself and the value associated 
        /// to when it should ping the players
        /// </summary>
        private static PingRequirementTypes pingRequirementType;

        private static int pingRequirementValue;

        private static System.Timers.Timer pingTimer { get; set; }

        public Pinger(Main game) : base(game)
        {
            /* you can define when the plugin 
             * will load (order) here */
        }

        /// <summary>
        /// Stores the plugin's permissions
        /// </summary>
        private static class Permissions
        {
            public const string RELOAD_PERM = "pinger.reload";

            public const string ENABLE_PERM = "pinger.enable";
        }

        /// <summary>
        /// Executes initialization logic (Hook registering,
        /// Command adding, Config loading)
        /// </summary>
        public override void Initialize()
        {
            /* Hooks:
             * You can make you plugins list to hooks from `ServerApi.Hooks` 
             * and `TShockAPI.Hooks` in order for them to execute code
             * when the said hooks are fired. Note that `TShockAPI.Hooks` uses
             * events(built into c#) for hooks instead of registering/deregistering.
             * Read more: https://tshock.readme.io/docs/hooks, 
             *            https://github.com/TShockResources/ServerHooksExample 
             */

            /* Makes `OnInitialize` execute when every other plugin 
             * and TShock itself finished loading(GameInitilize) */
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);

            /* Use of GeneralHooks from `TShockAPI.Hooks, note
             * the use of events. */
            GeneralHooks.ReloadEvent += OnReload;

            /* Commands:
             * Here we are adding our commands into TShock,
             * we are creating new commands with its permissions
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
        /// Called when the plugin is destroyed
        /// or that the server is shut down, is used
        /// to dispose the plugin's resources and deregister hooks.
        /// </summary>
        /// <param name="disposing"></param>

        protected override void Dispose(bool disposing)
        {
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
        /// Called once TShock / Every plugins initialized,
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
        /// Logic executed when the timer is elapsed,
        /// creates a ping at every currently alive players' positions
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
                 * packets can be sent from the player or from the server some even
                 * syncing the data between both(look at: https://tshock.readme.io/docs/multiplayer-packet-structure),
                 * here we want to notify everyone about a new ping getting created, fortunately thats a special
                 * type of packet(https://tshock.readme.io/docs/multiplayer-packet-structure#net-modules)
                 * that can be created by simply using `Terraria.GameContent.NetModules` then we can directly send it to everyone with
                 * `NetManager.Instance.Broadcast(packet)`, other ways to send packets are `NetMessage.SendData()`, `TSPlayer.SendData()`
                 * and PacketFactories + `TSPlayer.SendRawData()`(https://github.com/Maxthegreat99/PacketFactory). 
                 * Read more: https://tshock.readme.io/docs/multiplayer-packet-structure,
                 *            https://github.com/TShockResources/LavaSucks/blob/master/LavaSucks/LavaSucks.cs,
                 * repo using `SendData()` Example: https://github.com/Maxthegreat99/Ghost2
                 * repo using `GetData()` Example:  https://github.com/Maxthegreat99/MapTeleport
                 */

                /* initiate the change on the server first */
                Main.Pings.Add(new Microsoft.Xna.Framework.Vector2(position.X / 16, position.Y / 16));

                /* create a packet using Terraria.GameContent.NetModules.NetPingModule */
                var packet = NetPingModule.Serialize(new(position.X /16, position.Y / 16)); 
                /*                         ^ The method transforms the vector2 into data that can be sent
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
         * TShock only gives you the sender and the parameters as
         * context(args) for your commands which is quite enough as this allows
         * you to make sub commands or even as sub-sub commands(macro-sub commands?)
         * for your commands, at the cost of course of having to handle the input of each 
         * parameter making sure the data is in the right type to manipulate it, it is  
         * even possible to make your commands send other commands(Commands.HandleCommand(player,text)). 
         * Read More: https://github.com/Maxthegreat99/CustomItems
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
         * More resources: https://tshock.readme.io/docs/getting-started
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

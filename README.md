# Pinger
A plugin conceived for Hardcore Challenge minigames, that pings players once a number of them are left alive.

## How to Install
1. Put the .dll into the `\ServerPlugins\` folder.
2. Restart the server.
3. Give your desired group the the permissions defined in the configs folder.

## User Instructions
### Commands and Usage
- `/pingreload` - Reloads the plugin's configurations. Requires the `pinger.reload` permission to use.
- `/pingenable` - Enables or disables the plugin. Requires the `pinger.enable` permission to use.

### Configs
- `Enabled` - Determines whether the plugin is enabled or not.
- `PingRequirementType` - Specifies how the plugin checks if it should ping the server's players. Valid options are: `PlayersAlive`, `PlayersDead`, and `PercentageAlive`.
- `PingRequirementValue` - The associated value for the `PingRequirementType`. For example, if the value is `5` and the `PingRequirementType` is `PlayersAlive`, the plugin will ping the players when there are `5` or fewer players left alive. If the `PingRequirementType` is `PlayersDead`, it will ping when there are `5` or more players dead. If the `PingRequirementType` is `PercentageAlive`, the value should be between `1` and `10`, a value of `5` would represent **50% or less** of players left alive.
- `PingIntervalSec` - The time interval in seconds at which the plugin should ping the players.


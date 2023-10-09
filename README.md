# Pinger
A plugin conceived for Hardcore Chalenge minigames, that pings players once a number of them are left alive.

## How to Install
1. Put the .dll into the `\ServerPlugins\` folder.
2. Restart the server.
3. Give your desired group the the permissions defined in the configs folder.

## User Instructions
### Commands and Usage
- `/pingreload` - reloads the plugin's configs, requires the `pinger.reload` perm to use.
- `/pingenable` - enables/disables the plugin, requires the `pinger.enable` perm to use.

### Configs
- `Enabled` - Whether or not the plugin is enabled.
- `ghost.hardghost` - .
- `PingRequirementType` - The way the plugin shall check for if it should ping the server's players or not, its value can only be one of these: `PlayersAlive`,`PlayersDead`, `PercentageAlive`.
- `PingRequirementValue` - the value associated with the RequirementType, for instance if the value is `5` PingRequirementType is `PlayersAlive` it will ping the players once there are 5 or less players left alive, for `PlayersDead` it will be 5 or more dead and for `PercentageAlive` it will be **50% or less** left alive, this means that when the type is PercentageAlive, the value should only be between 1 and 10.
- `PingIntervalSec` - The time interval in seconds at which the plugin should ping the players.


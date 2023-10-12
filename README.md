# Pinger
This plugin is designed for Hardcore Challenge minigames or other PvP challenges. It creates a ping at the position of each currently alive player once a configurable timer has elapsed and a specific number of players are still alive. 

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
- `PingRequirementType` - Specifies when the plugin should ping the players. Valid options are:
  - `PlayersAlive`, setting to this basicly translates to "the plugin should ping the players when a certain number of players are left alive",
  - `PlayersDead`, setting to this would translate to "the plugin should ping the players when a certain number of players are dead"
  - `PercentageAlive`, finally this translate to "the plugin should ping the players when a certain percentage of players are left alive".
- `PingRequirementValue` - This is the value in numbers that should be associated with the `PingRequirementType` field, these are the meaning of this field when `PingRequirementType` field is set to:
  - `PlayersAlive`, `PingRequirementType` represents the maximum number of players that should be alive for the plugin to ping players when `PlayersAlive` is `PingRequirementType`'s value.
  - `PlayersDead`, in this case the value represent the minimum amount of players to be dead (ghosts) for the plugin to ping its alive players.
  - `PercentageAlive`, in the case of this type the `PingRequirementValue` should be between 1-10, so for example setting the value to `5` would mean saying that "the plugin should ping its players once 50% of the players are left alive".
- `PingIntervalSec` - The time interval in seconds at which the plugin should ping its players, for instance setting it to 10 shall create a ping on the server's alive players every 10 seconds.

### Default Configs
Here are the default configs, as you can see the `PingRequirementType` field is set to `PlayersAlive` while `PingRequirementValue` and `PingIntervalSec` are both set to `5`, this means that the plugin shall ping the server's currently-alive players every `5` seconds once there are `5` or less players left alive.
```json
{
  "Settings": {
    "Enabled": true,
    "PingRequirementType": "PlayersAlive",
    "PingRequirementValue": 5,
    "PingIntervalSec": 5
  }
}
```

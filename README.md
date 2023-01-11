# MoreVanillaBuilds
[Changelog](https://github.com/galathil/MoreVanillaBuilds/blob/main/CHANGELOG.md)
## What is MoreVanillaBuilds?
MoreVanillaBuilds (MVB) is a Valheim mod to make all vanilla prefabs buildable with the hammer (survival way). This mod is inspirated by [BetterCreative](https://github.com/heinermann/Valheim_mods/tree/main/BetterCreative).

For each detected prefab in the game you can : 
 - Enable/Disable it from the hammer
 - Define custom recipe to build the prefab ingame
 
## How to install it?
You need a modded instance of Valheim ([BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)) and [Jötunn](https://www.nexusmods.com/valheim/mods/1138) plugin installed.

 1) Download the last `MoreVanillaBuilds.dll` available in the [releases](https://github.com/galathil/MoreVanillaBuilds/releases) section.
 2) Place the `MoreVanillaBuilds.dll` into your `BepInEx\plugins` folder
 3) You need launch the game first (and enter in a world) to generate the configuration files. The plugin search for prefabs in the game loading screen.
 4) Stop the game. You found a `fr.galathil.MoreVanillaBuilds.cfg` in your `BepInEx\config` folder, open it to customize mod configuration (describe below)

## How to configure the mod? 
Open `fr.galathil.MoreVanillaBuilds.cfg` in your `BepInEx\config` with a notepad.

You can find a FULL configuration made by AlpZz [here](https://github.com/galathil/MoreVanillaBuilds/releases/download/1.0.0/fr.galathil.MoreVanillaBuilds.cfg). Just replace existing configuration.

You need to edit the configuration file with client/server **off** ! If you use an ingame configuration manager, you need to restart the game/server to apply configuration.

In the `main` section you found : 
 - `forceAllPrefabs = false` change `false` to `true` for enable all prefabs in the hammer.
 - `verboseMode = false` You should keep it to `false`. This configuration display informations in the console (and slow down game performances)

The rest of the configuration files contains `[prefab-xxxxxx]` sections to configure each prefab. Each section contains : 
 - `isEnable = false`. Change it to `true` to show the prefab in the hammer. Note that if `forceAllPrefabs` is set to `true`, this config is ignored.
 - `category = Misc`. In wich tab the prefab should appear. Vanilla categories are : `Misc | Crafting | Building | Furniture`.
 - `requirements = `. The requirements to build the prefab. By default, no requirements needed (like creative mod). Each requirement is separated by a semicolon (`;`). Each requirement contain the itemID and the quantity separated by a comma (`,`). You can find itemID on [Valheim Wiki](https://valheim.fandom.com/wiki/Wood) or on this link : https://valheim-modding.github.io/Jotunn/data/objects/item-list.html. Example : `requirements = Wood,5;Stone,2`, in this case you need 5 woods and 2 stones to build the prefab
 
 ## Who create this mod?
  - [AlpZz](https://www.twitch.tv/alpzz_) : project manager, tester
  - [Galathil](https://github.com/galathil/) : developer

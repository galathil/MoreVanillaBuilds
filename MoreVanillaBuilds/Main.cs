using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace MoreVanillaBuilds
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        const string pluginGUID = "fr.galathil.MoreVanillaBuilds";
        const string pluginName = "MoreVanillaBuilds";
        const string pluginVersion = "1.0.0";
        public static ManualLogSource logger;
        public static ConfigFile config;
        private readonly Harmony harmony = new Harmony(pluginGUID);

        public void Awake()
        {
            logger = Logger;
            MVBConfig.setConfigFile(Config);
            MVBConfig.loadMainConfig();
            harmony.PatchAll();
        }
    }
}

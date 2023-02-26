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
        const string pluginVersion = "1.1.3";
        
        public static ConfigFile config;
        private readonly Harmony harmony = new Harmony(pluginGUID);

        public void Awake()
        {
            MVBLog.init(Logger);
            MVBConfig.setConfigFile(Config);
            MVBConfig.loadMainConfig();
            harmony.PatchAll();
        }
    }
}

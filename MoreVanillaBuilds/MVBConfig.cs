using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using UnityEngine;

namespace MoreVanillaBuilds
{
    class MVBConfig
    {
        private static ConfigFile configFile;

        private static string MainSectionName = "main";
        private static ConfigEntry<bool> forceAllPrefabs;
        private static ConfigEntry<bool> verboseMode;

        public static void setConfigFile(ConfigFile file) 
        {
            configFile = file;
        }

        public static void loadMainConfig() 
        {
            forceAllPrefabs = configFile.Bind(MainSectionName, "forceAllPrefabs", false, new ConfigDescription("If enable, allow all filtered prefabs from the game with/within configurations (requirements)."));
            verboseMode = configFile.Bind(MainSectionName, "verboseMode", false, new ConfigDescription("If enable, print debug informations in console."));
        }

        public static void save() 
        {
            configFile.Save();
        }

        public static MVBPrefabConfig loadPrefabConfig(GameObject prefab)
        {
            MVBPrefabConfig prefabConfig = new MVBPrefabConfig();
            String sectionName = "prefab-" + prefab.name;
            prefabConfig.isEnable = configFile.Bind(sectionName, "isEnable", false);
            prefabConfig.category = configFile.Bind(sectionName, "category", "Misc");
            prefabConfig.requirements = configFile.Bind(sectionName, "requirements", "");
            return prefabConfig;
        }

        public static bool isVerbose()
        {
            return verboseMode.Value;
        }

        public static bool isForceAllPrefabs()
        {
            return forceAllPrefabs.Value;
        }
    }
}

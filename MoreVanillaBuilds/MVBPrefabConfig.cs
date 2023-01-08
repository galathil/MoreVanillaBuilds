using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreVanillaBuilds
{
    class MVBPrefabConfig
    {
        public ConfigEntry<bool> isEnable { get; set; }
        public ConfigEntry<string> category { get; set; }
        public ConfigEntry<string> requirements { get; set; }
    }
}

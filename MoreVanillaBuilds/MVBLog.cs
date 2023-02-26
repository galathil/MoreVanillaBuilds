using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreVanillaBuilds
{
    class MVBLog
    {
        private static ManualLogSource _logger;

        public static void init(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void info(object data)
        {
            if (MVBConfig.isVerbose())
            {
                _logger.LogInfo(data);
            }
        }

        public static void prefab(GameObject prefab)
        {
            info("***** " + prefab.name + " *****");
            foreach (Component compo in prefab.GetComponents<Component>())
            {
                info("-" + compo.GetType().Name);
                PropertyInfo[] properties = prefab.GetType().GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    info("  -" + property.Name + " = " + property.GetValue(prefab));
                }
            }
            info("***** " + prefab.name + " (childs) *****");
            foreach (Transform child in prefab.transform)
            {

                info("-" + child.gameObject.name);

                foreach (Component component in child.gameObject.GetComponents<Component>())
                {
                    info("  -" + component.GetType().Name);
                    PropertyInfo[] properties = component.GetType().GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        info("    -" + property.Name + " = " + property.GetValue(component));
                    }
                }
            }
        }
    }
}

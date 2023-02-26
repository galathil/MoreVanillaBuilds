using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoreVanillaBuilds
{
    [HarmonyPatch]
    class Patches
    {
        // Hook just before Jotunn registers the Pieces
        [HarmonyPatch(typeof(ObjectDB), "Awake"), HarmonyPrefix]
        static void ObjectDBAwakePrefix()
        {
            if (SceneManager.GetActiveScene().name == "main")
            {
                Prefabs.FindAndRegisterPrefabs();
            }
        }

        static bool settingUpPlacementGhost = false;

        // Detours Player.SetupPlacementGhost
        // Refs: 
        //  - Player.m_buildPieces
        //  - PieceTable.GetSelectedPrefab
        [HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
        class PlayerSetupPlacementGhost
        {
            static void Prefix(Player __instance)
            {
                GameObject selected = __instance?.GetSelectedPiece()?.gameObject;

                if (selected != null)
                {
                    settingUpPlacementGhost = true;
                }
            }

            static void Postfix()
            {
                settingUpPlacementGhost = false;
            }
        }


        [HarmonyPatch(typeof(UnityEngine.Object), "Internal_CloneSingle", new Type[] { typeof(UnityEngine.Object) }), HarmonyPrefix]
        static bool ObjectInstantiate1Prefix(ref UnityEngine.Object __result, UnityEngine.Object data)
        {
            if (settingUpPlacementGhost)
            {
                settingUpPlacementGhost = false;
                if (Prefabs.AddedPrefabs.Contains(data.name))
                {
                    Jotunn.Logger.LogInfo($"Setting up placement ghost for {data.name}");

                    var staging = new GameObject();
                    staging.SetActive(false);

                    var ghostPrefab = UnityEngine.Object.Instantiate(data, staging.transform, false);
                    Prefabs.PrepareGhostPrefab(ghostPrefab as GameObject);

                    __result = UnityEngine.Object.Instantiate(ghostPrefab);

                    UnityEngine.Object.DestroyImmediate(staging);
                    return false;
                }
            }
            return true;
        }

        // Detours Player.UpdatePlacementGhost
        // Refs:
        //  - Player.m_placementStatus
        //  - Player.PlacementStatus
        //  - Player.SetPlacementGhostValid
        //  - Player.m_placementGhost
        public static GameObject lastPlacementGhost = null;

        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost"), HarmonyPostfix]
        static void PlayerUpdatePlacementGhostPostfix(ref GameObject ___m_placementGhost, ref int ___m_placementStatus)
        {
            lastPlacementGhost = ___m_placementGhost;
            if (___m_placementGhost)
            {
                ___m_placementStatus = 0;
                ___m_placementGhost.GetComponent<Piece>().SetInvalidPlacementHeightlight(false);
            }
        }

        // Called when piece is just placed
        [HarmonyPatch(typeof(Piece), "SetCreator"), HarmonyPrefix]
        static void PieceSetCreatorPrefix(long uid, Piece __instance)
        {
            // Synchronize the positions and rotations of otherwise non-persistent objects
            var view = __instance.GetComponent<ZNetView>();
            if (view && !view.m_persistent)
            {
                view.SetPersistent(true);

                var sync = __instance.gameObject.GetComponent<ZSyncTransform>();
                if(sync == null)
                {
                    __instance.gameObject.AddComponent<ZSyncTransform>();
                }
                sync.m_syncPosition = true;
                sync.m_syncRotation = true;

            }
        }

        // Detours Player.SetLocalPlayer
        // Refs:
        //  - Console.TryRunCommand
        //  - Player.SetGodMode
        //  - Player.SetGhostMode
        //  - Player.ToggleNoPlacementCost
        //  - Player.m_placeDelay
        /*
        [HarmonyPatch(typeof(Player), "SetLocalPlayer"), HarmonyPostfix]
        static void SetLocalPlayerPostfix(Player __instance)
        {
            Console.instance.TryRunCommand("devcommands", silentFail: true, skipAllowedCheck: true);
            Player.m_debugMode = true;
            __instance.SetGodMode(true);
            __instance.SetGhostMode(true);
            __instance.ToggleNoPlacementCost();
        }
        */
    }
}

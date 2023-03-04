using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
        public static class Piece_DropResources_Transpiler
        {
            private static MethodInfo method_Piece_IsPlacedByPlayer = AccessTools.Method(typeof(Piece), nameof(Piece.IsPlacedByPlayer));
            private static FieldInfo field_Requirement_m_recover = AccessTools.Field(typeof(Piece.Requirement), nameof(Piece.Requirement.m_recover));

            /// <summary>
            /// This transpiler does two things:
            ///
            /// * First, it patches IsPlacedByPlayer to always return true inside Piece.DropResources, ensuring the resources that drop are
            ///   never less than the resources it cost to build the piece in the first place.
            /// * Second, some pieces are marked m_recover = false (e.g. never drop). We can patch out this check to ensure that all pieces
            ///   always drop even if they have been marked by the Valheim devs to never drop.
            /// </summary>
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Source : https://github.com/valheimPlus/ValheimPlus/blob/development/ValheimPlus/GameClasses/Piece.cs

                List<CodeInstruction> il = instructions.ToList();

                // Patch out the call to Piece::IsPlacedByPlayer().
                // We want to always return true from this call site rather than whatever the original function does.
                // We can't hook the original function because the JIT inlines it.
                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].Calls(method_Piece_IsPlacedByPlayer))
                    {
                        il[i] = new CodeInstruction(OpCodes.Ldc_I4_1, null); // replace with a true return value
                        il.RemoveAt(i - 1); // remove prev ldarg.0
                    }
                }

                // Patch out the m_recover check.
                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].LoadsField(field_Requirement_m_recover))
                    {
                        il.RemoveRange(i - 1, 3); // ldloc.3, ldfld, brfalse
                    }
                }

                return il.AsEnumerable();
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

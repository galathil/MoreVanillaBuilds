using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx.Configuration;
using UnityEngine.EventSystems;
using System.Reflection;

namespace MoreVanillaBuilds
{
    class Prefabs
    {
        public static HashSet<string> AddedPrefabs = new HashSet<string>();

        public static void FindAndRegisterPrefabs()
        {
            MVBLog.info("FindAndRegisterPrefabs()");
            ZNetScene.instance.m_prefabs
            .Where(go => go.transform.parent == null && !ShouldIgnorePrefab(go))
            .OrderBy(go => go.name)
            .ToList()
            .ForEach(CreatePrefabPiece);
            MVBConfig.save();
        }

        private static readonly HashSet<string> IgnoredPrefabs = new HashSet<string>() {
          "Player", "Valkyrie", "HelmetOdin", "CapeOdin", "CastleKit_pot03", "Ravens", "TERRAIN_TEST", "PlaceMarker", "Circle_section",
          "guard_stone_test", "Haldor", "odin", "dvergrprops_wood_stake"
        };

        private static bool ShouldIgnorePrefab(GameObject prefab)
        {
            // Ignore existing pieces
            HashSet<string> prefabsToSkip = GetExistingPieceNames();
            if (prefabsToSkip.Contains(prefab.name))
            {
                return true;
            }

            // Ignore specific prefab names
            if (IgnoredPrefabs.Contains(prefab.name))
            {
                return true;
            }

            // Customs filters
            if (prefab.GetComponent("Projectile") != null ||
                prefab.GetComponent("Humanoid") != null ||
                prefab.GetComponent("AnimalAI") != null ||
                prefab.GetComponent("Character") != null ||
                prefab.GetComponent("CreatureSpawner") != null ||
                prefab.GetComponent("SpawnArea") != null ||
                prefab.GetComponent("Fish") != null ||
                prefab.GetComponent("RandomFlyingBird") != null ||
                prefab.GetComponent("MusicLocation") != null ||
                prefab.GetComponent("Aoe") != null ||
                prefab.GetComponent("ItemDrop") != null ||
                prefab.GetComponent("DungeonGenerator") != null ||
                prefab.GetComponent("TerrainModifier") != null ||
                prefab.GetComponent("EventZone") != null ||
                prefab.GetComponent("LocationProxy") != null ||
                prefab.GetComponent("LootSpawner") != null ||
                prefab.GetComponent("Mister") != null ||
                prefab.GetComponent("Ragdoll") != null ||
                prefab.GetComponent("MineRock5") != null ||
                prefab.GetComponent("TombStone") != null ||
                prefab.GetComponent("LiquidVolume") != null ||
                prefab.GetComponent("Gibber") != null ||
                prefab.GetComponent("TimedDestruction") != null ||
                prefab.GetComponent("TeleportAbility") != null ||
                prefab.GetComponent("ShipConstructor") != null ||
                prefab.GetComponent("TriggerSpawner") != null ||
                prefab.GetComponent("TeleportAbility") != null ||
                prefab.GetComponent("TeleportWorld") != null ||
                
                prefab.name.StartsWith("_") ||
                prefab.name.StartsWith("OLD_") ||
                prefab.name.EndsWith("_OLD") ||
                prefab.name.StartsWith("vfx_") ||
                prefab.name.StartsWith("sfx_") ||
                prefab.name.StartsWith("fx_")
            )
            {
                return true;
            }
            return false;
        }

        // Refs:
        //  - PieceTable
        //  - PieceTable.m_pieces
        private static HashSet<string> pieceNameCache = null;
        private static HashSet<string> GetExistingPieceNames()
        {
            if (pieceNameCache == null)
            {
                var result = Resources.FindObjectsOfTypeAll<PieceTable>()
                  .SelectMany(pieceTable => pieceTable.m_pieces)
                  .Select(piece => piece.name);

                pieceNameCache = new HashSet<string>(result);
            }
            return pieceNameCache;
        }

        private static bool EnsureNoDuplicateZNetView(GameObject prefab)
        {
            var views = prefab.GetComponents<ZNetView>();
            for (int i = 1; i < views.Length; ++i)
            {
                GameObject.DestroyImmediate(views[i]);
            }

            return views.Length <= 1;
        }

        private static void InitPieceData(GameObject prefab)
        {
            if (prefab.GetComponent<Piece>() == null)
            {
                Piece piece = prefab.AddComponent<Piece>();
                if (piece != null)
                {
                    piece.m_enabled = true;
                    piece.m_canBeRemoved = true;
                    piece.m_groundPiece = false;
                    piece.m_groundOnly = false;
                    piece.m_noInWater = false;
                    piece.m_notOnWood = false;
                    piece.m_notOnTiltingSurface = false;
                    piece.m_notOnFloor = false;
                    piece.m_allowedInDungeons = true;
                    piece.m_onlyInTeleportArea = false;
                    piece.m_inCeilingOnly = false;
                    piece.m_cultivatedGroundOnly = false;
                    piece.m_onlyInBiome = Heightmap.Biome.None;
                    piece.m_allowRotatedOverlap = true;
                }
            }
        }

        private static void CreatePrefabPiece(GameObject prefab)
        {
            if (!EnsureNoDuplicateZNetView(prefab))
            {
                if (MVBConfig.isVerbose())
                {
                    MVBLog.info("Ignore " + prefab.name);
                }
                return;
            }

            /*
            if (MVBConfig.isVerbose())
            {
                MVBLog.info("Initialize '" + prefab.name + "'");
                foreach (Component compo in prefab.GetComponents<Component>())
                {
                    MVBLog.info("  - " + compo.GetType().Name);
                }
            }
            */

            MVBPrefabConfig prefabConfig = MVBConfig.loadPrefabConfig(prefab);
            if (!prefabConfig.isEnable.Value && !MVBConfig.isForceAllPrefabs())
            {
                // prefab denied by config
                return;
            }

            PatchPrefabIfNeeded(prefab);
            InitPieceData(prefab);

            var pieceConfig = new PieceConfig
            {
                Name = prefab.name,
                Description = GetPrefabFriendlyName(prefab),
                PieceTable = "_HammerPieceTable",
                Category = prefabConfig.category.Value,
                AllowedInDungeons = true,
                Icon = CreatePrefabIcon(prefab),
            };

            if (!prefabConfig.requirements.Value.Equals(""))
            {
                foreach (string reqConfLine in prefabConfig.requirements.Value.Split(';'))
                {
                    string[] values = reqConfLine.Split(',');
                    RequirementConfig reqConf = new RequirementConfig();
                    reqConf.Item = values[0];
                    reqConf.Amount = int.Parse(values[1]);
                    reqConf.Recover = true;
                    pieceConfig.AddRequirement(reqConf);
                }
            }

            var piece = new CustomPiece(prefab, true, pieceConfig);
            PieceManager.Instance.AddPiece(piece);
            AddedPrefabs.Add(prefab.name);
        }

        /**
         * Fix collider and snap points on the prefab if necessary
         */
        private static void PatchPrefabIfNeeded(GameObject prefab)
        {
            switch (prefab.name)
            {
                case "blackmarble_column_3":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-1, 4, -1),
                        new Vector3(1, 4, 1),
                        new Vector3(-1, 4, 1),
                        new Vector3(1, 4, -1),

                        new Vector3(-1, 2, -1),
                        new Vector3(1, 2, 1),
                        new Vector3(-1, 2, 1),
                        new Vector3(1, 2, -1),

                        new Vector3(-1, 0, -1),
                        new Vector3(1, 0, 1),
                        new Vector3(-1, 0, 1),
                        new Vector3(1, 0, -1),

                        new Vector3(-1, -2, -1),
                        new Vector3(1, -2, 1),
                        new Vector3(-1, -2, 1),
                        new Vector3(1, -2, -1),

                        new Vector3(-1, -4, -1),
                        new Vector3(1, -4, 1),
                        new Vector3(-1, -4, 1),
                        new Vector3(1, -4, -1),
                    });

                    break;
                case "blackmarble_creep_4x1x1":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-0.5f, 2, -0.5f),
                        new Vector3(0.5f, 2, 0.5f),
                        new Vector3(-0.5f, 2, 0.5f),
                        new Vector3(0.5f, 2, -0.5f),

                        new Vector3(-0.5f, -2, -0.5f),
                        new Vector3(0.5f, -2, 0.5f),
                        new Vector3(-0.5f, -2, 0.5f),
                        new Vector3(0.5f, -2, -0.5f),

                        new Vector3(0, -2, 0),
                        new Vector3(0, 2, 0),
                    });

                    // ? Place the piece randomly in horizontal or vertical position ?
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("new").gameObject.GetComponent<RandomPieceRotation>());

                    break;
                case "blackmarble_creep_4x2x1":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(1, 2, -0.5f),
                        new Vector3(1, 2, 0.5f),
                        new Vector3(-1, 2, -0.5f),
                        new Vector3(-1, 2, 0.5f),

                        new Vector3(1, -2, -0.5f),
                        new Vector3(1, -2, 0.5f),
                        new Vector3(-1, -2, -0.5f),
                        new Vector3(-1, -2, 0.5f),

                        new Vector3(0, -2, 0),
                        new Vector3(0, 2, 0),
                    });

                    // ? Place the piece randomly in horizontal or vertical position ?
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("new").gameObject.GetComponent<RandomPieceRotation>());
                    break;
                case "blackmarble_creep_slope_inverted_1x1x2":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-0.5f, 1, -0.5f),
                        new Vector3(0.5f, 1, 0.5f),
                        new Vector3(-0.5f, 1, 0.5f),
                        new Vector3(0.5f, 1, -0.5f),

                        new Vector3(0.5f, -1, -0.5f),
                        new Vector3(-0.5f, -1, -0.5f),
                    });
                    break;
                case "blackmarble_creep_slope_inverted_2x2x1":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-1, 0.5f, -1),
                        new Vector3(1, 0.5f,1),
                        new Vector3(-1, 0.5f, 1),
                        new Vector3(1, 0.5f, -1),

                        new Vector3(-1, -0.5f, -1),
                        new Vector3(1, -0.5f, -1),
                    });
                    break;
                case "blackmarble_creep_stair":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-1, 1, -1),
                        new Vector3(1, 1, -1),
                        new Vector3(-1, 0, -1),
                        new Vector3(1, 0, -1),
                        new Vector3(-1, 0, 1),
                        new Vector3(1, 0, 1),
                    });
                    break;
                case "blackmarble_floor_large":
                    List<Vector3> points = new List<Vector3>();
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int x = -4; x <= 4; x += 2)
                        {
                            for (int z = -4; z <= 4; z += 2)
                            {
                                points.Add(new Vector3(x, y, z));
                            }
                        }
                    }
                    generateSnapPoints(prefab, points);
                    break;
                case "blackmarble_head_big01" :
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-1, 1, -1),
                        new Vector3(1, 1,1),
                        new Vector3(-1, 1, 1),
                        new Vector3(1, 1, -1),

                        new Vector3(1, -1, -1),
                        new Vector3(-1, -1, -1),
                    });
                    break;
                case "blackmarble_head_big02":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-1, 1, -1),
                        new Vector3(1, 1,1),
                        new Vector3(-1, 1, 1),
                        new Vector3(1, 1, -1),

                        new Vector3(1, -1, -1),
                        new Vector3(-1, -1, -1),
                    });
                    break;
                case "blackmarble_out_2":
                    // Temp fix collider
                    UnityEngine.Object.DestroyImmediate(prefab.GetComponent<MeshCollider>());
                    prefab.AddComponent<BoxCollider>();
                    prefab.GetComponent<BoxCollider>().size = new Vector3(2, 2, 2);
                    break;
                case "blackmarble_tile_floor_1x1":
                    prefab.transform.Find("_snappoint").gameObject.transform.localPosition = new Vector3(0.5f,0.1f,0.5f);
                    prefab.transform.Find("_snappoint (1)").gameObject.transform.localPosition = new Vector3(0.5f, 0.1f, -0.5f);
                    prefab.transform.Find("_snappoint (2)").gameObject.transform.localPosition = new Vector3(-0.5f, 0.1f, 0.5f);
                    prefab.transform.Find("_snappoint (3)").gameObject.transform.localPosition = new Vector3(-0.5f, 0.1f, -0.5f);
                    break;
                case "blackmarble_tile_floor_2x2":
                    prefab.transform.Find("_snappoint").gameObject.transform.localPosition = new Vector3(1, 0.1f, 1);
                    prefab.transform.Find("_snappoint (1)").gameObject.transform.localPosition = new Vector3(1, 0.1f, -1);
                    prefab.transform.Find("_snappoint (2)").gameObject.transform.localPosition = new Vector3(-1, 0.1f, 1);
                    prefab.transform.Find("_snappoint (3)").gameObject.transform.localPosition = new Vector3(-1, 0.1f, -1);
                    break;
                case "blackmarble_tile_wall_1x1":
                    prefab.transform.Find("_snappoint").gameObject.transform.localPosition = new Vector3(0.5f, 0, 0.1f);
                    prefab.transform.Find("_snappoint (1)").gameObject.transform.localPosition = new Vector3(0.5f, 1, 0.1f);
                    prefab.transform.Find("_snappoint (2)").gameObject.transform.localPosition = new Vector3(-0.5f, 0, 0.1f);
                    prefab.transform.Find("_snappoint (3)").gameObject.transform.localPosition = new Vector3(-0.5f, 1, 0.1f);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (4)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (5)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (6)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (7)").gameObject);
                    break;
                case "blackmarble_tile_wall_2x2":
                    prefab.transform.Find("_snappoint").gameObject.transform.localPosition = new Vector3(1, 0, 0.1f);
                    prefab.transform.Find("_snappoint (1)").gameObject.transform.localPosition = new Vector3(1, 2, 0.1f);
                    prefab.transform.Find("_snappoint (2)").gameObject.transform.localPosition = new Vector3(-1, 0, 0.1f);
                    prefab.transform.Find("_snappoint (3)").gameObject.transform.localPosition = new Vector3(-1, 2, 0.1f);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (4)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (5)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (6)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (7)").gameObject);
                    break;
                case "blackmarble_tile_wall_2x4":
                    prefab.transform.Find("_snappoint").gameObject.transform.localPosition = new Vector3(1, 0, 0.1f);
                    prefab.transform.Find("_snappoint (1)").gameObject.transform.localPosition = new Vector3(1, 4, 0.1f);
                    prefab.transform.Find("_snappoint (2)").gameObject.transform.localPosition = new Vector3(-1, 0, 0.1f);
                    prefab.transform.Find("_snappoint (3)").gameObject.transform.localPosition = new Vector3(-1, 4, 0.1f);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (4)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (5)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (6)").gameObject);
                    UnityEngine.Object.DestroyImmediate(prefab.transform.Find("_snappoint (7)").gameObject);
                    break;
                case "dungeon_queen_door":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2.5f, 0, 0),
                        new Vector3(-2.5f, 0, 0),
                    });
                    break;
                case "dungeon_sunkencrypt_irongate":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(1, -0.4f, 0),
                        new Vector3(-1, -0.4f, 0),
                    });
                    break;
                case "sunken_crypt_gate":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(1, 0, 0),
                        new Vector3(-1, 0, 0),
                    });
                    break;
                case "dvergrprops_wood_beam":
                    /*generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(3, 0.45f, 0.45f),
                        new Vector3(3, 0.45f, -0.42f),
                        new Vector3(3, -0.45f, -0.42f),
                        new Vector3(3, -0.45f, -0.45f),

                        new Vector3(-3, 0.45f, 0.45f),
                        new Vector3(-3, 0.45f, -0.42f),
                        new Vector3(-3, -0.45f, -0.42f),
                        new Vector3(-3, -0.45f, -0.45f),
                    });*/
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(3, 0, 0),
                        new Vector3(-3, 0, 0),
                    });
                    break;
                case "dvergrprops_wood_pole":
                    /*generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0.45f, 2, 0.45f),
                        new Vector3(-0.45f, 2, 0.45f),
                        new Vector3(0.45f, 2, -0.45f),
                        new Vector3(-0.45f, 2, -0.45f),
                        new Vector3(0.45f, -2, 0.45f),
                        new Vector3(-0.45f, -2, 0.45f),
                        new Vector3(0.45f, -2, -0.45f),
                        new Vector3(-0.45f, -2, -0.45f),
                    });*/
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0, 2, 0),
                        new Vector3(0, -2, 0),
                    });
                    break;
                case "dvergrprops_wood_wall":
                    // Patch only the floor
                    /*generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2.2f, -2, -0.45f),
                        new Vector3(2.2f, -2, 0.45f),
                        new Vector3(-2.2f, -2, -0.45f),
                        new Vector3(-2.2f, -2, 0.45f),
                    });*/
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2.2f, 2, 0),
                        new Vector3(-2.2f, 2, 0),
                        new Vector3(2.2f, -2, 0),
                        new Vector3(-2.2f, -2, 0),
                    });
                    break;
                case "dvergrtown_arch":
                    /*
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(1, 0, -0.5f),
                        new Vector3(1, 0, 0.5f),
                        new Vector3(1, 1, -0.5f),
                        new Vector3(1, 1, 0.5f),

                        new Vector3(-1, 1, -0.5f),
                        new Vector3(-1, 1, 0.5f),

                        new Vector3(-1, -1, 0.5f),
                        new Vector3(-1, -1, -0.5f),
                        new Vector3(0, -1, 0.5f),
                        new Vector3(0, -1, -0.5f),
                    });
                    */
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(1, 0.5f, 0),
                    });
                    break;

                case "dvergrtown_secretdoor":
                    /*
                     generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2, 0, -0.35f),
                        new Vector3(2, 0, 0.4f),
                        new Vector3(-2, 0, -0.35f),
                        new Vector3(-2, 0, 0.4f),
                        
                        new Vector3(2, 4, -0.35f),
                        new Vector3(2, 4, 0.4f),
                        new Vector3(-2, 4, -0.35f),
                        new Vector3(-2, 4, 0.4f),
                    });*/
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2, 0, 0),
                        new Vector3(-2, 0, 0),
                        new Vector3(2, 4, 0),
                        new Vector3(-2, 4, 0),
                    });
                    break;
                case "dvergrtown_slidingdoor":
                    /*generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2, 0, 0),
                        new Vector3(2, 0, 0.2f),
                        new Vector3(-2, 0, -0.2f),
                        new Vector3(-2, 0, 0.2f),

                        new Vector3(2, 4, -0.2f),
                        new Vector3(2, 4, 0.2f),
                        new Vector3(-2, 4, -0.2f),
                        new Vector3(-2, 4, 0.2f),
                    });
                    */
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2, 0, 0),
                        new Vector3(-2, 0, 0),
                        new Vector3(2, 4, 0),
                        new Vector3(-2, 4, 0),
                    });
                    break;
                case "dvergrtown_stair_corner_wood_left":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0.25f, 0, -0.25f),
                        new Vector3(0.25f, 0, 0.25f),
                        new Vector3(0.25f, 1.1f, -0.25f),
                        new Vector3(0.25f, 1.1f, 0.25f),

                        new Vector3(0.25f, 0, 2),
                        new Vector3(-0.25f, 1.1f, -0.25f),

                        new Vector3(-2, 1.1f, -0.25f),


                    });
                    break;
                case "dvergrtown_wood_beam":
                    /*
                     generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(3, 0.45f, 0.45f),
                        new Vector3(3, 0.45f, -0.42f),
                        new Vector3(3, -0.45f, -0.42f),
                        new Vector3(3, -0.45f, -0.45f),

                        new Vector3(-3, 0.45f, 0.45f),
                        new Vector3(-3, 0.45f, -0.42f),
                        new Vector3(-3, -0.45f, -0.42f),
                        new Vector3(-3, -0.45f, -0.45f),
                    });
                    */
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(3, 0, 0),
                        new Vector3(-3, 0, 0),
                    });
                    break;
                case "dvergrtown_wood_pole":
                    /*generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0.45f, 2, 0.45f),
                        new Vector3(-0.45f, 2, 0.45f),
                        new Vector3(0.45f, 2, -0.45f),
                        new Vector3(-0.45f, 2, -0.45f),
                        new Vector3(0.45f, -2, 0.45f),
                        new Vector3(-0.45f, -2, 0.45f),
                        new Vector3(0.45f, -2, -0.45f),
                        new Vector3(-0.45f, -2, -0.45f),
                    });*/
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0, -2, 0),
                        new Vector3(0, 2, 0),
                    });
                    break;
                case "dvergrtown_wood_stake":
                    // Patch only the floor
                    /*
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0.15f, 0, 0.15f),
                        new Vector3(-0.15f, 0, 0.15f),
                        new Vector3(0.15f, 0, -0.15f),
                        new Vector3(-0.15f, 0, -0.15f),
                    });
                    */
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0, 0, 0),
                    });
                    break;
                case "dvergrtown_wood_crane":
                    /*
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0.4f, -3, 0.4f),
                        new Vector3(-0.4f, -3, 0.4f),
                        new Vector3(0.4f, -3, -0.4f),
                        new Vector3(-0.4f, -3, -0.4f),
                    });*/
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(0, -3, 0),
                    });
                    break;
                case "dvergrtown_wood_support":
                    /*
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-2.4f, 0, -0.4f),
                        new Vector3(-2.4f, 0, 0.4f),
                        new Vector3(-1.6f, 0, -0.4f),
                        new Vector3(-1.6f, 0, 0.4f),

                        new Vector3(2.4f, 0, -0.4f),
                        new Vector3(2.4f, 0, 0.4f),
                        new Vector3(1.6f, 0, -0.4f),
                        new Vector3(1.6f, 0, 0.4f),
                    });*/
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-2, 0, 0),
                        new Vector3(2, 0, 0),
                    });
                    break;
                case "dvergrtown_wood_wall01":
                    // Fix collider y
                    prefab.transform.Find("wallcollider").transform.localPosition = new Vector3(0, 0, 0);
                    /*
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(3, -2.7f, -0.45f),
                        new Vector3(3, -2.7f, 0.45f),
                        new Vector3(-3, -2.7f, -0.45f),
                        new Vector3(-3, -2.7f, 0.45f),

                        new Vector3(3, 2.7f, -0.45f),
                        new Vector3(3, 2.7f, 0.45f),
                        new Vector3(-3, 2.7f, -0.45f),
                        new Vector3(-3, 2.7f, 0.45f),
                    });
                    */
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-3, -2.7f, 0),
                        new Vector3(3, -2.7f, 0),
                        new Vector3(-3, 2.7f, 0),
                        new Vector3(3, 2.7f, 0),
                    });
                    break;
                case "dvergrtown_wood_wall02":
                    /*
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(3, -0.6f, -0.25f),
                        new Vector3(3, -0.6f, 0.25f),
                        new Vector3(-3, -0.6f, -0.25f),
                        new Vector3(-3, -0.6f, 0.25f),

                        new Vector3(3, 4.6f, -0.25f),
                        new Vector3(3, 4.6f, 0.25f),
                        new Vector3(-3, 4.6f, -0.25f),
                        new Vector3(-3, 4.6f, 0.25f),
                    });
                    */

                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-3, -0.5f, 0),
                        new Vector3(3, -0.5f, 0),
                        new Vector3(-3, 4.5f, 0),
                        new Vector3(3, 4.5f, 0),
                    });

                    break;
                case "dvergrtown_wood_wall03":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(1.1f, 0, 0),
                        new Vector3(-1.1f, 0, 0),

                        new Vector3(2, 2, 0),
                        new Vector3(-2, 2, 0),

                        new Vector3(1.1f, 4, 0),
                        new Vector3(-1.1f, 4, 0),
                    });
                    break;
                case "goblin_roof_45d":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(1, 0, 1),
                        new Vector3(-1, 0, 1),
                        new Vector3(1, 2, -1),
                        new Vector3(-1, 2, -1),
                    });
                    break;
                case "goblin_roof_45d_corner":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-1, 0, -1),
                        new Vector3(1, 0, 1),
                    });
                    break;
                case "goblin_woodwall_1m":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-0.5f, 0, 0),
                        new Vector3(0.5f, 0, 0),
                        new Vector3(-0.5f, 2, 0),
                        new Vector3(0.5f, 2, 0),
                    });
                    break;
                case "goblin_woodwall_2m":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(-1, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(-1, 2, 0),
                        new Vector3(1, 2, 0),
                    });
                    break;
                case "Ice_floor":
                    generateSnapPoints(prefab, new Vector3[] {
                        new Vector3(2, 1, 2),
                        new Vector3(-2, 1, -2),
                        new Vector3(2, 1, -2),
                        new Vector3(-2, 1, 2),

                        new Vector3(2, -1, 2),
                        new Vector3(-2, -1, -2),
                        new Vector3(2, -1, -2),
                        new Vector3(-2, -1, 2),
                    });
                    break;
                default:
                    break;
            }
        }

        private static void generateSnapPoints(GameObject prefab, Vector3[] positions)
        {
            for(int i=0; i<positions.Length; i++)
            {
                GameObject snappoint = new GameObject("_snappointt"+i);
                snappoint.transform.parent = prefab.transform;
                snappoint.transform.localPosition = positions[i];
                snappoint.tag = "snappoint";
            }
        }

        private static void generateSnapPoints(GameObject prefab, List<Vector3> positions)
        {
            generateSnapPoints(prefab, positions.ToArray());
        }

        private static string GetPrefabFriendlyName(GameObject prefab)
        {
            HoverText hover = prefab.GetComponent<HoverText>();
            if (hover) return hover.m_text;

            ItemDrop item = prefab.GetComponent<ItemDrop>();
            if (item) return item.m_itemData.m_shared.m_name;

            Character chara = prefab.GetComponent<Character>();
            if (chara) return chara.m_name;

            RuneStone runestone = prefab.GetComponent<RuneStone>();
            if (runestone) return runestone.m_name;

            ItemStand itemStand = prefab.GetComponent<ItemStand>();
            if (itemStand) return itemStand.m_name;

            MineRock mineRock = prefab.GetComponent<MineRock>();
            if (mineRock) return mineRock.m_name;

            Pickable pickable = prefab.GetComponent<Pickable>();
            if (pickable) return GetPrefabFriendlyName(pickable.m_itemPrefab);

            CreatureSpawner creatureSpawner = prefab.GetComponent<CreatureSpawner>();
            if (creatureSpawner) return GetPrefabFriendlyName(creatureSpawner.m_creaturePrefab);

            SpawnArea spawnArea = prefab.GetComponent<SpawnArea>();
            if (spawnArea && spawnArea.m_prefabs.Count > 0)
            {
                return GetPrefabFriendlyName(spawnArea.m_prefabs[0].m_prefab);
            }

            Piece piece = prefab.GetComponent<Piece>();
            if (piece && !string.IsNullOrEmpty(piece.m_name)) return piece.m_name;

            return prefab.name;
        }

        // Refs:
        //  - CreatureSpawner.m_creaturePrefab
        //  - PickableItem.m_randomItemPrefabs
        //  - PickableItem.RandomItem.m_itemPrefab
        private static Sprite CreatePrefabIcon(GameObject prefab)
        {
            Sprite result = generateObjectIcon(prefab);

            if (result == null)
            {
                PickableItem.RandomItem[] randomItemPrefabs = prefab.GetComponent<PickableItem>()?.m_randomItemPrefabs;
                if (randomItemPrefabs != null && randomItemPrefabs.Length > 0)
                {
                    GameObject item = randomItemPrefabs[0].m_itemPrefab?.gameObject;
                    if (item != null)
                    {
                        result = generateObjectIcon(item);
                    }
                }
            }

            return result;
        }

        private static Sprite generateObjectIcon(GameObject obj)
        {
            RenderManager.RenderRequest request = new RenderManager.RenderRequest(obj);
            request.Rotation = RenderManager.IsometricRotation;
            request.UseCache = true;
            return RenderManager.Instance.Render(request);
        }

        public static void PrepareGhostPrefab(GameObject ghost)
        {
            UnityEngine.Object.Destroy(ghost.GetComponent<CharacterDrop>());

            // Only keep components that are part of a whitelist
            var components = ghost.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                // Rigidbody, MeshFilter
                if (component is Piece ||
                  component is Collider ||
                  component is Renderer ||
                  component is Transform ||
                  component is ZNetView ||
                  component is Rigidbody ||
                  component is MeshFilter ||
                  component is LODGroup ||
                  component is PickableItem ||
                  component is Canvas ||
                  component is CanvasRenderer ||
                  component is UIBehaviour ||
                  component is WearNTear)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(component);
            }

            // Needed to make some things work, like Stalagmite, Rock_destructible, Rock_7, silvervein, etc.
            
            
            Bounds desiredBounds = new Bounds();
            foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>())
            {
                desiredBounds.Encapsulate(renderer.bounds);
            }
            var collider = ghost.AddComponent<BoxCollider>();
            collider.center = desiredBounds.center;
            collider.size = desiredBounds.size;
            

            // Only if there is no collider on the ghost.
            // Without it, the pieces doesn't snap "by the bottom"
            //if (ghost.GetComponent<Collider>() == null)
            //{

            //}
        }
    }
}

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

namespace MoreVanillaBuilds
{
    class Prefabs
    {
        public static HashSet<string> AddedPrefabs = new HashSet<string>();

        public static void FindAndRegisterPrefabs()
        {
            Main.logger.LogInfo("FindAndRegisterPrefabs()");
            ZNetScene.instance.m_prefabs
            .Where(go => go.transform.parent == null && !ShouldIgnorePrefab(go))
            .OrderBy(go => go.name)
            .ToList()
            .ForEach(CreatePrefabPiece);
            MVBConfig.save();
        }

        private static readonly HashSet<string> IgnoredPrefabs = new HashSet<string>() {
          "Player", "Valkyrie", "HelmetOdin", "CapeOdin", "CastleKit_pot03", "Ravens", "TERRAIN_TEST", "PlaceMarker", "Circle_section",
          "guard_stone_test", "Haldor", "odin"
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
                    //piece.m_clipEverything = true;
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
                    Main.logger.LogInfo("Ignore " + prefab.name);
                }
                return;
            }

            if (MVBConfig.isVerbose())
            {
                Main.logger.LogInfo("Initialize '" + prefab.name + "'");
                foreach (Component compo in prefab.GetComponents<Component>())
                {
                    Main.logger.LogInfo("  - " + compo.GetType().Name);
                }
            }

            MVBPrefabConfig prefabConfig = MVBConfig.loadPrefabConfig(prefab);
            if (!prefabConfig.isEnable.Value && !MVBConfig.isForceAllPrefabs())
            {
                // prefab denied by config
                return;
            }

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
                    pieceConfig.AddRequirement(reqConf);
                }
            }

            var piece = new CustomPiece(prefab, true, pieceConfig);
            PieceManager.Instance.AddPiece(piece);
            AddedPrefabs.Add(prefab.name);
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
            Sprite result = RenderManager.Instance.Render(prefab, RenderManager.IsometricRotation);
            if (result == null)
            {
                GameObject spawnedCreaturePrefab = prefab.GetComponent<CreatureSpawner>()?.m_creaturePrefab;
                if (spawnedCreaturePrefab != null)
                    result = RenderManager.Instance.Render(spawnedCreaturePrefab, RenderManager.IsometricRotation);
            }

            if (result == null)
            {
                PickableItem.RandomItem[] randomItemPrefabs = prefab.GetComponent<PickableItem>()?.m_randomItemPrefabs;
                if (randomItemPrefabs != null && randomItemPrefabs.Length > 0)
                {
                    GameObject item = randomItemPrefabs[0].m_itemPrefab?.gameObject;
                    if (item != null)
                        result = RenderManager.Instance.Render(item, RenderManager.IsometricRotation);
                }
            }

            if (result == null || SpriteIsBlank(result))
            {
                // TODO: Do something if there's still no image
            }
            return result;
        }

        private static bool SpriteIsBlank(Sprite sprite)
        {
            Color[] pixels = sprite.texture.GetPixels();
            foreach (var color in pixels)
            {
                if (color.a != 0) return false;
            }
            return true;
        }

        public static void PrepareGhostPrefab(GameObject ghost)
        {
            //ghost.DestroyComponent<CharacterDrop>();
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

        }
    }
}

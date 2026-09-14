using System;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using OCDheim.Utilities;
using System.IO;
using UnityEngine;

namespace OCDheim
{
    [HarmonyPatch]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInPlugin(GUID, Name, Version)]
    [NetworkCompatibility(CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Minor)]
    public class OCDheim : BaseUnityPlugin
    {
        public const string GUID = "dymek.dev.OCDheim";
        private const string Name = "OCDheim";
        private const string Version = "0.2.4";

        private static bool resourceBundleLoadAttempted;
        private static AssetBundle resourceBundleBacking;

        // The shader/material bundle is built by the Unity editor against an older engine than the one
        // Valheim currently ships (Unity 6). A bundle the running engine refuses to read must not take
        // the whole plugin down with it: every OCDheim feature except the World Grid overlay works fine
        // without it, so resolve it lazily and hand back null on failure.
        public static AssetBundle resourceBundle
        {
            get
            {
                if (resourceBundleLoadAttempted) { return resourceBundleBacking; }
                resourceBundleLoadAttempted = true;

                try
                {
                    resourceBundleBacking = LoadResourceBundle();
                }
                catch (Exception e)
                {
                    global::OCDheim.Logger.Warn(() => $"Could not load the OCDheim asset bundle, the World Grid overlay stays disabled: {e.Message}");
                }

                if (resourceBundleBacking == null)
                {
                    global::OCDheim.Logger.Warn(() => "OCDheim asset bundle is unavailable, the World Grid overlay stays disabled.");
                }

                return resourceBundleBacking;
            }
        }
        private Texture2D brick1x1 { get; } = LoadTextureFromDisk("brick_1x1.png");
        private Texture2D brick2x1 { get; } = LoadTextureFromDisk("brick_2x1.png");
        private Texture2D brick2x2 { get; } = LoadTextureFromDisk("brick_2x2.png");
        private Texture2D brick1x2 { get; } = LoadTextureFromDisk("brick_1x2.png");
        private Texture2D brick4x2 { get; } = LoadTextureFromDisk("brick_4x2.png");
        private Harmony harmony { get; } = new Harmony(GUID);

        private static AssetBundle LoadResourceBundle()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsServer:
                    return AssetUtils.LoadAssetBundleFromResources(Path.Combine("bundle_windows"));
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxServer:
                    return AssetUtils.LoadAssetBundleFromResources(Path.Combine("bundle_linux"));
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXServer:
                    return AssetUtils.LoadAssetBundleFromResources(Path.Combine("bundle_osx"));
                default:
                    throw new PlatformNotSupportedException(Application.platform.ToString());
            }
        }

        public static Texture2D LoadTextureFromDisk(string fileName)
        {
            var modDir = Path.GetDirectoryName(typeof(OCDheim).Assembly.Location) ?? throw new InvalidOperationException();
            var fullPath = Path.Combine(modDir,  fileName);

            return AssetUtils.LoadTexture(fullPath);
        }

        private void Awake()
        {
            global::OCDheim.Logger.logLevel = Config.Bind("Logging", "LogLevel", BepInEx.Logging.LogLevel.Info,
                "How chatty OCDheim is. Debug logs every piece the placement ghost evaluates and costs frames while building.").Value;

            PatchEachFeatureSeparately();
            gameObject.AddComponent<KeyBinder>();
            PrefabManager.OnVanillaPrefabsAvailable += AddOCDheimToolPieces;
            PrefabManager.OnVanillaPrefabsAvailable += AddOCDheimBuildPieces;
            PrefabManager.OnVanillaPrefabsAvailable += ModVanillaValheimTools;
        }

        // Harmony.PatchAll() gives up on the first patch class that will not apply, so one vanilla method whose
        // signature moved used to leave the entire mod unpatched. Patching class by class keeps the damage local:
        // the feature whose hook no longer fits goes quiet, everything else still works.
        private void PatchEachFeatureSeparately()
        {
            foreach (var type in AccessTools.GetTypesFromAssembly(typeof(OCDheim).Assembly))
            {
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    global::OCDheim.Logger.Warn(() => $"Could not apply the patches of '{type.Name}', that feature stays disabled: {e.Message}");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch(nameof(Player.OnSpawned))]
        private static void OnPlayerAvailable()
        {
            Refresher.Of(PrecisionDrill.groundLevels);
            Refresher.Of(PrecisionDrill.floorLevels);
        }

        private void AddOCDheimToolPieces()
        {
            //AddToolPiece<UndoModificationsOverlayVisualizer>("Undo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.undo);
            //AddToolPiece<RedoModificationsOverlayVisualizer>("Redo Terrain Modification", "mud_road_v2", "Hoe", OverlayVisualizer.redo);
            // The prefab name must not contain a space. Valheim hashes prefabs through Utils.GetPrefabName,
            // which truncates at the first '(' or ' ', so "Remove Terrain Modifications(Clone)" went over the
            // wire as plain "Remove" and never matched anything ObjectDB knew about.
            AddToolPiece<RemoveModificationsOverlayVisualizer>("ocdheim_remove_terrain_modifications", "Remove Terrain Modifications", "mud_road_v2", "Hoe", OverlayVisualizer.remove);
        }

        private void AddToolPiece<TOverlayVisualizer>(string prefabName, string displayName, string basePieceName, string pieceTable, Texture2D iconTexture, bool level = false, bool raise = false, bool smooth = false, bool paint = false) where TOverlayVisualizer: OverlayVisualizer
        {
            var pieceExists = PieceManager.Instance.GetPiece(prefabName);
            if (pieceExists != null) { return; }

            if (PrefabManager.Instance.GetPrefab(basePieceName) == null)
            {
                global::OCDheim.Logger.Warn(() => $"Vanilla prefab '{basePieceName}' is gone, skipping tool piece '{displayName}'");
                return;
            }

            var pieceIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            var piece = new CustomPiece(prefabName, basePieceName, new PieceConfig
            {
                Name = displayName,
                Icon = pieceIcon,
                PieceTable = pieceTable
            });

            var terrainOp = piece.PiecePrefab.GetComponent<TerrainOp>();
            if (terrainOp == null)
            {
                global::OCDheim.Logger.Warn(() => $"Prefab '{basePieceName}' carries no TerrainOp, skipping tool piece '{displayName}'");
                return;
            }

            var settings = terrainOp.m_settings;
            settings.m_level = level;
            settings.m_raise = raise;
            settings.m_smooth = smooth;
            settings.m_paintCleared = paint;
            piece.PiecePrefab.AddComponent<TOverlayVisualizer>();

            PieceManager.Instance.AddPiece(piece);
            TerrainOpRegistrar.Register(piece.PiecePrefab);
        }

        private void AddOCDheimBuildPieces()
        {
            AddBrickBuildPiece("1x1", new Vector3(0.5f, 1.0f, 0.5f), 3, brick1x1);
            AddBrickBuildPiece("2x1", new Vector3(1.0f, 1.0f, 0.5f), 4, brick2x1);
            AddBrickBuildPiece("1x2", new Vector3(0.5f, 2.0f, 0.5f), 5, brick1x2);
            AddBrickBuildPiece("4x2", new Vector3(2.0f, 2.0f, 0.5f), 6, brick4x2);
            AddBrickBuildPiece("2x2 (Vertical)", new Vector3(1.0f, 2.0f, 0.5f), 5, brick2x2);

            PrefabManager.OnVanillaPrefabsAvailable -= AddOCDheimBuildPieces;
        }

        private void AddBrickBuildPiece(string brickSuffix, Vector3 brickScale, int brickPrice, Texture2D iconTexture)
        {
            var brickName = $"Smooth Stone {brickSuffix}";
            var snakeSuffix = brickSuffix.Replace(" ", "_").Replace("(", "").Replace(")", "").ToLower();
            var brickExists = PieceManager.Instance.GetPiece(brickName);
            if (brickExists != null) { return; }
            
            var brick = PrefabManager.Instance.CreateClonedPrefab($"stone_floor_{snakeSuffix}", "stone_floor_2x2");
            if (brick == null)
            {
                global::OCDheim.Logger.Warn(() => $"Vanilla prefab 'stone_floor_2x2' is gone, skipping build piece '{brickName}'");
                return;
            }

            var brickIcon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), Vector2.zero);
            brick.transform.localScale = brickScale;

            var brickConfig = new PieceConfig();
            brickConfig.Name = brickName;
            brickConfig.PieceTable = "Hammer";
            brickConfig.Category = "HeavyBuild";
            brickConfig.Icon = brickIcon;
            brickConfig.AddRequirement(new RequirementConfig("Stone", brickPrice));

            PieceManager.Instance.AddPiece(new CustomPiece(brick, false, brickConfig));
        }

        private void ModVanillaValheimTools()
        {
            AddVisualizerTo<LevelGroundOverlayVisualizer>("mud_road_v2");
            AddVisualizerTo<RaiseGroundOverlayVisualizer>("raise_v2");
            AddVisualizerTo<PaveRoadOverlayVisualizer>("path_v2");
            AddVisualizerTo<PaveRoadOverlayVisualizer>("paved_road_v2");
            AddVisualizerTo<CultivateOverlayVisualizer>("cultivate_v2");
            AddVisualizerTo<SeedGrassOverlayVisualizer>("replant_v2");
        }

        // A renamed or removed vanilla prefab used to null-reference its way out of this callback and take
        // every tool that came after it down with it. Each tool now stands or falls on its own.
        private static void AddVisualizerTo<TOverlayVisualizer>(string prefabName) where TOverlayVisualizer : OverlayVisualizer
        {
            var prefab = PrefabManager.Instance.GetPrefab(prefabName);
            if (prefab == null)
            {
                global::OCDheim.Logger.Warn(() => $"Vanilla prefab '{prefabName}' is gone, skipping its {typeof(TOverlayVisualizer).Name}");
                return;
            }

            if (prefab.GetComponent<TOverlayVisualizer>() != null) { return; }

            prefab.AddComponent<TOverlayVisualizer>();
        }
    }
}

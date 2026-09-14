using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OCDheim
{

    [HarmonyPatch]
    public static class TerrainOpRegistrar
    {
        private static readonly List<GameObject> CustomTerrainOps = new List<GameObject>();

        public static void Register(GameObject prefab)
        {
            if (prefab == null || prefab.GetComponent<TerrainOp>() == null) { return; }
            if (CustomTerrainOps.Contains(prefab)) { return; }

            CustomTerrainOps.Add(prefab);
            AnnounceTo(ObjectDB.instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ObjectDB))]
        [HarmonyPatch(nameof(ObjectDB.UpdateRegisters))]
        private static void AnnounceAfterRebuild(ObjectDB __instance) => AnnounceTo(__instance);

        private static void AnnounceTo(ObjectDB objectDb)
        {
            if (objectDb == null || objectDb.m_terrainOpsByHash == null) { return; }

            foreach (var prefab in CustomTerrainOps)
            {
                if (prefab == null) { continue; }

                var terrainOp = prefab.GetComponent<TerrainOp>();
                if (terrainOp == null) { continue; }

                var wireName = Utils.GetPrefabName(prefab.name);
                if (wireName != prefab.name)
                {
                    Logger.Warn(() => $"TerrainOp prefab '{prefab.name}' normalises to '{wireName}'; rename it without spaces or brackets to avoid colliding with other prefabs");
                }

                var hash = wireName.GetStableHashCode();
                if (objectDb.m_terrainOpsByHash.ContainsKey(hash)) { continue; }

                objectDb.m_terrainOpsByHash[hash] = terrainOp;
                if (objectDb.m_terrainOps != null && !objectDb.m_terrainOps.Contains(terrainOp))
                {
                    objectDb.m_terrainOps.Add(terrainOp);
                }

                Logger.Debug(() => $"Announced TerrainOp '{prefab.name}' (hash {hash}) to ObjectDB");
            }
        }
    }
}

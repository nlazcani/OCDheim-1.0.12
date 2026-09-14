using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OCDheim
{
    // Valheim 1.0.12 stopped putting TerrainOp settings on the wire field by field. TerrainComp.ApplyOperation
    // now writes nothing but the prefab's name hash, and the receiving end resolves the settings back through
    // ObjectDB.TryGetTerrainOp. A TerrainOp prefab ObjectDB has never been told about deserializes to null and
    // the whole operation is cancelled:
    //
    //   Failed to deserialize TerrainOp settings for prefab hash 1388922078, cancelling TerrainOp.
    //   Did you add new TerrainOp object and not refresh ZNetScene list?
    //
    // which is exactly what happened to OCDheim's "Remove Terrain Modifications" hoe piece: it placed, it
    // logged, and it did nothing.
    //
    // Appending to ObjectDB.m_terrainOps is not enough on its own. ObjectDB.CopyOtherDB assigns another
    // instance's list wholesale and rebuilds the lookup from it, so a custom entry can be dropped again the
    // moment the database is copied. The lookup itself is therefore rewritten after every rebuild - the
    // registers are rebuilt by UpdateRegisters and nothing else, so that is the single place to hook.
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

                // TerrainOp.Settings.Serialize hashes Utils.GetPrefabName(instance.name), and that truncates at
                // the first '(' or ' ' - so a prefab named with spaces travels as its first word only. Key off
                // the same normalised name the wire will carry, and complain loudly about names that cannot
                // survive the trip.
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

                Logger.Info(() => $"Announced TerrainOp '{prefab.name}' (hash {hash}) to ObjectDB");
            }
        }
    }
}

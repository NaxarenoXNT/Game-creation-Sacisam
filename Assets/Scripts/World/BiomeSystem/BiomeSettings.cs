using System.Collections.Generic;
using UnityEngine;

namespace World.BiomeSystem
{
    /// <summary>
    /// Define los parámetros visuales de un bioma: color del terreno y transiciones.
    /// 
    /// DISEÑO ACTUAL:
    /// • El sistema de biomas SOLO controla el color del terreno (splatmap) y las
    ///   transiciones entre biomas. NO genera objetos ni vegetación proceduralmente.
    /// • Árboles, rocas y decoración se colocan manualmente con los pinceles de Unity Terrain.
    /// • Los campos de vegetación (treeDensity, rockDensity, etc.) se conservan para
    ///   compatibilidad con assets existentes pero NO son usados por ningún sistema runtime.
    /// 
    /// Crear desde: Project → Create → World → Biome Settings
    /// </summary>
    [CreateAssetMenu(menuName = "World/Biome Settings", fileName = "NewBiome")]
    public class BiomeSettings : ScriptableObject
    {
        [Header("Información")]
        public string biomeName = "Nuevo Bioma";
        public BiomeCategory category = BiomeCategory.Forest;
        [TextArea(2, 4)]
        public string description;

        // ─── Colores del Bioma ──────────────────────────────────────────────────
        [Header("Colores del Bioma")]
        [Tooltip("Color principal del follaje. Se aplica vía MaterialPropertyBlock (_TopColor) " +
                 "a todos los biomeTintedProps instanciados en la zona de este bioma.")]
        [ColorUsage(false, true)]
        public Color foliageColor = new Color(0.2f, 0.6f, 0.15f, 1f);

        // ─── Terreno ────────────────────────────────────────────────────────────
        [Header("Terreno (Splatmap)")]
        [Tooltip("TerrainLayer que representa el suelo de este bioma (pasto, nieve, arena, etc.). " +
                 "Se usa para pintar el alphamap del Terrain según la influencia del bioma.")]
        public TerrainLayer terrainLayer;

        // ─── Vegetación con Tinteo de Bioma (LEGACY — no se usa en runtime) ────
        [Header("[LEGACY] Vegetación — Tinteo por Bioma")]
        [Tooltip("LEGACY: Estos campos ya no se usan. La vegetación se coloca manualmente con Unity Terrain Tools.")]
        [Range(0f, 1f)]
        public float tintedTreeDensity = 0f;
        public List<WeightedPrefab> biomeTintedTrees = new List<WeightedPrefab>();

        [Range(0f, 1f)]
        public float tintedUnderstoryDensity = 0.3f;
        public List<WeightedPrefab> biomeTintedUnderstory = new List<WeightedPrefab>();

        [Range(0f, 1f)]
        public float tintedGroundCoverDensity = 0.5f;
        public List<WeightedPrefab> biomeTintedGroundCover = new List<WeightedPrefab>();

        // ─── Vegetación sin Tinteo (LEGACY — no se usa en runtime) ───────────────
        [Header("[LEGACY] Vegetación — Sin Tinteo")]
        [Tooltip("LEGACY: Estos campos ya no se usan. Colocar vegetación manualmente con Unity Terrain Tools.")]
        [Range(0f, 1f)]
        public float treeDensity = 0.5f;
        public List<WeightedPrefab> treeTypes = new List<WeightedPrefab>();
        [Tooltip("Distancia mínima entre árboles en metros")]
        public float minTreeSpacing = 4f;
        [Range(0f, 0.5f)]
        [Tooltip("Variación de escala aleatoria. 0.2 = ±20% del tamaño base")]
        public float treeScaleVariation = 0.15f;

        [Header("Árboles Grandes")]
        [Tooltip("Probabilidad (0-1) de que un árbol sea instanciado con escala grande.\n" +
                 "0 = nunca | 0.1 = 10% de los árboles serán grandes | 1 = todos grandes")]
        [Range(0f, 1f)]
        public float largeTreeChance = 0.1f;

        [Tooltip("Multiplicador de escala aplicado a los árboles 'grandes'.\n" +
                 "2 = el doble del tamaño base | 3 = el triple, etc.")]
        [Range(1f, 5f)]
        public float largeTreeScaleMultiplier = 2.5f;

        [Header("Rocas")]
        [Range(0f, 1f)]
        public float rockDensity = 0.1f;
        public List<WeightedPrefab> rockTypes = new List<WeightedPrefab>();
        public float minRockSpacing = 3f;

        [Header("Sotobosque (sin tinteo)")]
        [Range(0f, 1f)]
        [Tooltip("Arbustos, helechos, hongos, raíces con texturas propias.")]
        public float understoryDensity = 0.3f;
        public List<WeightedPrefab> understoryTypes = new List<WeightedPrefab>();
        [Tooltip("Distancia mínima entre elementos de sotobosque en metros")]
        public float minUnderstorySpacing = 1.5f;

        [Header("Cobertura de Suelo (sin tinteo)")]
        [Range(0f, 1f)]
        public float groundCoverDensity = 0.5f;
        public List<WeightedPrefab> groundCoverTypes = new List<WeightedPrefab>();

        // ─── Spacing compartido (tinted + default) ──────────────────────────────
        [Header("Spacing (compartido)")]
        [Tooltip("Distancia mínima entre árboles tinted en metros")]
        public float minTintedTreeSpacing = 4f;
        [Tooltip("Distancia mínima entre elementos de sotobosque tinted en metros")]
        public float minTintedUnderstorySpacing = 1.5f;

        [Header("Atmósfera")]
        [Tooltip("Partículas ambientales: niebla, polvo, ascuas, etc. Puede ser null.")]
        public GameObject ambientParticlesPrefab;

        [Header("Flags")]
        [Tooltip("Si está activo, este bioma no genera nada proceduralmente. " +
                 "Usar para ciudades, pueblos y dungeons donde todo es manual.")]
        public bool usesManualLayoutOnly = false;

        // ─── Helpers: Default Props ─────────────────────────────────────────────

        /// <summary>
        /// Elige un prefab de árbol (default, sin tinteo) al azar respetando los pesos.
        /// </summary>
        public GameObject PickTree(System.Random rng) => PickWeighted(treeTypes, rng);

        /// <summary>
        /// Elige un prefab de roca al azar respetando los pesos definidos.
        /// </summary>
        public GameObject PickRock(System.Random rng) => PickWeighted(rockTypes, rng);

        /// <summary>
        /// Elige un prefab de sotobosque (default) al azar respetando los pesos.
        /// </summary>
        public GameObject PickUnderstory(System.Random rng) => PickWeighted(understoryTypes, rng);

        /// <summary>
        /// Elige un prefab de cobertura de suelo (default) al azar respetando los pesos.
        /// </summary>
        public GameObject PickGroundCover(System.Random rng) => PickWeighted(groundCoverTypes, rng);

        // ─── Helpers: Biome-Tinted Props ────────────────────────────────────────

        /// <summary>
        /// Elige un prefab de árbol tinted al azar.
        /// </summary>
        public GameObject PickTintedTree(System.Random rng) => PickWeighted(biomeTintedTrees, rng);

        /// <summary>
        /// Elige un prefab de sotobosque tinted al azar.
        /// </summary>
        public GameObject PickTintedUnderstory(System.Random rng) => PickWeighted(biomeTintedUnderstory, rng);

        /// <summary>
        /// Elige un prefab de cobertura de suelo tinted al azar.
        /// </summary>
        public GameObject PickTintedGroundCover(System.Random rng) => PickWeighted(biomeTintedGroundCover, rng);

        // ─── Utilidad Interna ───────────────────────────────────────────────────

        private static GameObject PickWeighted(List<WeightedPrefab> list, System.Random rng)
        {
            if (list == null || list.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var entry in list)
                totalWeight += Mathf.Max(0f, entry.weight);

            if (totalWeight <= 0f) return list[0].prefab;

            float roll = (float)rng.NextDouble() * totalWeight;
            float cumulative = 0f;

            foreach (var entry in list)
            {
                cumulative += Mathf.Max(0f, entry.weight);
                if (roll <= cumulative)
                    return entry.prefab;
            }

            return list[list.Count - 1].prefab;
        }
    }

    // ─── Tipos de soporte ───────────────────────────────────────────────────────

    public enum BiomeCategory
    {
        Forest,
        Plains,
        Mountain,
        Arid,
        Coastal,
        Dark,
        Urban,
        Underground
    }

    [System.Serializable]
    public class WeightedPrefab
    {
        public GameObject prefab;
        [Range(0f, 1f)]
        public float weight = 1f;
    }
}
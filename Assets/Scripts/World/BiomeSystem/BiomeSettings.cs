using System.Collections.Generic;
using UnityEngine;

namespace World.BiomeSystem
{
    /// <summary>
    /// Define los parámetros de un bioma: qué genera, con qué densidad y con qué prefabs.
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

        [Header("Vegetación")]
        [Range(0f, 1f)]
        [Tooltip("Probabilidad de intentar colocar un árbol en cada punto candidato")]
        public float treeDensity = 0.5f;
        public List<WeightedPrefab> treeTypes = new List<WeightedPrefab>();
        [Tooltip("Distancia mínima entre árboles en metros")]
        public float minTreeSpacing = 4f;
        [Range(0f, 0.5f)]
        [Tooltip("Variación de escala aleatoria. 0.2 = ±20% del tamaño base")]
        public float treeScaleVariation = 0.15f;

        [Header("Rocas")]
        [Range(0f, 1f)]
        public float rockDensity = 0.1f;
        public List<WeightedPrefab> rockTypes = new List<WeightedPrefab>();
        public float minRockSpacing = 3f;

        [Header("Sotobosque")]
        [Range(0f, 1f)]
        [Tooltip("Arbustos, helechos, hongos, raíces, etc.")]
        public float understoryDensity = 0.3f;
        public List<WeightedPrefab> understoryTypes = new List<WeightedPrefab>();
        [Tooltip("Distancia mínima entre elementos de sotobosque en metros")]
        public float minUnderstorySpacing = 1.5f;

        [Header("Cobertura de Suelo")]
        [Range(0f, 1f)]
        public float groundCoverDensity = 0.5f;
        public List<WeightedPrefab> groundCoverTypes = new List<WeightedPrefab>();

        [Header("Atmósfera")]
        [Tooltip("Partículas ambientales: niebla, polvo, ascuas, etc. Puede ser null.")]
        public GameObject ambientParticlesPrefab;

        [Header("Flags")]
        [Tooltip("Si está activo, este bioma no genera nada proceduralmente. " +
                 "Usar para ciudades, pueblos y dungeons donde todo es manual.")]
        public bool usesManualLayoutOnly = false;

        // ─── Helpers ────────────────────────────────────────────────────────────

        /// <summary>
        /// Elige un prefab de árbol al azar respetando los pesos definidos.
        /// Devuelve null si la lista está vacía.
        /// </summary>
        public GameObject PickTree(System.Random rng) => PickWeighted(treeTypes, rng);

        /// <summary>
        /// Elige un prefab de roca al azar respetando los pesos definidos.
        /// </summary>
        public GameObject PickRock(System.Random rng) => PickWeighted(rockTypes, rng);

        /// <summary>
        /// Elige un prefab de sotobosque al azar respetando los pesos definidos.
        /// </summary>
        public GameObject PickUnderstory(System.Random rng) => PickWeighted(understoryTypes, rng);

        /// <summary>
        /// Elige un prefab de cobertura de suelo al azar respetando los pesos definidos.
        /// </summary>
        public GameObject PickGroundCover(System.Random rng) => PickWeighted(groundCoverTypes, rng);

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
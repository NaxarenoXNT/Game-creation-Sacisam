using System.Collections.Generic;
using UnityEngine;

namespace World.BiomeSystem
{
    /// <summary>
    /// Resultado de samplear el WorldBiomeMap en un punto XZ del mundo.
    /// Contiene todos los biomas que influyen en ese punto con sus pesos normalizados (suma = 1).
    /// </summary>
    public class BiomeSample
    {
        /// <summary>
        /// Lista de biomas con su peso de influencia, ordenada de mayor a menor peso.
        /// </summary>
        public readonly List<(BiomeSettings biome, float weight)> Influences;

        /// <summary>
        /// El bioma con mayor influencia en este punto.
        /// </summary>
        public BiomeSettings Dominant => Influences.Count > 0 ? Influences[0].biome : null;

        public BiomeSample(List<(BiomeSettings biome, float weight)> influences)
        {
            Influences = influences;
        }

        /// <summary>
        /// Calcula el valor blended de un parámetro float entre todos los biomas que influyen.
        /// Ejemplo: BlendFloat(b => b.treeDensity) → densidad de árboles mezclada por pesos.
        /// </summary>
        public float BlendFloat(System.Func<BiomeSettings, float> selector)
        {
            float result = 0f;
            foreach (var (biome, weight) in Influences)
                result += selector(biome) * weight;
            return result;
        }

        /// <summary>
        /// Elige un prefab de árbol considerando el blend de biomas.
        /// Primero elige el bioma por peso, luego elige el prefab dentro de ese bioma.
        /// </summary>
        public GameObject PickTree(System.Random rng) => PickFromBlend(rng, (b, r) => b.PickTree(r));

        /// <summary>
        /// Elige un prefab de roca considerando el blend de biomas.
        /// </summary>
        public GameObject PickRock(System.Random rng) => PickFromBlend(rng, (b, r) => b.PickRock(r));

        /// <summary>
        /// Elige un prefab de sotobosque considerando el blend de biomas.
        /// </summary>
        public GameObject PickUnderstory(System.Random rng) => PickFromBlend(rng, (b, r) => b.PickUnderstory(r));

        /// <summary>
        /// Elige un prefab de cobertura de suelo considerando el blend de biomas.
        /// </summary>
        public GameObject PickGroundCover(System.Random rng) => PickFromBlend(rng, (b, r) => b.PickGroundCover(r));

        /// <summary>
        /// Devuelve true si TODOS los biomas con influencia significativa son manuales.
        /// Se usa para no generar vegetación procedural en zonas urbanas.
        /// </summary>
        public bool IsFullyManual()
        {
            foreach (var (biome, weight) in Influences)
            {
                // Si algún bioma con más del 10% de influencia NO es manual, no es zona manual
                if (weight > 0.1f && !biome.usesManualLayoutOnly)
                    return false;
            }
            return true;
        }

        private GameObject PickFromBlend(System.Random rng, System.Func<BiomeSettings, System.Random, GameObject> picker)
        {
            if (Influences.Count == 0) return null;

            // Elegir bioma por peso
            float roll = (float)rng.NextDouble();
            float cumulative = 0f;

            foreach (var (biome, weight) in Influences)
            {
                cumulative += weight;
                if (roll <= cumulative)
                {
                    var result = picker(biome, rng);
                    if (result != null) return result;
                }
            }

            // Fallback: intentar con el dominante
            return picker(Dominant, rng);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using World.BiomeSystem;

namespace World.BiomeSystem
{
    /// <summary>
    /// Singleton que define el layout de biomas del mundo mediante puntos de control.
    /// Cada punto de control define qué bioma domina en esa posición del mundo.
    /// El sistema interpola entre puntos para crear transiciones suaves.
    ///
    /// SETUP: Agregar este componente a un GameObject en la escena (ej: "WorldBiomeMap").
    /// Debe estar presente ANTES de que WorldChunkManager empiece a cargar chunks.
    /// </summary>
    public class WorldBiomeMap : MonoBehaviour
    {
        public static WorldBiomeMap Instance { get; private set; }

        [Header("Puntos de Control")]
        [Tooltip("Cada punto define qué bioma domina en esa posición del mundo. " +
                 "El sistema interpola entre puntos para crear transiciones graduales.")]
        [SerializeField] private List<BiomeControlPoint> controlPoints = new List<BiomeControlPoint>();

        [Header("Blending")]
        [Tooltip("Radio de influencia de cada punto de control en unidades de mundo. " +
                 "Valores más altos = transiciones más suaves y amplias. " +
                 "Recomendado: 200-400 para chunks de 256u.")]
        [SerializeField] private float blendRadius = 300f;

        [Tooltip("Peso mínimo para que un bioma aparezca en el blend. " +
                 "Filtra biomas con influencia despreciable.")]
        [SerializeField] [Range(0.01f, 0.2f)] private float minInfluenceThreshold = 0.05f;

        [Tooltip("Bioma por defecto si no hay ningún punto de control cercano.")]
        [SerializeField] private BiomeSettings defaultBiome;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private float gizmoSphereRadius = 20f;

        // Acceso de solo lectura a los puntos para el Custom Editor
        public IReadOnlyList<BiomeControlPoint> ControlPoints => controlPoints;
        public float BlendRadius => blendRadius;

        // ─── Unity ──────────────────────────────────────────────────────────────

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WorldBiomeMap] Ya existe una instancia. Destruyendo duplicado.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── API Pública ─────────────────────────────────────────────────────────

        /// <summary>
        /// Samplea el mapa de biomas en una posición del mundo.
        /// Devuelve un BiomeSample con todos los biomas que influyen y sus pesos normalizados.
        /// El resultado siempre tiene al menos un bioma (el defaultBiome si no hay puntos cercanos).
        /// </summary>
        public BiomeSample GetBiomeAt(Vector3 worldPos)
        {
            var rawInfluences = new Dictionary<BiomeSettings, float>();

            foreach (var point in controlPoints)
            {
                if (point.dominantBiome == null) continue;

                float distance = Vector2.Distance(
                    new Vector2(worldPos.x, worldPos.z),
                    new Vector2(point.worldPosition.x, point.worldPosition.z)
                );

                // Fuera del radio → sin influencia
                if (distance >= blendRadius) continue;

                // Falloff cuadrático inverso: cerca = mucho peso, lejos = poco peso
                float t = 1f - (distance / blendRadius);
                float influence = t * t * point.influence;

                if (influence < minInfluenceThreshold) continue;

                if (rawInfluences.ContainsKey(point.dominantBiome))
                    rawInfluences[point.dominantBiome] += influence;
                else
                    rawInfluences[point.dominantBiome] = influence;
            }

            // Si no hay ningún punto cercano, usar el bioma por defecto
            if (rawInfluences.Count == 0)
            {
                var fallbackList = new List<(BiomeSettings, float)>();
                if (defaultBiome != null)
                    fallbackList.Add((defaultBiome, 1f));
                return new BiomeSample(fallbackList);
            }

            // Normalizar pesos para que sumen 1
            float total = 0f;
            foreach (var kvp in rawInfluences)
                total += kvp.Value;

            var influences = new List<(BiomeSettings biome, float weight)>();
            foreach (var kvp in rawInfluences)
                influences.Add((kvp.Key, kvp.Value / total));

            // Ordenar de mayor a menor peso
            influences.Sort((a, b) => b.weight.CompareTo(a.weight));

            return new BiomeSample(influences);
        }

        /// <summary>
        /// Versión rápida: devuelve solo el bioma dominante en una posición.
        /// Más eficiente cuando no necesitás el blend completo.
        /// </summary>
        public BiomeSettings GetDominantBiomeAt(Vector3 worldPos)
        {
            BiomeSettings dominant = defaultBiome;
            float bestInfluence = 0f;

            foreach (var point in controlPoints)
            {
                if (point.dominantBiome == null) continue;

                float distance = Vector2.Distance(
                    new Vector2(worldPos.x, worldPos.z),
                    new Vector2(point.worldPosition.x, point.worldPosition.z)
                );

                if (distance >= blendRadius) continue;

                float t = 1f - (distance / blendRadius);
                float influence = t * t * point.influence;

                if (influence > bestInfluence)
                {
                    bestInfluence = influence;
                    dominant = point.dominantBiome;
                }
            }

            return dominant;
        }

        // ─── Editor Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Agrega un nuevo punto de control en la posición indicada.
        /// Usado por el Custom Editor.
        /// </summary>
        public void AddControlPoint(Vector3 position, BiomeSettings biome)
        {
            controlPoints.Add(new BiomeControlPoint
            {
                pointId = $"point_{controlPoints.Count:D3}",
                worldPosition = position,
                dominantBiome = biome,
                influence = 1f
            });
        }

        /// <summary>
        /// Elimina un punto de control por índice.
        /// </summary>
        public void RemoveControlPoint(int index)
        {
            if (index >= 0 && index < controlPoints.Count)
                controlPoints.RemoveAt(index);
        }

        // ─── Gizmos ─────────────────────────────────────────────────────────────

        void OnDrawGizmos()
        {
            if (!showGizmos) return;

            foreach (var point in controlPoints)
            {
                if (point.dominantBiome == null) continue;

                Color biomeColor = GetBiomeGizmoColor(point.dominantBiome.category);

                // Esfera del punto
                Gizmos.color = biomeColor;
                Gizmos.DrawSphere(point.worldPosition, gizmoSphereRadius);

                // Wireframe del radio de influencia
                biomeColor.a = 0.15f;
                Gizmos.color = biomeColor;
                Gizmos.DrawWireSphere(point.worldPosition, blendRadius);
            }
        }

        private static Color GetBiomeGizmoColor(BiomeCategory category)
        {
            return category switch
            {
                BiomeCategory.Forest     => new Color(0.1f, 0.6f, 0.1f),
                BiomeCategory.Plains     => new Color(0.7f, 0.9f, 0.2f),
                BiomeCategory.Mountain   => new Color(0.6f, 0.5f, 0.4f),
                BiomeCategory.Arid       => new Color(0.9f, 0.8f, 0.2f),
                BiomeCategory.Coastal    => new Color(0.2f, 0.7f, 0.9f),
                BiomeCategory.Dark       => new Color(0.4f, 0.1f, 0.5f),
                BiomeCategory.Urban      => new Color(0.7f, 0.7f, 0.7f),
                BiomeCategory.Underground => new Color(0.3f, 0.2f, 0.1f),
                _                        => Color.white
            };
        }

        // ─── Context Menu Debug ──────────────────────────────────────────────────

        [ContextMenu("Debug: Samplear posición (0,0,0)")]
        private void DebugSampleOrigin()
        {
            var sample = GetBiomeAt(Vector3.zero);
            Debug.Log($"=== BiomeSample en (0,0,0) ===");
            if (sample.Influences.Count == 0)
            {
                Debug.Log("Sin biomas (no hay puntos de control cercanos)");
                return;
            }
            foreach (var (biome, weight) in sample.Influences)
                Debug.Log($"  {biome.biomeName}: {weight:P0}");
        }

        [ContextMenu("Debug: Listar todos los puntos")]
        private void DebugListPoints()
        {
            Debug.Log($"=== WorldBiomeMap: {controlPoints.Count} puntos de control ===");
            for (int i = 0; i < controlPoints.Count; i++)
            {
                var p = controlPoints[i];
                string biomeName = p.dominantBiome != null ? p.dominantBiome.biomeName : "NULL";
                Debug.Log($"  [{i}] {p.pointId} | {biomeName} | pos: {p.worldPosition} | influence: {p.influence}");
            }
        }
    }

    // ─── BiomeControlPoint ───────────────────────────────────────────────────────

    [System.Serializable]
    public class BiomeControlPoint
    {
        [Tooltip("Identificador opcional para organización")]
        public string pointId = "point_000";

        [Tooltip("Posición en el mundo donde este bioma domina (Y se ignora, solo X y Z importan)")]
        public Vector3 worldPosition;

        [Tooltip("El bioma que domina en este punto")]
        public BiomeSettings dominantBiome;

        [Range(0.1f, 2f)]
        [Tooltip("Multiplicador de intensidad. 1 = normal, >1 = domina más, <1 = influye menos")]
        public float influence = 1f;
    }
}
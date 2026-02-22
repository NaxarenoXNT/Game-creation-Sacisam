using System.Collections.Generic;
using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Configuración de un objeto con identidad propia dentro de un chunk.
    /// A diferencia de la decoración procedural, estos objetos tienen una posición fija,
    /// pueden tener estado persistente y se configuran manualmente en el ChunkDataAsset.
    ///
    /// Ejemplos: edificios, cofres, NPCs estáticos, entradas a cuevas.
    /// </summary>
    [System.Serializable]
    public class PropSpawnConfig
    {
        [Header("Identificación")]
        [Tooltip("ID único global. Convención: tipo_chunkX_chunkY_índice. Ej: cofre_3_7_01")]
        public string propId;

        [Tooltip("Definición del objeto (ScriptableObject).")]
        public PropData propData;

        [Header("Transform")]
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;

        [Header("Estado")]
        [Tooltip("Si está activo, este prop no se spawnea. " +
                 "Se activa cuando el jugador interactúa con un objeto consumible.")]
        public bool isConsumed = false;

        [Header("Interacción")]
        [Tooltip("Tipo de interacción. Usado por PropController para disparar la lógica correcta. " +
                 "Valores sugeridos: 'cofre', 'npc', 'puerta', 'consumible', 'carga_zona'")]
        public string interactionType = "";

        [Tooltip("Para interactionType 'carga_zona': nombre de la escena o zona a cargar.")]
        public string targetZone = "";

        // ─── Runtime (no serializado) ────────────────────────────────────────────
        [System.NonSerialized]
        public PropController activeController;
    }

    /// <summary>
    /// Define una zona dentro de un chunk donde la generación procedural no coloca nada.
    /// Se usa para caminos, plazas, footprints de edificios, clearings, etc.
    /// </summary>
    [System.Serializable]
    public class ProceduralExclusion
    {
        public ExclusionShape shape = ExclusionShape.Circle;

        [Header("Círculo")]
        [Tooltip("Centro de la exclusión circular (coordenadas de mundo).")]
        public Vector3 circleCenter;
        public float circleRadius = 20f;

        [Header("Rectángulo")]
        [Tooltip("Centro del rectángulo (coordenadas de mundo).")]
        public Vector3 rectCenter;
        public Vector3 rectSize = new Vector3(20f, 0f, 20f);
        [Tooltip("Rotación en grados sobre el eje Y.")]
        public float rectRotationY = 0f;

        [Header("Camino (Path)")]
        [Tooltip("Puntos que definen el camino. En coordenadas de mundo.")]
        public List<Vector3> pathPoints = new List<Vector3>();
        [Tooltip("Ancho del corredor libre a ambos lados del camino.")]
        public float pathWidth = 6f;

        /// <summary>
        /// Verifica si un punto está dentro de esta zona de exclusión.
        /// Solo usa X y Z (ignora Y).
        /// </summary>
        public bool Contains(Vector3 point)
        {
            return shape switch
            {
                ExclusionShape.Circle    => ContainsCircle(point),
                ExclusionShape.Rectangle => ContainsRect(point),
                ExclusionShape.Path      => ContainsPath(point),
                _                        => false
            };
        }

        private bool ContainsCircle(Vector3 point)
        {
            float dx = point.x - circleCenter.x;
            float dz = point.z - circleCenter.z;
            return (dx * dx + dz * dz) <= (circleRadius * circleRadius);
        }

        private bool ContainsRect(Vector3 point)
        {
            // Transformar el punto al espacio local del rectángulo
            float cos = Mathf.Cos(-rectRotationY * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-rectRotationY * Mathf.Deg2Rad);

            float localX = cos * (point.x - rectCenter.x) - sin * (point.z - rectCenter.z);
            float localZ = sin * (point.x - rectCenter.x) + cos * (point.z - rectCenter.z);

            return Mathf.Abs(localX) <= rectSize.x * 0.5f &&
                   Mathf.Abs(localZ) <= rectSize.z * 0.5f;
        }

        private bool ContainsPath(Vector3 point)
        {
            if (pathPoints == null || pathPoints.Count < 2) return false;

            float halfWidth = pathWidth * 0.5f;

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Vector2 a = new Vector2(pathPoints[i].x, pathPoints[i].z);
                Vector2 b = new Vector2(pathPoints[i + 1].x, pathPoints[i + 1].z);
                Vector2 p = new Vector2(point.x, point.z);

                if (DistancePointToSegment(p, a, b) <= halfWidth)
                    return true;
            }

            return false;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;

            if (lengthSq < 0.0001f) return Vector2.Distance(p, a);

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
            Vector2 projection = a + t * ab;
            return Vector2.Distance(p, projection);
        }
    }

    public enum ExclusionShape
    {
        Circle,
        Rectangle,
        Path
    }
}
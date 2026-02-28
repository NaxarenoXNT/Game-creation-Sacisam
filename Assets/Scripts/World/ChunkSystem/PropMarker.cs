using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Componente marcador para props colocados visualmente en la escena.
    /// Agregá este componente a cualquier GameObject que quieras "bakear" 
    /// en el ChunkDataAsset correspondiente.
    /// 
    /// Flujo:
    /// 1. Arrastrá un prefab a la escena y posicionalo donde quieras.
    /// 2. Agregale este componente.
    /// 3. Asignale el PropData (ScriptableObject que define el tipo de prop).
    /// 4. En Tools → Generador de Mundo PRO → "Bakear Props", 
    ///    el sistema lee la posición, rotación y escala del transform
    ///    y los guarda en el ChunkDataAsset correcto.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("World/Prop Marker")]
    public class PropMarker : MonoBehaviour
    {
        [Header("Configuración del Prop")]
        [Tooltip("ScriptableObject que define el tipo de prop (prefab, categoría, interacción).")]
        public PropData propData;

        [Header("Interacción (Opcional)")]
        [Tooltip("Tipo de interacción: 'cofre', 'npc', 'puerta', 'consumible', 'carga_zona'. " +
                 "Dejar vacío si el prop es puramente decorativo.")]
        public string interactionType = "";

        [Tooltip("Para interactionType 'carga_zona': nombre de la escena o zona a cargar.")]
        public string targetZone = "";

        [Header("Estado")]
        [Tooltip("Indica si este prop ya fue bakeado. Se marca automáticamente al bakear.")]
        [HideInInspector]
        public bool isBaked = false;

        /// <summary>
        /// Genera el PropSpawnConfig correspondiente a este marker.
        /// Usa las coordenadas del chunk para crear un ID único.
        /// </summary>
        public PropSpawnConfig ToSpawnConfig(Vector2Int chunkCoords, int index)
        {
            string category = propData != null ? propData.category.ToString().ToLower() : "prop";
            
            return new PropSpawnConfig
            {
                propId = $"{category}_{chunkCoords.x}_{chunkCoords.y}_{index:D2}",
                propData = propData,
                position = transform.position,
                rotation = transform.rotation,
                scale = transform.localScale,
                interactionType = interactionType,
                targetZone = targetZone,
                isConsumed = false
            };
        }

        /// <summary>
        /// Dibuja un icono en la Scene View para identificar props marcados.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (propData == null)
            {
                // Sin PropData → rojo de advertencia
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
                Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, Vector3.one * 0.8f);
                return;
            }

            // Color según categoría
            Gizmos.color = propData.category switch
            {
                PropCategory.Structure   => new Color(0.8f, 0.5f, 0.2f, 0.7f), // Naranja
                PropCategory.Interactive => new Color(1f, 0.9f, 0.1f, 0.7f),   // Amarillo
                PropCategory.NPC         => new Color(0.3f, 0.8f, 1f, 0.7f),   // Cyan
                PropCategory.ZoneEntry   => new Color(0.8f, 0.3f, 1f, 0.7f),   // Violeta
                PropCategory.Resource    => new Color(0.2f, 1f, 0.4f, 0.7f),   // Verde
                _                        => new Color(0.6f, 0.6f, 0.6f, 0.5f)  // Gris decoración
            };

            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.6f);

            if (isBaked)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Mostrar label con info
            string label = propData != null ? propData.propName : "⚠️ SIN PROP DATA";
            if (!string.IsNullOrEmpty(interactionType))
                label += $" [{interactionType}]";
            if (isBaked)
                label += " ✓BAKED";

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.5f, 
                label, 
                new GUIStyle(GUI.skin.label) 
                { 
                    alignment = TextAnchor.MiddleCenter, 
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                }
            );
        }
#endif
    }
}

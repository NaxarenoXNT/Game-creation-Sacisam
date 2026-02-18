using System;
using System.Collections.Generic;
using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Configuración estática de un enemigo en un chunk.
    /// NO guarda estado dinámico (HP, buffs), solo configuración de spawn e IA.
    /// </summary>
    [Serializable]
    public class EnemySpawnConfig
    {
        [Header("Identificación")]
        [Tooltip("ID único del spawn en el chunk (para tracking si es necesario)")]
        public string spawnId;
        
        [Header("Tipo de Enemigo")]
        [Tooltip("Datos del enemigo a spawnear")]
        public EnemigoData enemyData;
        
        [Header("Posición y Orientación")]
        [Tooltip("Posición de spawn en el mundo")]
        public Vector3 spawnPosition;
        
        [Tooltip("Rotación inicial")]
        public Quaternion spawnRotation = Quaternion.identity;
        
        /// <summary>
        /// Valida y corrige el quaternion si es inválido.
        /// </summary>
        public void ValidateRotation()
        {
            // Verificar si el quaternion es inválido
            float magnitude = Mathf.Sqrt(spawnRotation.x * spawnRotation.x + spawnRotation.y * spawnRotation.y + 
                                        spawnRotation.z * spawnRotation.z + spawnRotation.w * spawnRotation.w);
            
            if (float.IsNaN(spawnRotation.x) || float.IsNaN(spawnRotation.y) || 
                float.IsNaN(spawnRotation.z) || float.IsNaN(spawnRotation.w) ||
                spawnRotation == new Quaternion(0, 0, 0, 0) ||
                magnitude < 0.9f)
            {
                spawnRotation = Quaternion.identity;
            }
        }
        
        [Header("IA y Comportamiento")]
        [Tooltip("Estado inicial de IA")]
        public EnemyAIState initialAIState = EnemyAIState.Idle;
        
        [Tooltip("Waypoints para patrullaje (posiciones en mundo)")]
        public List<Vector3> patrolWaypoints = new List<Vector3>();
        
        [Tooltip("Comportamiento de patrulla")]
        public PatrolBehavior patrolBehavior = PatrolBehavior.Loop;
        
        [Tooltip("Velocidad de patrulla (0 = usar default del enemigo)")]
        public float patrolSpeed = 0f;
        
        [Tooltip("Tiempo de espera en cada waypoint (segundos)")]
        public float waypointWaitTime = 2f;
        
        [Header("Detección")]
        [Tooltip("Radio de detección del jugador (0 = usar default)")]
        public float detectionRadius = 0f;
        
        [Tooltip("Radio de persecución (0 = usar default)")]
        public float chaseRadius = 0f;
        
        [Header("Configuración Especial")]
        [Tooltip("Enemigo único que no respawnea si muere")]
        public bool isUnique = false;
        
        [Tooltip("ID único para enemigos especiales (bosses, NPCs nombrados)")]
        public string uniqueId;
        
        [Tooltip("Tags personalizados para lógica custom")]
        public List<string> customTags = new List<string>();
        
        [Header("Datos Personalizados")]
        [Tooltip("Datos extra en formato key-value para lógica custom")]
        public List<CustomDataEntry> customData = new List<CustomDataEntry>();
        
        // Estado runtime (no serializado)
        [NonSerialized] public bool isDefeated; // Para enemigos únicos
        [NonSerialized] public bool isDefeatedThisSession; // Para enemigos normales (se resetea al reiniciar)
        [NonSerialized] public int currentWaypointIndex; // Para continuar patrulla
        [NonSerialized] public EnemyController activeController; // Referencia al controller activo (si existe)
        
        /// <summary>
        /// Obtiene un valor de datos personalizados.
        /// </summary>
        public string GetCustomData(string key)
        {
            var entry = customData.Find(e => e.key == key);
            return entry.value ?? string.Empty;
        }
        
        /// <summary>
        /// Verifica si tiene un tag personalizado.
        /// </summary>
        public bool HasTag(string tag)
        {
            return customTags.Contains(tag);
        }
        
        /// <summary>
        /// Clona la configuración (útil para instanciar múltiples enemigos).
        /// </summary>
        public EnemySpawnConfig Clone()
        {
            return new EnemySpawnConfig
            {
                spawnId = spawnId,
                enemyData = enemyData,
                spawnPosition = spawnPosition,
                spawnRotation = spawnRotation,
                initialAIState = initialAIState,
                patrolWaypoints = new List<Vector3>(patrolWaypoints),
                patrolBehavior = patrolBehavior,
                patrolSpeed = patrolSpeed,
                waypointWaitTime = waypointWaitTime,
                detectionRadius = detectionRadius,
                chaseRadius = chaseRadius,
                isUnique = isUnique,
                uniqueId = uniqueId,
                customTags = new List<string>(customTags),
                customData = new List<CustomDataEntry>(customData)
            };
        }
        
        /// <summary>
        /// Resetea el estado de la sesión (llamar al reiniciar el juego).
        /// </summary>
        public void ResetSessionState()
        {
            isDefeatedThisSession = false;
            activeController = null;
            currentWaypointIndex = 0;
        }
    }
    
    /// <summary>
    /// Estados de IA para enemigos.
    /// </summary>
    public enum EnemyAIState
    {
        Idle,           // En reposo, parado
        Patrolling,     // Patrullando waypoints
        Resting,        // Descansando (sentado, durmiendo)
        Alerted,        // Alerta pero sin target
        Chasing,        // Persiguiendo al jugador
        Returning,      // Volviendo a posición inicial
        Custom          // Estado personalizado
    }
    
    /// <summary>
    /// Comportamiento de patrulla.
    /// </summary>
    public enum PatrolBehavior
    {
        Loop,           // Ciclo continuo (1→2→3→1)
        PingPong,       // Ida y vuelta (1→2→3→2→1)
        Random,         // Elige waypoints aleatorios
        Once            // Una vez y se detiene
    }
    
    /// <summary>
    /// Entrada de datos personalizados key-value.
    /// </summary>
    [Serializable]
    public struct CustomDataEntry
    {
        public string key;
        public string value;
    }
}

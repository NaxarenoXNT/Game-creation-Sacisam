using UnityEngine;
using System.Collections.Generic;
using Flags;

namespace Managers
{
    /// <summary>
    /// ScriptableObject que define las reglas y configuración para encuentros de combate.
    /// Permite ajustar parámetros sin tocar código.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatRules", menuName = "Combate/Combat Rules")]
    public class CombatRules : ScriptableObject
    {
        [Header("Detección")]
        [Tooltip("Radio en el que se detectan enemigos potenciales")]
        [Range(5f, 100f)]
        public float detectionRadius = 20f;
        
        [Tooltip("Radio mínimo para que un enemigo pueda iniciar combate")]
        [Range(1f, 50f)]
        public float engagementRadius = 10f;
        
        [Tooltip("Layers que contienen enemigos")]
        public LayerMask enemyLayers;
        
        [Header("Límites de Combate")]
        [Tooltip("Máximo de enemigos que pueden entrar en un combate (0 = sin límite)")]
        [Range(0, 20)]
        public int maxEnemiesPerEncounter = 5;
        
        [Tooltip("Mínimo de aliados necesarios para iniciar combate")]
        [Range(1, 10)]
        public int minAlliesRequired = 1;
        
        [Tooltip("Máximo de aliados en combate (0 = sin límite)")]
        [Range(0, 10)]
        public int maxAlliesPerEncounter = 4;
        
        [Header("Condiciones de Inicio")]
        [Tooltip("Si true, el combate inicia automáticamente al cumplir condiciones")]
        public bool autoStartCombat = true;
        
        [Tooltip("Tiempo mínimo entre encuentros (segundos)")]
        [Range(0f, 60f)]
        public float encounterCooldown = 5f;
        
        [Tooltip("Diferencia máxima de nivel permitida (0 = sin restricción)")]
        [Range(0, 50)]
        public int maxLevelDifference = 10;
        
        [Header("Priorización")]
        [Tooltip("Cómo ordenar enemigos cuando hay más del máximo")]
        public EnemyPrioritization prioritization = EnemyPrioritization.ByDistance;
        
        [Tooltip("Si true, enemigos con aggro tienen prioridad")]
        public bool prioritizeAggro = true;
        
        [Header("Condiciones Especiales")]
        [Tooltip("Tipos de entidad que pueden iniciar combate")]
        public List<TipoEntidades> tiposPermitidos = new List<TipoEntidades>();
        
        [Tooltip("Si true, requiere línea de visión para entrar en combate")]
        public bool requireLineOfSight = false;
        
        [Tooltip("Layer de obstáculos para línea de visión")]
        public LayerMask lineOfSightBlockers;
        
        [Header("Debug")]
        public bool showDebugGizmos = true;
        public Color detectionColor = new Color(1f, 1f, 0f, 0.3f);
        public Color engagementColor = new Color(1f, 0f, 0f, 0.3f);
    }
    
    /// <summary>
    /// Métodos de priorización cuando hay demasiados enemigos.
    /// </summary>
    public enum EnemyPrioritization
    {
        /// <summary>Los más cercanos primero.</summary>
        ByDistance,
        
        /// <summary>Los de mayor nivel primero.</summary>
        ByLevel,
        
        /// <summary>Los de menor nivel primero.</summary>
        ByLevelAscending,
        
        /// <summary>Por prioridad definida en cada enemigo.</summary>
        ByPriority,
        
        /// <summary>Aleatorio.</summary>
        Random
    }
}

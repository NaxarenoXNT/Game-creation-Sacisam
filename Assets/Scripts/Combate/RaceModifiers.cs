using System;
using System.Collections.Generic;
using Flags;
using UnityEngine;

namespace Combate
{
    /// <summary>
    /// ScriptableObject que define los multiplicadores de daño entre razas/tipos de entidad.
    /// </summary>
    [CreateAssetMenu(fileName = "RaceModifiers", menuName = "Combate/Race Modifiers")]
    public class RaceModifiers : ScriptableObject
    {
        [Header("Configuración Global")]
        [Tooltip("Multiplicador por defecto cuando no hay relación especial")]
        public float defaultMultiplier = 1.0f;
        
        [Header("Modificadores de Ataque por Raza")]
        [Tooltip("Multiplicadores de ataque según el tipo de atacante")]
        public List<RaceAttackModifier> attackModifiers = new List<RaceAttackModifier>();
        
        [Header("Modificadores de Defensa por Raza")]
        [Tooltip("Multiplicadores de defensa según el tipo de defensor")]
        public List<RaceDefenseModifier> defenseModifiers = new List<RaceDefenseModifier>();
        
        [Header("Relaciones entre Razas")]
        [Tooltip("Modificadores cuando una raza ataca a otra específica")]
        public List<RaceVsRaceModifier> raceVsRaceModifiers = new List<RaceVsRaceModifier>();
        
        /// <summary>
        /// Obtiene el multiplicador de ataque para un tipo de entidad atacante.
        /// </summary>
        public float GetAttackMultiplier(TipoEntidades attackerType)
        {
            foreach (var mod in attackModifiers)
            {
                if ((mod.entityType & attackerType) != 0)
                {
                    return mod.attackMultiplier;
                }
            }
            return defaultMultiplier;
        }
        
        /// <summary>
        /// Obtiene el multiplicador de defensa para un tipo de entidad defensora.
        /// </summary>
        public float GetDefenseMultiplier(TipoEntidades defenderType)
        {
            foreach (var mod in defenseModifiers)
            {
                if ((mod.entityType & defenderType) != 0)
                {
                    return mod.defenseMultiplier;
                }
            }
            return defaultMultiplier;
        }
        
        /// <summary>
        /// Obtiene el multiplicador especial cuando una raza ataca a otra.
        /// </summary>
        public float GetRaceVsRaceMultiplier(TipoEntidades attackerType, TipoEntidades defenderType)
        {
            foreach (var mod in raceVsRaceModifiers)
            {
                if ((mod.attackerType & attackerType) != 0 && (mod.defenderType & defenderType) != 0)
                {
                    return mod.damageMultiplier;
                }
            }
            return defaultMultiplier;
        }
    }
    
    /// <summary>
    /// Modificador de ataque base por tipo de entidad.
    /// </summary>
    [System.Serializable]
    public class RaceAttackModifier
    {
        [Tooltip("Tipo de entidad")]
        public TipoEntidades entityType;
        
        [Tooltip("Multiplicador de ataque (1.0 = normal, 1.2 = +20%)")]
        [Range(0.5f, 2.0f)]
        public float attackMultiplier = 1.0f;
        
        [TextArea(1, 2)]
        public string descripcion;
    }
    
    /// <summary>
    /// Modificador de defensa base por tipo de entidad.
    /// </summary>
    [System.Serializable]
    public class RaceDefenseModifier
    {
        [Tooltip("Tipo de entidad")]
        public TipoEntidades entityType;
        
        [Tooltip("Multiplicador de defensa (1.0 = normal, 1.2 = +20% efectividad)")]
        [Range(0.5f, 2.0f)]
        public float defenseMultiplier = 1.0f;
        
        [TextArea(1, 2)]
        public string descripcion;
    }
    
    /// <summary>
    /// Modificador de daño cuando una raza específica ataca a otra.
    /// Ej: Humanoid vs Undead = 0.8x (menos efectivo)
    /// </summary>
    [System.Serializable]
    public class RaceVsRaceModifier
    {
        [Tooltip("Tipo de entidad atacante")]
        public TipoEntidades attackerType;
        
        [Tooltip("Tipo de entidad defensora")]
        public TipoEntidades defenderType;
        
        [Tooltip("Multiplicador de daño")]
        [Range(0.25f, 3.0f)]
        public float damageMultiplier = 1.0f;
        
        [TextArea(1, 2)]
        public string descripcion;
    }
}

using System;
using Flags;
using Interfaces;
using UnityEngine;

namespace Combate
{
    /// <summary>
    /// Calculadora central de daño del juego.
    /// Implementa la fórmula:
    /// 
    /// BASE_OFFENSE = (ATK + ELEM_ATK) * RACE_ATK
    /// OFFENSE = BASE_OFFENSE * (isCrit ? CRIT_MULT : 1)
    /// DEF_MULT = 1 / (1 + ln(1 + DEF * RACE_DEF) / K)
    /// PHYSICAL_DAMAGE = OFFENSE * DEF_MULT
    /// ELEM_MULT = clamp(1 - RES_e, 0.1, 1.5)
    /// ELEMENTAL_DAMAGE = ELEM_ATK * ELEM_MULT * (critAppliesToElemental && isCrit ? CRIT_MULT : 1)
    /// FINAL_DAMAGE = PHYSICAL_DAMAGE + ELEMENTAL_DAMAGE
    /// </summary>
    public static class DamageCalculator
    {
        // Constante K para la fórmula de defensa (ajustada para stats en miles)
        // K más bajo = defensa más efectiva
        // K=22 da: DEF=1000 → ~24% mitigación, DEF=5000 → ~42%, DEF=10000 → ~47%
        private const float DEFAULT_K = 22f;
        
        // Límites para el multiplicador elemental
        private const float ELEM_MULT_MIN = 0.1f;   // Mínimo 10% del daño elemental
        private const float ELEM_MULT_MAX = 1.5f;   // Máximo 150% del daño elemental
        
        // Daño mínimo garantizado
        private const int MIN_DAMAGE = 1;
        
        /// <summary>
        /// Calcula el daño final usando la fórmula completa.
        /// </summary>
        /// <param name="attacker">Estadísticas del atacante</param>
        /// <param name="defender">Estadísticas del defensor</param>
        /// <param name="raceModifiers">Modificadores de raza (opcional)</param>
        /// <param name="k">Constante K para la fórmula de defensa</param>
        /// <returns>Resultado detallado del daño</returns>
        public static DamageResult CalculateDamage(
            AttackerData attacker,
            DefenderData defender,
            RaceModifiers raceModifiers = null,
            float k = DEFAULT_K)
        {
            var result = new DamageResult();
            
            // === 1. Obtener multiplicadores de raza ===
            float raceAtk = 1f;
            float raceDef = 1f;
            float raceVsRace = 1f;
            
            if (raceModifiers != null)
            {
                raceAtk = raceModifiers.GetAttackMultiplier(attacker.entityType);
                raceDef = raceModifiers.GetDefenseMultiplier(defender.entityType);
                raceVsRace = raceModifiers.GetRaceVsRaceMultiplier(attacker.entityType, defender.entityType);
            }
            
            result.raceAtkMultiplier = raceAtk * raceVsRace;
            result.raceDefMultiplier = raceDef;
            
            // === 2. Calcular BASE_OFFENSE ===
            float baseOffense = (attacker.attack + attacker.elementalAttack) * result.raceAtkMultiplier;
            
            // === 3. Determinar crítico ===
            result.isCritical = UnityEngine.Random.value <= attacker.critChance;
            float critMult = result.isCritical ? attacker.critMultiplier : 1f;
            
            // === 4. Calcular OFFENSE con crítico ===
            float offense = baseOffense * critMult;
            
            // === 5. Calcular DEF_MULT (fórmula logarítmica) ===
            float effectiveDefense = defender.defense * result.raceDefMultiplier;
            float defMult = CalculateDefenseMultiplier(effectiveDefense, k);
            result.defenseMultiplier = defMult;
            
            // === 6. Calcular PHYSICAL_DAMAGE ===
            float physicalDamage = offense * defMult;
            result.physicalDamage = Mathf.RoundToInt(physicalDamage);
            
            // === 7. Calcular ELEMENTAL_DAMAGE ===
            float elementalDamage = 0f;
            
            if (attacker.elementalAttack > 0 && attacker.attackElement != ElementAttribute.None)
            {
                // Obtener resistencia del defensor al elemento del ataque
                float resistance = defender.resistances?.GetResistance(attacker.attackElement) ?? 0f;
                
                // ELEM_MULT = clamp(1 - RES_e, 0.1, 1.5)
                float elemMult = Mathf.Clamp(1f - resistance, ELEM_MULT_MIN, ELEM_MULT_MAX);
                result.elementalMultiplier = elemMult;
                
                // Aplicar crítico al elemental si está habilitado
                float elemCritMult = (attacker.critAppliesToElemental && result.isCritical) 
                    ? attacker.critMultiplier 
                    : 1f;
                
                elementalDamage = attacker.elementalAttack * elemMult * elemCritMult;
            }
            
            result.elementalDamage = Mathf.RoundToInt(elementalDamage);
            
            // === 8. FINAL_DAMAGE ===
            result.finalDamage = Mathf.Max(MIN_DAMAGE, result.physicalDamage + result.elementalDamage);
            
            return result;
        }
        
        /// <summary>
        /// Calcula el multiplicador de defensa usando la fórmula logarítmica.
        /// DEF_MULT = 1 / (1 + ln(1 + DEF) / K)
        /// </summary>
        /// <param name="defense">Defensa efectiva</param>
        /// <param name="k">Constante K</param>
        /// <returns>Multiplicador de daño (0 a 1)</returns>
        public static float CalculateDefenseMultiplier(float defense, float k = DEFAULT_K)
        {
            if (defense <= 0) return 1f;
            if (k <= 0) k = DEFAULT_K;
            
            // DEF_MULT = 1 / (1 + ln(1 + DEF) / K)
            float lnValue = Mathf.Log(1f + defense);
            float defMult = 1f / (1f + lnValue / k);
            
            return Mathf.Clamp01(defMult);
        }
        
        /// <summary>
        /// Calcula el porcentaje de mitigación para mostrar en UI.
        /// </summary>
        public static float CalculateMitigationPercent(float defense, float k = DEFAULT_K)
        {
            return (1f - CalculateDefenseMultiplier(defense, k)) * 100f;
        }
        
        /// <summary>
        /// Versión simplificada para cálculos rápidos sin objetos.
        /// </summary>
        public static int CalculateSimpleDamage(
            int attack, 
            float defense, 
            float critChance = 0f, 
            float critMult = 1.5f,
            float k = DEFAULT_K)
        {
            bool isCrit = UnityEngine.Random.value <= critChance;
            float offense = attack * (isCrit ? critMult : 1f);
            float defMult = CalculateDefenseMultiplier(defense, k);
            
            return Mathf.Max(MIN_DAMAGE, Mathf.RoundToInt(offense * defMult));
        }
        
        /// <summary>
        /// Calcula daño desde interfaces IEntidadCombate.
        /// </summary>
        public static DamageResult CalculateFromEntities(
            IEntidadCombate attacker,
            CombatStats attackerStats,
            IEntidadCombate defender,
            CombatStats defenderStats,
            RaceModifiers raceModifiers = null,
            float k = DEFAULT_K)
        {
            var attackerData = new AttackerData
            {
                attack = attacker.PuntosDeAtaque_Entidad,
                elementalAttack = attackerStats?.elementalAttack ?? 0,
                attackElement = attackerStats?.elementoAtaque ?? ElementAttribute.None,
                critChance = attackerStats?.critChance ?? 0f,
                critMultiplier = attackerStats?.critMultiplier ?? 1.5f,
                critAppliesToElemental = attackerStats?.critAppliesToElemental ?? false,
                entityType = attacker.TipoEntidad
            };
            
            var defenderData = new DefenderData
            {
                defense = defender.PuntosDeDefensa_Entidad,
                resistances = defenderStats?.resistencias,
                entityType = defender.TipoEntidad
            };
            
            return CalculateDamage(attackerData, defenderData, raceModifiers, k);
        }
    }
    
    /// <summary>
    /// Datos del atacante para el cálculo de daño.
    /// </summary>
    public struct AttackerData
    {
        public int attack;
        public int elementalAttack;
        public ElementAttribute attackElement;
        public float critChance;
        public float critMultiplier;
        public bool critAppliesToElemental;
        public TipoEntidades entityType;
        
        public AttackerData(int atk, TipoEntidades type)
        {
            attack = atk;
            elementalAttack = 0;
            attackElement = ElementAttribute.None;
            critChance = 0.05f;
            critMultiplier = 1.5f;
            critAppliesToElemental = false;
            entityType = type;
        }
    }
    
    /// <summary>
    /// Datos del defensor para el cálculo de daño.
    /// </summary>
    public struct DefenderData
    {
        public float defense;
        public ElementalResistances resistances;
        public TipoEntidades entityType;
        
        public DefenderData(float def, TipoEntidades type)
        {
            defense = def;
            resistances = null;
            entityType = type;
        }
    }
}

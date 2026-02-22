using System;
using Flags;
using Interfaces;
using UnityEngine;

namespace Combate
{
    /// <summary>
    /// Fachada estática para el sistema de daño.
    /// Delega al DamagePipeline para todos los cálculos.
    ///
    /// Los métodos CalculateDamage / CalculateFromEntities se mantienen
    /// por backward-compatibility, pero internamente usan el pipeline.
    ///
    /// Para código nuevo, preferir usar DamagePipeline directamente:
    ///   var ctx = DamagePipeline.CreateContext(attacker, defender, isCrit);
    ///   DamagePipeline.Default.Execute(ctx);
    ///   objetivo.AplicarDanoDesdeContexto(ctx);
    /// </summary>
    public static class DamageCalculator
    {
        private const float DEFAULT_K = 22f;
        
        /// <summary>
        /// Calcula daño usando datos estructurados (backward-compatible).
        /// NOTA: Este método mantiene su propia lógica para no romper callers existentes
        /// que pasan AttackerData/DefenderData sin IEntidadCombate.
        /// Para entidades reales, usar CalculateFromEntities o el pipeline directo.
        /// </summary>
        public static DamageResult CalculateDamage(
            AttackerData attacker,
            DefenderData defender,
            RaceModifiers raceModifiers = null,
            float k = DEFAULT_K)
        {
            // Crear contexto con valores base pre-configurados
            var context = new DamageContext
            {
                // Sin IEntidadCombate — populamos canales manualmente
                HasBaseValues          = true,
                PhysicalDamage         = attacker.attack,
                ElementalDamage        = attacker.elementalAttack,
                AttackElement          = attacker.attackElement,
                CritMultiplier         = attacker.critMultiplier,
                CritAppliesToElemental = attacker.critAppliesToElemental,
                DefenseConstantK       = k,
                RaceModifiers          = raceModifiers,
            };

            // Decidir crit (el pipeline NO lo decide)
            context.IsCritical = UnityEngine.Random.value <= attacker.critChance;

            // Como no tenemos IEntidadCombate, debemos simular las etapas manualmente:
            // Race
            if (raceModifiers != null)
            {
                float raceAtk = raceModifiers.GetAttackMultiplier(attacker.entityType);
                float raceVsRace = raceModifiers.GetRaceVsRaceMultiplier(attacker.entityType, defender.entityType);
                float raceDef = raceModifiers.GetDefenseMultiplier(defender.entityType);

                context.RaceAtkMultiplier = raceAtk * raceVsRace;
                context.RaceDefMultiplier = raceDef;

                context.PhysicalDamage  *= context.RaceAtkMultiplier;
                context.ElementalDamage *= context.RaceAtkMultiplier;
            }

            // Crit
            if (context.IsCritical)
            {
                float critMult = context.CritMultiplier > 1f ? context.CritMultiplier : 1.5f;
                context.PhysicalDamage *= critMult;
                if (attacker.critAppliesToElemental)
                    context.ElementalDamage *= critMult;
            }

            // Defense
            float effectiveDefense = defender.defense * context.RaceDefMultiplier;
            float defMult = CalculateDefenseMultiplier(effectiveDefense, k);
            context.DefenseMultiplier = defMult;
            context.PhysicalDamage *= defMult;

            // Elemental resistance
            if (attacker.elementalAttack > 0 && attacker.attackElement != ElementAttribute.None)
            {
                float resistance = defender.resistances?.GetResistance(attacker.attackElement) ?? 0f;
                float elemMult = Mathf.Clamp(1f - resistance, 0.1f, 1.5f);
                context.ElementalMultiplier = elemMult;
                context.ElementalDamage *= elemMult;
            }

            // Clamp
            if (context.PhysicalDamage < 0f)  context.PhysicalDamage  = 0f;
            if (context.ElementalDamage < 0f) context.ElementalDamage = 0f;
            context.FinalDamage = Mathf.Max(1, Mathf.RoundToInt(context.TotalRawDamage));

            return context.ToResult();
        }
        
        /// <summary>
        /// Fórmula de defensa hiperbólica: DEF_MULT = 1 / (1 + DEF / K).
        /// Utility público para UI y queries.
        /// </summary>
        public static float CalculateDefenseMultiplier(float defense, float k = DEFAULT_K)
        {
            if (defense <= 0) return 1f;
            if (k <= 0) k = DEFAULT_K;
            return Mathf.Clamp01(1f / (1f + defense / k));
        }
        
        /// <summary>
        /// Calcula el porcentaje de mitigación para mostrar en UI.
        /// </summary>
        public static float CalculateMitigationPercent(float defense, float k = DEFAULT_K)
        {
            return (1f - CalculateDefenseMultiplier(defense, k)) * 100f;
        }
        
        /// <summary>
        /// Versión simplificada para cálculos rápidos sin pipeline (UI previews, tooltips).
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
            
            return Mathf.Max(1, Mathf.RoundToInt(offense * defMult));
        }
        
        /// <summary>
        /// Calcula daño entre dos IEntidadCombate usando el DamagePipeline completo.
        /// Este es el método preferido para código nuevo.
        /// </summary>
        public static DamageResult CalculateFromEntities(
            IEntidadCombate attacker,
            IEntidadCombate defender,
            bool isCritical = false)
        {
            var context = DamagePipeline.CreateContext(attacker, defender, isCritical);
            DamagePipeline.Default.Execute(context);
            return context.ToResult();
        }

        /// <summary>
        /// Ejecuta el pipeline completo y retorna el DamageContext para acceso detallado.
        /// </summary>
        public static DamageContext ExecutePipeline(
            IEntidadCombate attacker,
            IEntidadCombate defender,
            bool isCritical = false)
        {
            var context = DamagePipeline.CreateContext(attacker, defender, isCritical);
            return DamagePipeline.Default.Execute(context);
        }
    }
    
    /// <summary>
    /// Datos del atacante para el cálculo de daño (backward-compat).
    /// Para código nuevo, usar DamagePipeline.CreateContext con IEntidadCombate.
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
    /// Datos del defensor para el cálculo de daño (backward-compat).
    /// Para código nuevo, usar DamagePipeline.CreateContext con IEntidadCombate.
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

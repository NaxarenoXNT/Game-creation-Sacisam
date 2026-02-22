using Flags;
using Interfaces;
using UnityEngine;

namespace Combate
{
    /// <summary>
    /// Contenedor mutable que representa el estado de un cálculo de daño en curso.
    /// Es el objeto central que atraviesa el DamagePipeline y sobre el que actúan
    /// los IDamageModifier y los IEffectModifier.
    /// </summary>
    public class DamageContext
    {
        // === Participantes ===
        public IEntidadCombate Attacker { get; set; }
        public IEntidadCombate Defender { get; set; }

        // === Canales de daño (mutables por el pipeline) ===
        public float PhysicalDamage  { get; set; }
        public float ElementalDamage { get; set; }

        // === Elemento del ataque ===
        public ElementAttribute AttackElement { get; set; }

        // === Flags de estado del cálculo ===
        public bool IsCritical             { get; set; }
        public bool IgnoreDefense          { get; set; }
        public bool IsTrueDamage           { get; set; }   // bypassa todo el pipeline de mitigación
        public bool CritAppliesToElemental { get; set; }

        /// <summary>
        /// Si es true, el pipeline no sobreescribe PhysicalDamage/ElementalDamage
        /// en BaseDamageModifier (permite que el caller pre-configure los valores base).
        /// </summary>
        public bool HasBaseValues { get; set; }

        // === Configuración del pipeline (poblada por el caller) ===
        public float CritMultiplier    { get; set; } = 1.5f;
        public float DefenseConstantK  { get; set; } = 22f;
        public RaceModifiers RaceModifiers { get; set; }

        // === Multiplicadores cacheados para reporting / UI ===
        public float RaceAtkMultiplier   { get; set; } = 1f;
        public float RaceDefMultiplier   { get; set; } = 1f;
        public float DefenseMultiplier   { get; set; } = 1f;
        public float ElementalMultiplier { get; set; } = 1f;

        // === Resultado acumulado (escrito al final del pipeline) ===
        public int FinalDamage { get; set; }

        // -------------------------------------------------------

        public DamageContext() { }

        public DamageContext(
            IEntidadCombate attacker,
            IEntidadCombate defender,
            float physicalDamage,
            float elementalDamage,
            ElementAttribute element,
            bool isCritical = false)
        {
            Attacker        = attacker;
            Defender        = defender;
            PhysicalDamage  = physicalDamage;
            ElementalDamage = elementalDamage;
            AttackElement   = element;
            IsCritical      = isCritical;
            HasBaseValues   = physicalDamage > 0f || elementalDamage > 0f;
        }

        /// <summary>
        /// Daño total bruto antes de aplicar el clamp final.
        /// </summary>
        public float TotalRawDamage => PhysicalDamage + ElementalDamage;

        /// <summary>
        /// Convierte el contexto procesado a un DamageResult para backward compatibility.
        /// </summary>
        public DamageResult ToResult()
        {
            return new DamageResult
            {
                finalDamage          = FinalDamage,
                physicalDamage       = Mathf.RoundToInt(PhysicalDamage),
                elementalDamage      = Mathf.RoundToInt(ElementalDamage),
                isCritical           = IsCritical,
                defenseMultiplier    = DefenseMultiplier,
                elementalMultiplier  = ElementalMultiplier,
                raceAtkMultiplier    = RaceAtkMultiplier,
                raceDefMultiplier    = RaceDefMultiplier,
            };
        }
    }
}

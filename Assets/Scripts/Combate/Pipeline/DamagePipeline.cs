using System.Collections.Generic;
using Combate.Modifiers;
using Padres;
using UnityEngine;

namespace Combate
{
    /// <summary>
    /// Orquestador del sistema de daño. Ejecuta IDamageModifiers en orden,
    /// integra entity modifiers (pasivas/traits) y effect handlers,
    /// y produce el FinalDamage en el DamageContext.
    ///
    /// Orden de ejecución:
    ///   1. Pipeline modifiers globales (Order &lt; 1000)
    ///   2. Entity damage modifiers del atacante (pasivas, traits)
    ///   3. Entity damage modifiers del defensor
    ///   4. EffectHandler del atacante (IEffectModifier.Modify)
    ///   5. EffectHandler del defensor
    ///   6. Pipeline modifiers finales (Order ≥ 1000): FinalClamp
    /// </summary>
    public class DamagePipeline
    {
        private static DamagePipeline _default;

        /// <summary>Pipeline singleton con los modificadores base del juego.</summary>
        public static DamagePipeline Default => _default ??= CreateDefault();

        private readonly List<IDamageModifier> _modifiers;

        public DamagePipeline(List<IDamageModifier> modifiers)
        {
            _modifiers = new List<IDamageModifier>(modifiers);
            _modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        /// <summary>
        /// Crea el pipeline estándar con las etapas base del juego.
        /// </summary>
        public static DamagePipeline CreateDefault()
        {
            return new DamagePipeline(new List<IDamageModifier>
            {
                new BaseDamageModifier(),
                new RaceDamageModifier(),
                new CritDamageModifier(),
                new DefenseDamageModifier(),
                new ElementalResistanceDamageModifier(),
                new FinalClampModifier()
            });
        }

        /// <summary>
        /// Ejecuta el pipeline completo sobre el contexto dado.
        /// Al terminar, context.FinalDamage contiene el resultado.
        /// </summary>
        public DamageContext Execute(DamageContext context)
        {
            // 1. Etapas globales pre-effect (Order < 1000)
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Order >= 1000) break;
                _modifiers[i].Modify(context);
            }

            // 2. Entity damage modifiers (de pasivas, traits, etc.)
            ApplyEntityModifiers(context);

            // 3. Effect handlers (IEffectModifier.Modify de efectos activos)
            ApplyEffectHandlers(context);

            // 4. Etapas finales (Order ≥ 1000, e.g. FinalClamp)
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Order < 1000) continue;
                _modifiers[i].Modify(context);
            }

            return context;
        }

        // ── Helpers privados ────────────────────────────────────────

        private void ApplyEntityModifiers(DamageContext context)
        {
            if (context.Attacker is Entidad attackerEnt)
            {
                var mods = attackerEnt.EntityDamageModifiers;
                for (int i = 0; i < mods.Count; i++)
                    mods[i].Modify(context);
            }

            if (context.Defender is Entidad defenderEnt)
            {
                var mods = defenderEnt.EntityDamageModifiers;
                for (int i = 0; i < mods.Count; i++)
                    mods[i].Modify(context);
            }
        }

        private void ApplyEffectHandlers(DamageContext context)
        {
            if (context.Attacker is Entidad attackerEnt)
                attackerEnt.EffectHandler.ApplyToPipeline(context);

            if (context.Defender is Entidad defenderEnt)
                defenderEnt.EffectHandler.ApplyToPipeline(context);
        }

        // ── Utilidad ────────────────────────────────────────────────

        /// <summary>
        /// Crea un DamageContext pre-configurado con los valores de CombatConfig.
        /// Resuelve el crit flag de forma determinista a partir del critChance dado.
        /// El caller decide si es crítico ANTES de entrar al pipeline.
        /// </summary>
        public static DamageContext CreateContext(
            Interfaces.IEntidadCombate attacker,
            Interfaces.IEntidadCombate defender,
            bool isCritical = false)
        {
            var config = CombatConfig.Instance;
            var stats = attacker.CombatStats;

            return new DamageContext
            {
                Attacker             = attacker,
                Defender             = defender,
                IsCritical           = isCritical,
                CritMultiplier       = stats?.critMultiplier ?? config?.baseCritMultiplier ?? 1.5f,
                CritAppliesToElemental = stats?.critAppliesToElemental ?? false,
                AttackElement        = stats?.elementoAtaque ?? Flags.ElementAttribute.None,
                DefenseConstantK     = config?.defenseConstantK ?? 22f,
                RaceModifiers        = config?.raceModifiers,
            };
        }
    }
}

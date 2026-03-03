using Combate;
using Flags;
using Interfaces;
using Padres;
using UnityEngine;

namespace Subclases.Modulos
{
    /// <summary>
    /// Módulo de comportamiento del Paladín (evolución del Guerrero).
    ///
    /// Mecánicas activas al agregar este módulo:
    ///   1. +bonusCuracion (def. 20%) a toda curación otorgada Y recibida (aditivo).
    ///   2. Ataques sin elemento → Light (sustitutivo; no interviene si ya tiene elemento).
    ///   3. +bonusDanoUndead (def. 20%) de daño físico y elemental vs entidades Undead
    ///      (registrado como IDamageModifier en el pipeline del atacante).
    ///   4. Recurso principal: pendiente → Fe. Hook preparado pero sin efecto por ahora.
    /// </summary>
    public class PaladinModulo : IComportamientoDeClase
    {
        public string ModuloId => "paladin";

        private readonly float _bonusCuracion;
        private readonly float _bonusDanoUndead;
        private PaladinDamageMod _damageMod;

        public PaladinModulo(float bonusCuracion = 0.20f, float bonusDanoUndead = 0.20f)
        {
            _bonusCuracion   = bonusCuracion;
            _bonusDanoUndead = bonusDanoUndead;
        }

        // ── Ciclo de vida ────────────────────────────────────────────────────

        public void AlAgregar(Jugador jugador)
        {
            _damageMod = new PaladinDamageMod(_bonusDanoUndead);
            jugador.EntityDamageModifiers.Add(_damageMod);
            Debug.Log($"[PaladinModulo] Módulo agregado a {jugador.Nombre_Entidad}.");
        }

        public void AlRemover(Jugador jugador)
        {
            if (_damageMod != null)
                jugador.EntityDamageModifiers.Remove(_damageMod);
            Debug.Log($"[PaladinModulo] Módulo removido de {jugador.Nombre_Entidad}.");
        }

        // ── Hooks ADITIVOS ───────────────────────────────────────────────────

        public int ModificarCuracionOtorgada(int cantidadBase, IEntidadCombate objetivo)
            => Mathf.RoundToInt(cantidadBase * (1f + _bonusCuracion));

        public int ModificarCuracionRecibida(int cantidadBase)
            => Mathf.RoundToInt(cantidadBase * (1f + _bonusCuracion));

        // ── Hooks SUSTITUTIVOS ───────────────────────────────────────────────

        /// <summary>
        /// Si el ataque no tiene elemento asignado, lo cambia a Light.
        /// Si ya tiene cualquier otro elemento, no interviene (retorna null).
        /// </summary>
        public ElementAttribute? ModificarElementoAtaque(ElementAttribute elementoBase)
            => elementoBase == ElementAttribute.None ? ElementAttribute.Light : (ElementAttribute?)null;

        /// <summary>
        /// Fe pendiente de diseño. Retorna null para no alterar el flujo de Mana.
        /// PENDIENTE: cambiar a TipoRecurso.Fe cuando el sistema de Fe esté implementado.
        /// </summary>
        public TipoRecurso? OverridearRecursoPrincipal() => null;

        // ── IDamageModifier interno: bonus Undead ─────────────────────────────

        /// <summary>
        /// Se registra en EntityDamageModifiers del jugador para actuar dentro del pipeline.
        /// Aplica el bonus de daño después del crítico (Order 350) y antes de la defensa.
        /// </summary>
        private sealed class PaladinDamageMod : IDamageModifier
        {
            public int Order => 350;
            private readonly float _bonus;

            public PaladinDamageMod(float bonus) => _bonus = bonus;

            public void Modify(DamageContext context)
            {
                if (context.Defender == null) return;
                if ((context.Defender.TipoEntidad & TipoEntidades.Undead) == 0) return;

                float mult = 1f + _bonus;
                context.PhysicalDamage  *= mult;
                context.ElementalDamage *= mult;
            }
        }
    }
}

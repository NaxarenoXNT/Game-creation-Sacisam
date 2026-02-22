using System.Collections.Generic;
using Efectos.Modificadores;
using UnityEngine;

namespace Efectos
{
    /// <summary>
    /// Mapa singleton entre EffectModifierType y la instancia del modificador concreto.
    /// Se inicializa una sola vez al arrancar el juego.
    ///
    /// Para agregar un efecto nuevo:
    ///   1. Añadir valor a EffectModifierType.
    ///   2. Crear clase concreta que implemente IEffectModifier.
    ///   3. Registrarla en Initialize() abajo.
    ///   Sin tocar nada más.
    /// </summary>
    public static class EffectModifierRegistry
    {
        private static Dictionary<EffectModifierType, IEffectModifier> _registry;
        private static bool _initialized = false;

        // ── Auto-inicialización al arrancar Unity ────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;

            _registry = new Dictionary<EffectModifierType, IEffectModifier>();

            // ── Registrar todos los modificadores concretos ──────────────────────
            Register(EffectModifierType.Bleed,        new BleedModifier());
            Register(EffectModifierType.Poison,       new PoisonModifier());
            Register(EffectModifierType.Burn,         new BurnModifier());
            Register(EffectModifierType.Stun,         new StunModifier());
            Register(EffectModifierType.Freeze,       new FreezeModifier());
            Register(EffectModifierType.Weaken,       new WeakenModifier());
            Register(EffectModifierType.Slow,         new SlowModifier());
            Register(EffectModifierType.Vulnerable,   new VulnerableModifier());
            Register(EffectModifierType.Blind,        new BlindModifier());
            Register(EffectModifierType.Regeneration, new RegenerationModifier());
            // Agregar nuevos aquí ↓

            _initialized = true;
            Debug.Log($"[EffectModifierRegistry] Inicializado con {_registry.Count} modificadores.");
        }

        // ── API pública ──────────────────────────────────────────────────────────

        public static void Register(EffectModifierType type, IEffectModifier modifier)
        {
            if (_registry.ContainsKey(type))
            {
                Debug.LogWarning($"[EffectModifierRegistry] El tipo {type} ya estaba registrado. Sobreescribiendo.");
            }
            _registry[type] = modifier;
        }

        /// <summary>
        /// Obtiene el modificador para un tipo dado.
        /// Retorna NullEffectModifier si el tipo no está registrado (fail-safe).
        /// </summary>
        public static IEffectModifier Get(EffectModifierType type)
        {
            if (!_initialized) Initialize();

            if (_registry.TryGetValue(type, out var modifier))
                return modifier;

            Debug.LogWarning($"[EffectModifierRegistry] Tipo {type} no registrado. Usando NullModifier.");
            return NullEffectModifier.Instance;
        }

        public static bool IsRegistered(EffectModifierType type)
        {
            if (!_initialized) Initialize();
            return _registry.ContainsKey(type);
        }
    }

    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Implementación nula (Null Object Pattern).
    /// Todos los hooks son no-ops. Evita nullchecks en todo el sistema.
    /// </summary>
    public sealed class NullEffectModifier : IEffectModifier
    {
        public static readonly NullEffectModifier Instance = new NullEffectModifier();
        private NullEffectModifier() { }

        public int Order => 0;
        public void OnApply(EffectInstance instance, Padres.Entidad owner) { }
        public void OnRemove(EffectInstance instance, Padres.Entidad owner) { }
        public void OnTurnStart(EffectInstance instance, Padres.Entidad owner) { }
        public void OnTurnEnd(EffectInstance instance, Padres.Entidad owner) { }
        public void Modify(Combate.DamageContext context, EffectInstance instance) { }
        public void OnCriticalHit(EffectInstance instance, Combate.DamageContext ctx) { }
        public void OnKill(EffectInstance instance, Padres.Entidad owner) { }
    }
}

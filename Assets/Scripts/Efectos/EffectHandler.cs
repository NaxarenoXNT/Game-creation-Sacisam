using System;
using System.Collections.Generic;
using Combate;
using Flags;
using Padres;
using UnityEngine;

namespace Efectos
{
    /// <summary>
    /// Gestor de efectos activos para una entidad concreta.
    /// Vive instanciado en Entidad.
    /// </summary>
    [Serializable]
    public class EffectHandler
    {
        private readonly Entidad _owner;
        private readonly List<EffectInstance> _activeEffects = new List<EffectInstance>();

        // ── Eventos ──────────────────────────────────────────────────────────────
        public event Action<EffectInstance>       OnEffectApplied;
        public event Action<EffectDefinitionSO>   OnEffectExpired;
        public event Action<int, EffectInstance>  OnTurnDamage;   // daño, instancia

        public IReadOnlyList<EffectInstance> ActiveEffects => _activeEffects;

        // ────────────────────────────────────────────────────────────────────────

        public EffectHandler(Entidad owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        // ── Aplicar efecto ───────────────────────────────────────────────────────

        /// <summary>
        /// Intenta aplicar un efecto al owner.
        /// Respeta inmunidades, stacks y renovación de duración.
        /// </summary>
        public void AddEffect(EffectDefinitionSO definition, Entidad source)
        {
            if (definition == null || definition.modifierType == EffectModifierType.None)
            {
                Debug.LogWarning("[EffectHandler] AddEffect: definition nula o sin modifierType.");
                return;
            }

            // 1. Chequear inmunidad por tipo de entidad
            if (definition.IsImmuneEntityType(_owner.TipoEntidad))
            {
                Debug.Log($"[EffectHandler] {_owner.Nombre_Entidad} es inmune a '{definition.displayName}'.");
                return;
            }

            // 2. Buscar instancia existente del mismo efecto
            var existing = GetInstance(definition.id);

            if (existing != null)
            {
                if (!definition.stackable)
                {
                    // Renovar duración (la más larga gana)
                    if (definition.duration > existing.RemainingTurns)
                        existing.RemainingTurns = definition.duration;

                    Debug.Log($"[EffectHandler] '{definition.displayName}' renovado en {_owner.Nombre_Entidad}.");
                    return;
                }

                // Stackeable: agregar stack si no superó el máximo
                if (existing.CurrentStacks < definition.maxStacks)
                {
                    existing.CurrentStacks++;
                    existing.RemainingTurns = definition.duration; // reiniciar duración
                    Debug.Log($"[EffectHandler] '{definition.displayName}' stack {existing.CurrentStacks}/{definition.maxStacks} en {_owner.Nombre_Entidad}.");
                    return;
                }

                Debug.Log($"[EffectHandler] '{definition.displayName}' ya tiene máximo de stacks en {_owner.Nombre_Entidad}.");
                return;
            }

            // 3. Crear instancia nueva
            var instance = new EffectInstance(definition, _owner, source);
            var modifier  = EffectModifierRegistry.Get(definition.modifierType);

            modifier.OnApply(instance, _owner);
            _activeEffects.Add(instance);

            OnEffectApplied?.Invoke(instance);
            Debug.Log($"[EffectHandler] '{definition.displayName}' aplicado a {_owner.Nombre_Entidad}.");
        }

        // ── Remover efecto ───────────────────────────────────────────────────────

        /// <summary>Remueve el efecto con el id dado, si existe.</summary>
        public bool RemoveEffect(string effectId)
        {
            var instance = GetInstance(effectId);
            if (instance == null) return false;

            var modifier = EffectModifierRegistry.Get(instance.Definition.modifierType);
            modifier.OnRemove(instance, _owner);
            _activeEffects.Remove(instance);
            OnEffectExpired?.Invoke(instance.Definition);

            Debug.Log($"[EffectHandler] '{instance.Definition.displayName}' removido de {_owner.Nombre_Entidad}.");
            return true;
        }

        public void ClearAll()
        {
            foreach (var inst in _activeEffects)
            {
                var modifier = EffectModifierRegistry.Get(inst.Definition.modifierType);
                modifier.OnRemove(inst, _owner);
            }
            _activeEffects.Clear();
        }

        // ── Tick de turno ────────────────────────────────────────────────────────

        /// <summary>
        /// Avanza un turno: ejecuta OnTurnStart en cada efecto y descuenta duración.
        /// Llama a OnRemove en los que expiran.
        /// </summary>
        public void Tick()
        {
            var toRemove = new List<EffectInstance>();

            foreach (var instance in _activeEffects)
            {
                var modifier = EffectModifierRegistry.Get(instance.Definition.modifierType);
                modifier.OnTurnStart(instance, _owner);

                instance.RemainingTurns--;

                if (instance.HasExpired)
                    toRemove.Add(instance);
            }

            foreach (var expired in toRemove)
            {
                var modifier = EffectModifierRegistry.Get(expired.Definition.modifierType);
                modifier.OnRemove(expired, _owner);
                _activeEffects.Remove(expired);
                OnEffectExpired?.Invoke(expired.Definition);

                Debug.Log($"[EffectHandler] '{expired.Definition.displayName}' expiró en {_owner.Nombre_Entidad}.");
            }
        }

        // ── Integración con DamagePipeline ───────────────────────────────────────

        /// <summary>
        /// Aplica todos los efectos activos al DamageContext.
        /// Llamar desde el DamagePipeline con el contexto del ataque en curso.
        /// Los efectos se ordenan por IEffectModifier.Order antes de ejecutarse.
        /// </summary>
        public void ApplyToPipeline(DamageContext context)
        {
            // Recopilar pares (modificador, instancia) y ordenar por Order
            var pairs = new List<(IEffectModifier mod, EffectInstance inst)>();

            foreach (var instance in _activeEffects)
            {
                var modifier = EffectModifierRegistry.Get(instance.Definition.modifierType);
                pairs.Add((modifier, instance));
            }

            pairs.Sort((a, b) => a.mod.Order.CompareTo(b.mod.Order));

            foreach (var (mod, inst) in pairs)
                mod.Modify(context, inst);
        }

        /// <summary>
        /// Notifica a todos los efectos activos que se produjo un golpe crítico.
        /// </summary>
        public void NotifyCriticalHit(DamageContext context)
        {
            foreach (var instance in _activeEffects)
            {
                var modifier = EffectModifierRegistry.Get(instance.Definition.modifierType);
                modifier.OnCriticalHit(instance, context);
            }
        }

        /// <summary>
        /// Notifica a todos los efectos activos que el owner mató a un enemigo.
        /// </summary>
        public void NotifyKill()
        {
            foreach (var instance in _activeEffects)
            {
                var modifier = EffectModifierRegistry.Get(instance.Definition.modifierType);
                modifier.OnKill(instance, _owner);
            }
        }

        // ── Queries ──────────────────────────────────────────────────────────────

        public bool HasEffect(string effectId) => GetInstance(effectId) != null;

        public bool HasEffect(EffectModifierType type)
        {
            foreach (var inst in _activeEffects)
                if (inst.Definition.modifierType == type) return true;
            return false;
        }

        /// <summary>
        /// Retorna true si algún efecto activo impide actuar (Stun, Freeze).
        /// Alineado con GestorEstados.EstaIncapacitado para mantener compatibilidad.
        /// </summary>
        public bool EstaIncapacitado =>
            HasEffect(EffectModifierType.Stun) || HasEffect(EffectModifierType.Freeze);

        public EffectInstance GetInstance(string effectId)
        {
            foreach (var inst in _activeEffects)
                if (inst.Definition.id == effectId) return inst;
            return null;
        }

        public override string ToString()
        {
            if (_activeEffects.Count == 0) return $"{_owner.Nombre_Entidad}: sin efectos";
            return $"{_owner.Nombre_Entidad}: [{string.Join(", ", _activeEffects)}]";
        }
    }
}

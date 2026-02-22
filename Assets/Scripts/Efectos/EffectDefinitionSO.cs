using System;
using System.Collections.Generic;
using Flags;
using UnityEngine;

namespace Efectos
{
    /// <summary>
    /// Par clave-valor serializable. Reemplaza Dictionary para que Unity lo muestre en el inspector.
    /// </summary>
    [Serializable]
    public class EffectParameter
    {
        [Tooltip("Nombre del parámetro (ej: damagePercent, duration, threshold)")]
        public string key;

        [Tooltip("Valor del parámetro")]
        public float value;

        public EffectParameter() { }

        public EffectParameter(string key, float value)
        {
            this.key   = key;
            this.value = value;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asset (ScriptableObject) que describe QUÉ es un efecto y cómo se comporta.
    /// No contiene estado mutable — el estado runtime vive en EffectInstance.
    ///
    /// Crear desde: Assets → Create → Efectos → Effect Definition
    /// </summary>
    [CreateAssetMenu(fileName = "Nuevo Efecto", menuName = "Efectos/Effect Definition")]
    public class EffectDefinitionSO : ScriptableObject
    {
        [Header("Identificación")]
        [Tooltip("ID único del efecto. Se usa como clave en el EffectHandler. No cambiar en runtime.")]
        public string id;

        [Tooltip("Nombre visible en UI y logs.")]
        public string displayName;

        [TextArea(1, 3)]
        [Tooltip("Descripción breve para tooltips.")]
        public string description;

        // ── Comportamiento ──────────────────────────────────────────────────────

        [Header("Duración y Stacks")]
        [Tooltip("Duración inicial en turnos.")]
        [Min(1)]
        public int duration = 3;

        [Tooltip("Si true, se puede tener más de una instancia activa del efecto.")]
        public bool stackable = false;

        [Tooltip("Máximo de stacks permitidos (solo relevante si stackable = true).")]
        [Min(1)]
        public int maxStacks = 1;

        // ── Lógica ──────────────────────────────────────────────────────────────

        [Header("Tipo de Modificador")]
        [Tooltip("Clave que EffectModifierRegistry usa para encontrar la clase de lógica concreta.")]
        public EffectModifierType modifierType = EffectModifierType.None;

        // ── Parámetros configurables ────────────────────────────────────────────

        [Header("Parámetros")]
        [Tooltip("Valores configurables desde el editor que la lógica del modificador consume.")]
        public List<EffectParameter> parameters = new List<EffectParameter>();

        // ── Inmunidades ─────────────────────────────────────────────────────────

        [Header("Inmunidades")]
        [Tooltip("Tipos de entidad inmunes a este efecto. Se evalúan antes de crear la instancia.")]
        public List<TipoEntidades> immuneEntityTypes = new List<TipoEntidades>();

        // ── Compatibilidad UI ───────────────────────────────────────────────────

        [Header("UI / Compatibilidad")]
        [Tooltip("StatusFlag asociado para que la UI y GestorEstados puedan reconocer el estado visualmente.")]
        public StatusFlag linkedStatusFlag = StatusFlag.None;

        [Tooltip("Color del ícono/indicador de estado en la UI.")]
        public Color uiColor = Color.white;

        [Tooltip("Ícono de estado para HUD.")]
        public Sprite icon;

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve el valor de un parámetro por clave.
        /// Si la clave no existe retorna defaultValue.
        /// </summary>
        public float GetParam(string key, float defaultValue = 0f)
        {
            foreach (var p in parameters)
            {
                if (p.key == key)
                    return p.value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Verifica si una entidad de cierto tipo es inmune a este efecto.
        /// </summary>
        public bool IsImmuneEntityType(TipoEntidades entityType)
        {
            foreach (var immuneType in immuneEntityTypes)
            {
                // Chequeo compatible con Flags: si el tipo tiene alguno de los bits inmunes
                if ((entityType & immuneType) != 0)
                    return true;
            }
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using Flags;
using UnityEngine;

namespace Combate
{
    /// <summary>
    /// Estadísticas de combate para el cálculo de daño.
    /// Contiene crítico, ataque elemental y resistencias.
    /// </summary>
    [System.Serializable]
    public class CombatStats
    {
        [Header("Estadísticas de Crítico")]
        [Tooltip("Probabilidad de crítico (0.0 a 1.0)")]
        [Range(0f, 1f)]
        public float critChance = 0.05f;
        
        [Tooltip("Multiplicador de daño crítico")]
        [Min(1f)]
        public float critMultiplier = 1.5f;
        
        [Header("Ataque Elemental")]
        [Tooltip("Daño elemental base adicional")]
        public int elementalAttack = 0;
        
        [Tooltip("El elemento principal de ataque")]
        public ElementAttribute elementoAtaque = ElementAttribute.None;
        
        [Header("Resistencias Elementales")]
        [Tooltip("Resistencias a cada elemento (0.0 = sin resistencia, 1.0 = inmune, negativo = vulnerable)")]
        public ElementalResistances resistencias = new ElementalResistances();
        
        [Header("Configuración de Crítico Elemental")]
        [Tooltip("Si es true, el crítico también aplica al daño elemental")]
        public bool critAppliesToElemental = false;
        
        public CombatStats() { }
        
        public CombatStats(float critChance, float critMult, int elemAtk = 0)
        {
            this.critChance = Mathf.Clamp01(critChance);
            this.critMultiplier = Mathf.Max(1f, critMult);
            this.elementalAttack = elemAtk;
        }
        
        /// <summary>
        /// Crea una copia de las estadísticas.
        /// </summary>
        public CombatStats Clone()
        {
            return new CombatStats
            {
                critChance = this.critChance,
                critMultiplier = this.critMultiplier,
                elementalAttack = this.elementalAttack,
                elementoAtaque = this.elementoAtaque,
                resistencias = this.resistencias.Clone(),
                critAppliesToElemental = this.critAppliesToElemental
            };
        }
    }
    
    /// <summary>
    /// Resistencias elementales de una entidad.
    /// Valores: 0 = neutro, positivo = resistencia, negativo = vulnerabilidad
    /// </summary>
    [System.Serializable]
    public class ElementalResistances
    {
        [Range(-0.5f, 1f)] public float fire = 0f;
        [Range(-0.5f, 1f)] public float water = 0f;
        [Range(-0.5f, 1f)] public float light = 0f;
        [Range(-0.5f, 1f)] public float dark = 0f;
        [Range(-0.5f, 1f)] public float air = 0f;
        [Range(-0.5f, 1f)] public float geo = 0f;
        [Range(-0.5f, 1f)] public float electric = 0f;
        [Range(-0.5f, 1f)] public float bloodSpilet = 0f;
        
        /// <summary>
        /// Obtiene la resistencia para un elemento específico.
        /// </summary>
        public float GetResistance(ElementAttribute elemento)
        {
            // Si tiene múltiples elementos, usar el promedio
            float total = 0f;
            int count = 0;
            
            if ((elemento & ElementAttribute.Fire) != 0) { total += fire; count++; }
            if ((elemento & ElementAttribute.Water) != 0) { total += water; count++; }
            if ((elemento & ElementAttribute.Light) != 0) { total += light; count++; }
            if ((elemento & ElementAttribute.Dark) != 0) { total += dark; count++; }
            if ((elemento & ElementAttribute.Air) != 0) { total += air; count++; }
            if ((elemento & ElementAttribute.Geo) != 0) { total += geo; count++; }
            if ((elemento & ElementAttribute.Electric) != 0) { total += electric; count++; }
            if ((elemento & ElementAttribute.BloodSpilet) != 0) { total += bloodSpilet; count++; }
            
            return count > 0 ? total / count : 0f;
        }
        
        /// <summary>
        /// Establece la resistencia para un elemento específico.
        /// </summary>
        public void SetResistance(ElementAttribute elemento, float value)
        {
            value = Mathf.Clamp(value, -0.5f, 1f);
            
            if ((elemento & ElementAttribute.Fire) != 0) fire = value;
            if ((elemento & ElementAttribute.Water) != 0) water = value;
            if ((elemento & ElementAttribute.Light) != 0) light = value;
            if ((elemento & ElementAttribute.Dark) != 0) dark = value;
            if ((elemento & ElementAttribute.Air) != 0) air = value;
            if ((elemento & ElementAttribute.Geo) != 0) geo = value;
            if ((elemento & ElementAttribute.Electric) != 0) electric = value;
            if ((elemento & ElementAttribute.BloodSpilet) != 0) bloodSpilet = value;
        }
        
        /// <summary>
        /// Modifica la resistencia de un elemento (suma/resta).
        /// </summary>
        public void ModifyResistance(ElementAttribute elemento, float delta)
        {
            float current = GetResistance(elemento);
            SetResistance(elemento, current + delta);
        }
        
        public ElementalResistances Clone()
        {
            return new ElementalResistances
            {
                fire = this.fire,
                water = this.water,
                light = this.light,
                dark = this.dark,
                air = this.air,
                geo = this.geo,
                electric = this.electric,
                bloodSpilet = this.bloodSpilet
            };
        }
    }
    
    /// <summary>
    /// Resultado detallado del cálculo de daño.
    /// </summary>
    public struct DamageResult
    {
        public int finalDamage;
        public int physicalDamage;
        public int elementalDamage;
        public bool isCritical;
        public float defenseMultiplier;
        public float elementalMultiplier;
        public float raceAtkMultiplier;
        public float raceDefMultiplier;
        
        public override string ToString()
        {
            return $"Daño: {finalDamage} (Físico: {physicalDamage}, Elemental: {elementalDamage})" +
                   $"{(isCritical ? " ¡CRÍTICO!" : "")}";
        }
    }
}

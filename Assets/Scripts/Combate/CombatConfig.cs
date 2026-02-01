using UnityEngine;

namespace Combate
{
    /// <summary>
    /// Configuración global del sistema de combate.
    /// Singleton accesible desde cualquier parte del código.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Combate/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        private static CombatConfig _instance;
        
        public static CombatConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CombatConfig>("CombatConfig");
                    
                    if (_instance == null)
                    {
                        Debug.LogWarning("CombatConfig no encontrado en Resources. Usando valores por defecto.");
                        _instance = CreateInstance<CombatConfig>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("Fórmula de Defensa")]
        [Tooltip("Constante K para la fórmula de defensa. Valores más bajos = defensa más efectiva")]
        [Range(1f, 20f)]
        public float defenseConstantK = 5f;
        
        [Header("Multiplicadores Elementales")]
        [Tooltip("Multiplicador mínimo de daño elemental (cuando el defensor tiene alta resistencia)")]
        [Range(0f, 0.5f)]
        public float minElementalMultiplier = 0.1f;
        
        [Tooltip("Multiplicador máximo de daño elemental (cuando el defensor es vulnerable)")]
        [Range(1f, 3f)]
        public float maxElementalMultiplier = 1.5f;
        
        [Header("Críticos")]
        [Tooltip("Probabilidad de crítico base para todas las entidades")]
        [Range(0f, 0.5f)]
        public float baseCritChance = 0.05f;
        
        [Tooltip("Multiplicador de crítico base")]
        [Range(1f, 5f)]
        public float baseCritMultiplier = 1.5f;
        
        [Header("Daño")]
        [Tooltip("Daño mínimo garantizado por cualquier ataque")]
        [Min(1)]
        public int minimumDamage = 1;
        
        [Header("Referencias")]
        [Tooltip("Modificadores de raza (opcional)")]
        public RaceModifiers raceModifiers;
        
        [Header("Debug")]
        [Tooltip("Mostrar logs detallados de cálculo de daño")]
        public bool debugDamageCalculation = false;
        
        /// <summary>
        /// Calcula el daño usando la configuración global.
        /// </summary>
        public DamageResult CalculateDamage(AttackerData attacker, DefenderData defender)
        {
            var result = DamageCalculator.CalculateDamage(
                attacker, 
                defender, 
                raceModifiers, 
                defenseConstantK
            );
            
            if (debugDamageCalculation)
            {
                Debug.Log($"[CombatConfig] {result}");
                Debug.Log($"  - RaceATK: {result.raceAtkMultiplier:F2}, RaceDEF: {result.raceDefMultiplier:F2}");
                Debug.Log($"  - DefMult: {result.defenseMultiplier:F2} ({(1f - result.defenseMultiplier) * 100:F1}% mitigación)");
                Debug.Log($"  - ElemMult: {result.elementalMultiplier:F2}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Obtiene stats de combate por defecto.
        /// </summary>
        public CombatStats GetDefaultCombatStats()
        {
            return new CombatStats
            {
                critChance = baseCritChance,
                critMultiplier = baseCritMultiplier,
                elementalAttack = 0,
                critAppliesToElemental = false
            };
        }
    }
}

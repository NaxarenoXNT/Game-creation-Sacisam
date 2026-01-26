using UnityEngine;
using Flags;


[CreateAssetMenu(fileName = "Nuevo Elemento", menuName = "Combate/Element Definition")]
public class ElementDefinition : ScriptableObject
{
    [Header("Identificación")]
    public string elementName;

    public ElementAttribute elementFlag;
    
    [Header("Modificadores Base")]
    [Tooltip("Multiplicador de daño base (1.0 = sin cambio, 1.5 = +50% daño)")]
    public float baseDamageMultiplier = 1.0f;
    
    [Tooltip("Bonus de vida (valor absoluto que se suma)")]
    public int baseHealthBonus = 0;
    
    [Tooltip("Bonus de defensa (valor absoluto que se suma)")]
    public float baseDefenseBonus = 0f;
    
    [Tooltip("Bonus de velocidad (valor absoluto que se suma)")]
    public int baseSpeedBonus = 0;
    
    [Header("Progresión")]
    [Tooltip("XP necesaria para subir del nivel 1 al 2")]
    public float xpPerLevel = 1000f;
    
    [Tooltip("Multiplicador de XP por nivel (cada nivel requiere xpPerLevel * multiplicador^nivel)")]
    public float xpScaling = 1.45f;

    [Tooltip("Nivel máximo del elemento")]
    public int maxLevel = 10;
    
    [Tooltip("Incremento del multiplicador de daño por nivel")]
    public float damagePerLevel = 0.1f;
    
    [Tooltip("Incremento de vida por nivel")]
    public int healthPerLevel = 50;
    
    [Tooltip("Incremento de defensa por nivel")]
    public float defensePerLevel = 2f;
    
    [Tooltip("Incremento de velocidad por nivel")]
    public int speedPerLevel = 5;

    
    [Header("Visual (Para Fase 3)")]
    public Color elementColor = Color.white;
    public GameObject particlePrefab;
    public AudioClip activationSound;
    
    // Método helper para calcular XP requerida para un nivel específico
    public float GetXPRequiredForLevel(int level)
    {
        if (level <= 1) return 0;
        if (level > maxLevel) return float.MaxValue; // Evita pasar del nivel 10
        return xpPerLevel * Mathf.Pow(xpScaling, level - 2);
    }
}
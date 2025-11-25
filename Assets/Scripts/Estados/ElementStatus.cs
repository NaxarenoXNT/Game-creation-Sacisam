using UnityEngine;

[System.Serializable]
public class ElementStatus
{
    [Header("Referencias")]
    public ElementDefinition definition;
    
    [Header("Progresión")]
    public int level = 1;
    public float currentXP = 0f;
    
    // Constructor
    public ElementStatus(ElementDefinition def)
    {
        definition = def;
        level = 1;
        currentXP = 0f;
    }
    
    // Calcula el multiplicador final de daño considerando el nivel
    public float GetFinalDamageMultiplier()
    {
        if (definition == null) return 1.0f;
        return definition.baseDamageMultiplier + (definition.damagePerLevel * (level - 1));
    }
    
    // Calcula el bonus final de vida considerando el nivel
    public int GetFinalHealthBonus()
    {
        if (definition == null) return 0;
        return definition.baseHealthBonus + (definition.healthPerLevel * (level - 1));
    }
    
    // Calcula el bonus final de defensa considerando el nivel
    public float GetFinalDefenseBonus()
    {
        if (definition == null) return 0f;
        return definition.baseDefenseBonus + (definition.defensePerLevel * (level - 1));
    }
    
    // Calcula el bonus final de velocidad considerando el nivel
    public int GetFinalSpeedBonus()
    {
        if (definition == null) return 0;
        return definition.baseSpeedBonus + (definition.speedPerLevel * (level - 1));
    }
    
    // Añade experiencia y sube de nivel si es necesario
    public bool GainXP(float amount)
    {
        if (definition == null) return false;
        
        currentXP += amount;
        float xpNeeded = definition.GetXPRequiredForLevel(level + 1);
        
        if (currentXP >= xpNeeded)
        {
            level++;
            currentXP -= xpNeeded;
            return true; // Subió de nivel
        }
        
        return false; // No subió de nivel
    }
    
    // Obtiene el progreso de XP actual (0 a 1)
    public float GetXPProgress()
    {
        if (definition == null) return 0f;
        
        float xpNeeded = definition.GetXPRequiredForLevel(level + 1);
        if (xpNeeded <= 0) return 1f;
        
        return Mathf.Clamp01(currentXP / xpNeeded);
    }
    
    // Info para debug
    public override string ToString()
    {
        if (definition == null) return "ElementStatus (Sin definición)";
        return $"{definition.elementName} Lv.{level} ({currentXP:F1}/{definition.GetXPRequiredForLevel(level + 1):F1} XP)";
    }
}
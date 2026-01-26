using System.Collections.Generic;
using UnityEngine;
using Flags;
using Padres;
using Interfaces;

/// <summary>
/// Componente que maneja las estadísticas y elementos activos de una entidad.
/// Se sincroniza automáticamente con la clase Entidad para aplicar bonos elementales.
/// </summary>
public class EntityStats : MonoBehaviour
{
    [Header("Sistema Elemental")]
    [Tooltip("Flags de elementos activos (se actualiza automáticamente)")]
    public ElementAttribute activeAttributes = ElementAttribute.None;
    
    [Tooltip("Estados complejos de cada elemento activo")]
    public List<ElementStatus> activeStatuses = new List<ElementStatus>();
    
    [Header("Estadísticas Base (Solo lectura - vienen de Entidad)")]
    [SerializeField] private int baseDamage;
    [SerializeField] private int baseHealth;
    [SerializeField] private float baseDefense;
    [SerializeField] private int baseSpeed;
    
    [Header("Estadísticas Actuales con Bonos Elementales")]
    [SerializeField] private int currentDamage;
    [SerializeField] private int currentMaxHealth;
    [SerializeField] private float currentDefense;
    [SerializeField] private int currentSpeed;
    
    // Propiedades públicas para acceso desde otras clases
    public int CurrentDamage => currentDamage;
    public int CurrentMaxHealth => currentMaxHealth;
    public float CurrentDefense => currentDefense;
    public int CurrentSpeed => currentSpeed;
    
    private Entidad entidadVinculada;
    
    private GameConfig gameConfig;
    
    private void Awake()
    {
        gameConfig = GameConfig.Instance;
    }
    
    
    public void VincularEntidad(Entidad entidad)
    {
        entidadVinculada = entidad;
        SincronizarStatsBase();
        ApplyElementalModifiers();
        
        Debug.Log($"{gameObject.name}: EntityStats vinculado con {entidad.Nombre_Entidad}");
    }
    
    public void SincronizarStatsBase()
    {
        if (entidadVinculada == null) return;
        
        baseDamage = entidadVinculada.PuntosDeAtaque_Entidad;
        baseHealth = entidadVinculada.Vida_Entidad;
        baseDefense = (int)entidadVinculada.PuntosDeDefensa_Entidad;
        baseSpeed = entidadVinculada.Velocidad;
    }
    
    public void AplicarStatsAEntidad()
    {
        if (entidadVinculada == null) return;
        
        // Usar el método interno de Entidad para aplicar los bonos
        entidadVinculada.AplicarBonusElementales(
            currentDamage,
            currentMaxHealth,
            currentDefense,
            currentSpeed
        );
    }
    
    public void AplicarElemento(ElementAttribute elementFlag)
    {
        // Validación
        if (elementFlag == ElementAttribute.None)
        {
            Debug.LogWarning("Intentando aplicar elemento None");
            return;
        }
        
        if (gameConfig == null)
        {
            Debug.LogError("GameConfig no está disponible");
            return;
        }
        
        // Obtener la definición del elemento
        ElementDefinition definition = gameConfig.GetDefinition(elementFlag);
        if (definition == null)
        {
            Debug.LogError($"No se encontró definición para {elementFlag}");
            return;
        }
        
        // Buscar si ya existe un status para este elemento
        ElementStatus existingStatus = activeStatuses.Find(s => s.definition == definition);
        
        if (existingStatus != null)
        {
            // Ya existe: subir nivel o añadir XP
            bool leveledUp = existingStatus.GainXP(50f);
            
            if (leveledUp)
            {
                Debug.Log($"{gameObject.name}: {definition.elementName} subió a nivel {existingStatus.level}!");
            }
        }
        else
        {
            // No existe: crear nuevo status
            ElementStatus newStatus = new ElementStatus(definition);
            activeStatuses.Add(newStatus);
            
            // Actualizar la flag
            activeAttributes |= elementFlag;
            
            Debug.Log($"{gameObject.name}: Elemento {definition.elementName} aplicado (Nivel 1)");
        }
        
        // Recalcular estadísticas
        ApplyElementalModifiers();
    }
    
    public void RemoverElemento(ElementAttribute elementFlag)
    {
        if (gameConfig == null) return;
        
        ElementDefinition definition = gameConfig.GetDefinition(elementFlag);
        if (definition == null) return;
        
        ElementStatus status = activeStatuses.Find(s => s.definition == definition);
        if (status != null)
        {
            activeStatuses.Remove(status);
            activeAttributes &= ~elementFlag; // Quitar la flag
            
            ApplyElementalModifiers();
            Debug.Log($"{gameObject.name}: Elemento {definition.elementName} removido");
        }
    }
     
    public void ApplyElementalModifiers()
    {
        // 1. Sincronizar stats base desde la entidad (por si subió de nivel)
        SincronizarStatsBase();
        
        // 2. Reset a valores base
        currentDamage = baseDamage;
        currentMaxHealth = baseHealth;
        currentDefense = baseDefense;
        currentSpeed = baseSpeed;
        
        // 3. Aplicar multiplicadores y bonus de cada elemento activo
        float damageMultiplier = 1.0f;
        
        foreach (ElementStatus status in activeStatuses)
        {
            if (status == null || status.definition == null) continue;
            
            // Acumular multiplicadores de daño
            damageMultiplier *= status.GetFinalDamageMultiplier();
            
            // Añadir bonus aditivos
            currentMaxHealth += status.GetFinalHealthBonus();
            currentDefense += status.GetFinalDefenseBonus();
            currentSpeed += status.GetFinalSpeedBonus();
        }
        
        // Aplicar multiplicador final de daño
        currentDamage = Mathf.RoundToInt(currentDamage * damageMultiplier);
        
        // 4. Aplicar las stats calculadas a la Entidad
        AplicarStatsAEntidad();
        
        // Debug info
        if (activeStatuses.Count > 0)
        {
            Debug.Log($"{gameObject.name} Stats aplicadas: DMG={currentDamage}, HP={currentMaxHealth}, DEF={currentDefense}, SPD={currentSpeed}");
        }
    }
    
    public bool TieneElemento(ElementAttribute elemento)
    {
        return (activeAttributes & elemento) != 0;
    }
    
    public ElementStatus GetElementStatus(ElementAttribute elementFlag)
    {
        if (gameConfig == null) return null;
        
        ElementDefinition definition = gameConfig.GetDefinition(elementFlag);
        if (definition == null) return null;
        
        return activeStatuses.Find(s => s.definition == definition);
    }

    public void AñadirXPAElemento(ElementAttribute elementFlag, float xpAmount)
    {
        ElementStatus status = GetElementStatus(elementFlag);
        if (status != null)
        {
            bool leveledUp = status.GainXP(xpAmount);

            if (leveledUp)
            {
                Debug.Log($"{gameObject.name}: {status.definition.elementName} subió a nivel {status.level}!");
                ApplyElementalModifiers(); // Recalcular stats con el nuevo nivel
            }
        }
    }
    
    public void DistribuirXPElemental(float xpAmount)
    {
        if (activeStatuses.Count == 0) 
        {
            Debug.LogWarning("No hay elementos activos para distribuir XP.");
            return;
        }
        
        // Distribuye la XP equitativamente entre los elementos activos
        float xpPorElemento = xpAmount / activeStatuses.Count;
        
        Debug.Log($"Distribuyendo {xpAmount:F1} XP entre {activeStatuses.Count} elementos (+{xpPorElemento:F1} XP c/u)");

        foreach (var status in activeStatuses)
        {
            // Reutiliza la función existente para agregar XP
            AñadirXPAElemento(status.definition.elementFlag, xpPorElemento); 
        }
    }
}
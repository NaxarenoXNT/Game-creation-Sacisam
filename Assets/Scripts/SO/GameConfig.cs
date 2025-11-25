using UnityEngine;
using System.Collections.Generic;
using Flasgs;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Combate/Game Config")]
public class GameConfig : ScriptableObject
{
    [System.Serializable]
    public class ElementMapping
    {
        public ElementAttribute elementFlag;
        public ElementDefinition elementDefinition;
    }
    
    [Header("Mapeo de Elementos")]
    [Tooltip("Asocia cada flag de elemento con su definición correspondiente")]
    public List<ElementMapping> elementMappings = new List<ElementMapping>();
    
    
    private static GameConfig _instance;
    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                {
                    Debug.LogError("GameConfig no encontrado en Resources. Crea uno en Resources/GameConfig");
                }
            }
            return _instance;
        }
    }
    
    // Busca la definición de elemento correspondiente a una flag
    public ElementDefinition GetDefinition(ElementAttribute flag)
    {
        // Ignora None
        if (flag == ElementAttribute.None) return null;
        
        foreach (var mapping in elementMappings)
        {
            if (mapping.elementFlag == flag)
            {
                return mapping.elementDefinition;
            }
        }
        
        Debug.LogWarning($"No se encontró definición para el elemento: {flag}");
        return null;
    }
    
    // Validación en el editor
    private void OnValidate()
    {
        // Verificar duplicados
        HashSet<ElementAttribute> flags = new HashSet<ElementAttribute>();
        foreach (var mapping in elementMappings)
        {
            if (mapping.elementFlag == ElementAttribute.None) continue;
            
            if (flags.Contains(mapping.elementFlag))
            {
                Debug.LogWarning($"Flag duplicada detectada: {mapping.elementFlag}");
            }
            flags.Add(mapping.elementFlag);
        }
    }
}
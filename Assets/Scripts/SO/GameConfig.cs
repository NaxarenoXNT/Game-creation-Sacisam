using UnityEngine;
using System.Collections.Generic;
using Flags;

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
    private static bool _initialized = false;
    
    /// <summary>
    /// Instancia singleton del GameConfig.
    /// Se carga automáticamente desde Resources/GameConfig.
    /// </summary>
    public static GameConfig Instance
    {
        get
        {
            if (_instance == null && !_initialized)
            {
                _initialized = true;
                _instance = Resources.Load<GameConfig>("GameConfig");
                
                if (_instance == null)
                {
                    Debug.LogError("❌ GameConfig no encontrado en Resources/GameConfig. " +
                                   "Crea uno usando: Create > Combate > Game Config y muévelo a Assets/Resources/");
                }
                else
                {
                    Debug.Log("✅ GameConfig cargado correctamente.");
                }
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Permite inyectar una instancia manualmente (útil para testing).
    /// </summary>
    public static void SetInstance(GameConfig config)
    {
        _instance = config;
        _initialized = true;
    }
    
    /// <summary>
    /// Verifica si el GameConfig está disponible sin forzar la carga.
    /// </summary>
    public static bool IsAvailable => _instance != null || Resources.Load<GameConfig>("GameConfig") != null;
    
    private void OnEnable()
    {
        // Cuando el ScriptableObject se carga en el editor, registrarlo como instancia
        if (_instance == null)
        {
            _instance = this;
            _initialized = true;
        }
    }
    
    private void OnDisable()
    {
        // Limpiar referencia al descargar (importante para Play Mode)
        if (_instance == this)
        {
            _instance = null;
            _initialized = false;
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
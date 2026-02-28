using UnityEngine;
using Managers;
using World.ChunkSystem;

/// <summary>
/// Script para inicializar el player en la escena de testing.
/// DEBE estar en el GameObject del player con EntityController.
/// Si está en otro GameObject, buscará el player automáticamente.
/// </summary>
public class PlayerInitializer : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si está vacío, buscará EntityController automáticamente")]
    [SerializeField] private EntityController playerController;
    
    [Header("Opciones")]
    [Tooltip("Si true, se registra automáticamente como Main Character en Start")]
    [SerializeField] private bool autoRegisterAsMain = true;
    
    [Tooltip("Si true, registra el player en el WorldChunkManager")]
    [SerializeField] private bool registerInChunkManager = true;
    
    void Start()
    {
        // Buscar el EntityController
        if (playerController == null)
        {
            playerController = FindPlayerController();
        }
        
        if (playerController == null)
        {
            Debug.LogError($"❌ [{name}] No se pudo encontrar ningún EntityController en la escena. " +
                          "Asegúrate de que hay un GameObject con EntityController (el player).");
            return;
        }
        
        // Advertir si este script no está en el player
        if (playerController.gameObject != gameObject)
        {
            Debug.LogWarning($"⚠️ [{name}] PlayerInitializer está en '{gameObject.name}' pero debería estar en '{playerController.gameObject.name}'. " +
                           "Considera moverlo para una mejor organización.");
        }
        
        // Registrar como Main Character
        if (autoRegisterAsMain)
        {
            RegisterAsMainCharacter();
        }
        
        // Registrar en WorldChunkManager
        if (registerInChunkManager)
        {
            RegisterInChunkManager();
        }
    }
    
    /// <summary>
    /// Busca el EntityController del player en la escena.
    /// Prioridad: 1) Este GameObject, 2) Tag "Player", 3) Nombre, 4) Cualquier EntityController
    /// </summary>
    private EntityController FindPlayerController()
    {
        // 1. Intentar en este GameObject primero
        var controller = GetComponent<EntityController>();
        if (controller != null)
        {
            Debug.Log($"✅ EntityController encontrado en este GameObject: {gameObject.name}");
            return controller;
        }
        
        // 2. Buscar por Tag "Player"
        var playerByTag = GameObject.FindGameObjectWithTag("Player");
        if (playerByTag != null)
        {
            controller = playerByTag.GetComponent<EntityController>();
            if (controller != null)
            {
                Debug.Log($"✅ EntityController encontrado por Tag 'Player': {playerByTag.name}");
                return controller;
            }
        }
        
        // 3. Buscar por nombre común (Caballero, Player, Guerrero, etc.)
        string[] commonNames = { "Caballero", "Player", "Guerrero", "Mago", "Jugador" };
        foreach (var name in commonNames)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                controller = obj.GetComponent<EntityController>();
                if (controller != null)
                {
                    Debug.Log($"✅ EntityController encontrado por nombre '{name}': {obj.name}");
                    return controller;
                }
            }
        }
        
        // 4. Último recurso: buscar cualquier EntityController en la escena
        var allControllers = FindObjectsByType<EntityController>(FindObjectsSortMode.None);
        if (allControllers != null && allControllers.Length > 0)
        {
            controller = allControllers[0];
            Debug.LogWarning($"⚠️ Usando el primer EntityController encontrado: {controller.gameObject.name}");
            return controller;
        }
        
        // No se encontró nada
        return null;
    }
    
    private void RegisterAsMainCharacter()
    {
        try
        {
            var partyManager = PlayerPartyManager.Instance;

            // Registrar el personaje primero (requerido antes de SetMainCharacter)
            bool isRegistered = false;
            foreach (var character in partyManager.AllOwnedCharacters)
            {
                if (character == playerController)
                {
                    isRegistered = true;
                    break;
                }
            }
            
            if (!isRegistered)
            {
                partyManager.RegisterCharacter(playerController);
            }

            // Establecer como main (si RegisterCharacter no lo hizo ya automáticamente)
            partyManager.SetMainCharacter(playerController);

            Debug.Log($"✅ [{playerController.name}] registrado como Main Character en PlayerPartyManager");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al registrar Main Character: {e.Message}");
        }
    }
    
    private void RegisterInChunkManager()
    {
        if (WorldChunkManager.Instance != null)
        {
            // El WorldChunkManager obtiene el player transform del PlayerPartyManager
            // Solo necesitamos verificar que esté presente
            Debug.Log($"✅ WorldChunkManager está activo y detectará al player automáticamente");
        }
        else
        {
            Debug.LogWarning($"⚠️ WorldChunkManager no encontrado en la escena");
        }
    }
    
    /// <summary>
    /// Método público para registrar manualmente (útil para debugging)
    /// </summary>
    [ContextMenu("Registrar Manualmente")]
    public void RegisterManually()
    {
        RegisterAsMainCharacter();
        RegisterInChunkManager();
    }
}

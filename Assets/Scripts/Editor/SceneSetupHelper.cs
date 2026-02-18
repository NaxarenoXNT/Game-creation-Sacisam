using UnityEngine;
using UnityEditor;
using Managers;
using System.IO;
using World.ChunkSystem;

/// <summary>
/// Helper para configurar automáticamente la escena de prueba.
/// Menú: Tools > Setup Game Scene
/// </summary>
public class SceneSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup Game Scene")]
    public static void ShowWindow()
    {
        GetWindow<SceneSetupHelper>("Scene Setup Helper");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Setup de Escena para Testing", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Esta herramienta verifica y crea los componentes necesarios en la escena actual.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. Verificar/Crear CombatRules", GUILayout.Height(30)))
        {
            VerifyOrCreateCombatRules();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("2. Verificar GameConfig", GUILayout.Height(30)))
        {
            VerifyGameConfig();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("3. Setup Managers en Escena", GUILayout.Height(30)))
        {
            SetupSceneManagers();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("4. Verificar Player Setup", GUILayout.Height(30)))
        {
            VerifyPlayerSetup();
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("⚡ SETUP COMPLETO", GUILayout.Height(50)))
        {
            SetupCompleto();
        }
    }
    
    private void VerifyOrCreateCombatRules()
    {
        string path = "Assets/Resources/CombatRules.asset";
        
        var existing = Resources.Load<CombatRules>("CombatRules");
        if (existing != null)
        {
            EditorUtility.DisplayDialog("✅ CombatRules Existe", 
                "CombatRules ya existe en Resources.", "OK");
            return;
        }
        
        // Crear directorio si no existe
        Directory.CreateDirectory("Assets/Resources");
        
        // Crear nuevo CombatRules
        var rules = ScriptableObject.CreateInstance<CombatRules>();
        
        // Configuración por defecto
        rules.detectionRadius = 20f;
        rules.engagementRadius = 10f;
        rules.maxEnemiesPerEncounter = 5;
        rules.useReinforcementQueue = true;
        rules.minAlliesRequired = 1;
        rules.maxAlliesPerEncounter = 4;
        rules.autoStartCombat = true;
        rules.encounterCooldown = 5f;
        rules.maxLevelDifference = 10;
        rules.prioritization = EnemyPrioritization.ByDistance;
        rules.prioritizeAggro = true;
        rules.requireLineOfSight = false;
        rules.showDebugGizmos = true;
        
        AssetDatabase.CreateAsset(rules, path);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("✅ CombatRules Creado", 
            $"CombatRules creado en: {path}\n\nPuedes ajustar los valores en el Inspector.", "OK");
        
        Selection.activeObject = rules;
    }
    
    private void VerifyGameConfig()
    {
        var config = Resources.Load<GameConfig>("GameConfig");
        if (config != null)
        {
            EditorUtility.DisplayDialog("✅ GameConfig Existe", 
                $"GameConfig encontrado en Resources.\n\nMappings: {config.elementMappings.Count} elementos", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("❌ GameConfig No Encontrado", 
                "GameConfig no existe en Resources.\n\n" +
                "Por favor crea uno:\n" +
                "1. Click derecho en Project > Create > Combate > Game Config\n" +
                "2. Muévelo a Assets/Resources/\n" +
                "3. Configura los Element Mappings", "OK");
        }
    }
    
    private void SetupSceneManagers()
    {
        bool cambios = false;
        
        // 1. WorldChunkManager
        var chunkManager = FindFirstObjectByType<WorldChunkManager>();
        if (chunkManager == null)
        {
            var go = new GameObject("WorldChunkManager");
            chunkManager = go.AddComponent<WorldChunkManager>();
            Debug.Log("✅ WorldChunkManager creado");
            cambios = true;
        }
        
        // 2. DynamicEnemyPoolManager
        var poolManager = FindFirstObjectByType<DynamicEnemyPoolManager>();
        if (poolManager == null)
        {
            var go = new GameObject("DynamicEnemyPoolManager");
            poolManager = go.AddComponent<DynamicEnemyPoolManager>();
            Debug.Log("✅ DynamicEnemyPoolManager creado");
            cambios = true;
        }
        
        // 3. PlayerPartyManager
        var partyManager = FindFirstObjectByType<PlayerPartyManager>();
        if (partyManager == null)
        {
            var go = new GameObject("PlayerPartyManager");
            partyManager = go.AddComponent<PlayerPartyManager>();
            Debug.Log("✅ PlayerPartyManager creado");
            cambios = true;
        }
        
        // 4. CombatEncounterManager
        var encounterManager = FindFirstObjectByType<CombatEncounterManager>();
        if (encounterManager == null)
        {
            var go = new GameObject("CombatEncounterManager");
            encounterManager = go.AddComponent<CombatEncounterManager>();
            Debug.Log("✅ CombatEncounterManager creado");
            cambios = true;
        }
        
        // 5. CombateManager
        var combatManager = FindFirstObjectByType<CombateManager>();
        if (combatManager == null)
        {
            var go = new GameObject("CombateManager");
            combatManager = go.AddComponent<CombateManager>();
            Debug.Log("✅ CombateManager creado");
            cambios = true;
        }
        
        if (cambios)
        {
            EditorUtility.DisplayDialog("✅ Managers Creados", 
                "Managers necesarios han sido creados en la escena.\n\n" +
                "IMPORTANTE: Configura las referencias en el Inspector:\n" +
                "- WorldChunkManager: Player Transform\n" +
                "- DynamicEnemyPoolManager: Enemy Prefab\n" +
                "- PlayerPartyManager: Registra el player", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("ℹ️ Managers Ya Existen", 
                "Todos los managers necesarios ya están en la escena.", "OK");
        }
    }
    
    private void VerifyPlayerSetup()
    {
        // Buscar todos los GameObjects con "Player" en el nombre
        var allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        GameObject player = null;
        
        foreach (var obj in allObjects)
        {
            if (obj.name.ToLower().Contains("player") || obj.name.ToLower().Contains("caballero"))
            {
                player = obj;
                break;
            }
        }
        
        if (player == null)
        {
            EditorUtility.DisplayDialog("❌ Player No Encontrado", 
                "No se encontró el GameObject del player en la escena.\n\n" +
                "Arrastra tu prefab de player a la escena.", "OK");
            return;
        }
        
        string mensaje = $"Player encontrado: {player.name}\n\n";
        
        // Verificar EntityController
        var entityController = player.GetComponent<EntityController>();
        if (entityController != null)
        {
            mensaje += "✅ EntityController presente\n";
        }
        else
        {
            mensaje += "❌ EntityController FALTA\n";
        }
        
        // Verificar EntityStats
        var entityStats = player.GetComponent<EntityStats>();
        if (entityStats != null)
        {
            mensaje += "✅ EntityStats presente\n";
        }
        else
        {
            mensaje += "❌ EntityStats FALTA\n";
        }
        
        // Verificar PlayerInterestZone
        var interestZone = player.GetComponentInChildren<PlayerInterestZone>();
        if (interestZone != null)
        {
            mensaje += "✅ PlayerInterestZone presente\n";
        }
        else
        {
            mensaje += "⚠️ PlayerInterestZone FALTA (puede estar en otro GameObject)\n";
        }
        
        mensaje += "\nComponentes recomendados:\n";
        mensaje += "- EntityController (obligatorio)\n";
        mensaje += "- EntityStats (obligatorio)\n";
        mensaje += "- PlayerInterestZone (hijo con Collider)\n";
        mensaje += "- CharacterController o Rigidbody para movimiento";
        
        EditorUtility.DisplayDialog("Verificación del Player", mensaje, "OK");
    }
    
    private void SetupCompleto()
    {
        VerifyOrCreateCombatRules();
        VerifyGameConfig();
        SetupSceneManagers();
        VerifyPlayerSetup();
        
        Debug.Log("=== SETUP COMPLETO ===");
        Debug.Log("Verifica las advertencias en los diálogos anteriores.");
        Debug.Log("Configura las referencias faltantes en el Inspector.");
    }
    
    private static new T FindFirstObjectByType<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>();
    }
    
    private static new T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object
    {
        return Object.FindObjectsByType<T>(sortMode);
    }
}

using UnityEngine;
using UnityEditor;
using Managers;
using GameInput;
using GameFlow;
using Movement;
using Camera;
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
        
        if (GUILayout.Button("4. Setup Cámara", GUILayout.Height(30)))
        {
            SetupCamera();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("5. Verificar Player Setup", GUILayout.Height(30)))
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
        var log = new System.Text.StringBuilder();
        EnsureSceneManagers(log);

        EditorUtility.DisplayDialog(
            "Setup Managers",
            log.Length > 0
                ? "Cambios realizados:\n\n" + log +
                  "\nIMPORTANTE: Configura las referencias en el Inspector:\n" +
                  "- WorldChunkManager: Player Transform\n" +
                  "- DynamicEnemyPoolManager: Enemy Prefab\n" +
                  "- PlayerPartyManager: asigna el main character\n" +
                  "- GameInputManager: Ground/Entity/Enemy layers\n" +
                  "- IsometricCameraController: CameraSettings asset"
                : "Todos los managers ya estaban en la escena. Nada que crear.",
            "OK");
    }

    /// <summary>
    /// Crea los managers faltantes sin mostrar ningún diálogo.
    /// Añade un registro de lo creado al StringBuilder recibido.
    /// Seguro de llamar múltiples veces: nunca crea un manager si ya existe.
    /// </summary>
    private void EnsureSceneManagers(System.Text.StringBuilder log)
    {
        // 1. WorldChunkManager
        if (FindFirstObjectByType<WorldChunkManager>() == null)
        {
            new GameObject("WorldChunkManager").AddComponent<WorldChunkManager>();
            log.AppendLine("✅ WorldChunkManager creado");
        }

        // 2. DynamicEnemyPoolManager
        if (FindFirstObjectByType<DynamicEnemyPoolManager>() == null)
        {
            new GameObject("DynamicEnemyPoolManager").AddComponent<DynamicEnemyPoolManager>();
            log.AppendLine("✅ DynamicEnemyPoolManager creado");
        }

        // 3. PlayerPartyManager
        if (FindFirstObjectByType<PlayerPartyManager>() == null)
        {
            new GameObject("PlayerPartyManager").AddComponent<PlayerPartyManager>();
            log.AppendLine("✅ PlayerPartyManager creado");
        }

        // 4. CombatEncounterManager
        if (FindFirstObjectByType<CombatEncounterManager>() == null)
        {
            new GameObject("CombatEncounterManager").AddComponent<CombatEncounterManager>();
            log.AppendLine("✅ CombatEncounterManager creado");
        }

        // 5. CombateManager
        if (FindFirstObjectByType<CombateManager>() == null)
        {
            new GameObject("CombateManager").AddComponent<CombateManager>();
            log.AppendLine("✅ CombateManager creado");
        }

        // 6. GameFlowController
        if (FindFirstObjectByType<GameFlowController>() == null)
        {
            new GameObject("GameFlowController").AddComponent<GameFlowController>();
            log.AppendLine("✅ GameFlowController creado");
        }

        // 7. GameInputManager
        if (FindFirstObjectByType<GameInputManager>() == null)
        {
            new GameObject("GameInputManager").AddComponent<GameInputManager>();
            log.AppendLine("✅ GameInputManager creado");
        }

        // 8. PlayerMovementController (debe vivir en el prefab del player)
        if (FindFirstObjectByType<PlayerMovementController>() == null)
            log.AppendLine("⚠️ PlayerMovementController no encontrado (añádelo al prefab del player)");

        // 9. IsometricCameraController + CameraSettings
        EnsureCamera(log);
    }

    /// <summary>
    /// Garantiza que exista una cámara funcional:
    /// - Crea el GO con Camera + IsometricCameraController si falta.
    /// - Crea el asset CameraSettings en Resources si falta.
    /// - Asigna CameraSettings al controlador (el campo settings serializado).
    /// Nunca duplica: verifica antes de crear o asignar.
    /// </summary>
    private void EnsureCamera(System.Text.StringBuilder log)
    {
        // 1. Obtener o crear CameraSettings
        var settings = Resources.Load<Camera.CameraSettings>("CameraSettings");
        if (settings == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            settings = ScriptableObject.CreateInstance<Camera.CameraSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Resources/CameraSettings.asset");
            AssetDatabase.SaveAssets();
            log.AppendLine("✅ CameraSettings.asset creado en Resources/");
        }

        // 2. Obtener o crear el GameObject de cámara
        var controller = FindFirstObjectByType<IsometricCameraController>();
        if (controller == null)
        {
            // Reutilizar Main Camera si existe, o crear una nueva
            var camGO = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main.gameObject
                : new GameObject("MainCamera");

            if (camGO.GetComponent<UnityEngine.Camera>() == null)
            {
                var cam = camGO.AddComponent<UnityEngine.Camera>();
                cam.clearFlags   = CameraClearFlags.Skybox;
                cam.fieldOfView  = 60f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane  = 1000f;
            }

            if (camGO.GetComponent<AudioListener>() == null)
                camGO.AddComponent<AudioListener>();

            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0f, 15f, -10f);
            camGO.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            controller = camGO.AddComponent<IsometricCameraController>();
            log.AppendLine("✅ IsometricCameraController creado en " + camGO.name);
        }

        // 3. Asignar CameraSettings si el campo está vacío
        var so = new UnityEditor.SerializedObject(controller);
        var settingsProp = so.FindProperty("settings");
        if (settingsProp != null && settingsProp.objectReferenceValue == null)
        {
            settingsProp.objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("✅ CameraSettings asignado al IsometricCameraController");
        }
    }

    private void SetupCamera()
    {
        var log = new System.Text.StringBuilder();
        EnsureCamera(log);
        EditorUtility.DisplayDialog(
            "Setup Cámara",
            log.Length > 0
                ? log.ToString() + "\nRecuerda: necesitas NavMesh bakeado (Window > AI > Navigation > Bake)."
                : "La cámara ya estaba correctamente configurada.",
            "OK");
    }
    
    private void VerifyPlayerSetup()
    {
        // Buscar la entidad marcada como player
        var allControllers = FindObjectsByType<EntityController>(FindObjectsSortMode.None);
        EntityController playerEC = null;
        foreach (var ec in allControllers)
            if (ec.IsPlayerOwned) { playerEC = ec; break; }

        // Fallback: buscar por nombre
        if (playerEC == null)
        {
            foreach (var ec in allControllers)
            {
                string n = ec.gameObject.name.ToLower();
                if (n.Contains("player") || n.Contains("caballero"))
                { playerEC = ec; break; }
            }
        }

        if (playerEC == null)
        {
            EditorUtility.DisplayDialog("❌ Player No Encontrado",
                "No se encontró ningún EntityController con isPlayerOwned = true en la escena.\n\n" +
                "Arrastra el prefab del player y marca isPlayerOwned en el Inspector.", "OK");
            return;
        }

        GameObject player = playerEC.gameObject;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"GameObject: {player.name}");
        sb.AppendLine();

        // ── Componentes obligatorios en el root ──────────────────────────
        sb.AppendLine("=== ROOT ===");

        // EntityController
        sb.AppendLine("✅ EntityController presente");
        if (!playerEC.IsPlayerOwned)
            sb.AppendLine("   ❌ isPlayerOwned = false  ← actívalo en el Inspector");
        else
            sb.AppendLine("   ✅ isPlayerOwned = true");

        // ClaseData
        // (no hay getter público, pero si entidadLogica es null el juego da error)
        if (playerEC.EntidadLogica == null)
            sb.AppendLine("   ❌ ClaseData NO asignado  ← asigna un ClaseData en el Inspector");
        else
            sb.AppendLine($"   ✅ ClaseData → entidad: \"{playerEC.Nombre_Entidad}\"");

        // EntityStats
        if (player.GetComponent<EntityStats>() != null)
            sb.AppendLine("✅ EntityStats presente");
        else
            sb.AppendLine("⚠️ EntityStats ausente (se auto-crea en Awake, pero conviene añadirlo manualmente)");

        // NavMeshAgent — OBLIGATORIO ([RequireComponent] de PlayerMovementController)
        if (player.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            sb.AppendLine("✅ NavMeshAgent presente");
        else
            sb.AppendLine("❌ NavMeshAgent FALTA  ← requerido por PlayerMovementController");

        // PlayerMovementController — OBLIGATORIO para WASD / click-to-move
        if (player.GetComponent<PlayerMovementController>() != null)
            sb.AppendLine("✅ PlayerMovementController presente");
        else
            sb.AppendLine("❌ PlayerMovementController FALTA  ← sin este componente no hay control");

        // Collider de física (CapsuleCollider es el más habitual en personajes)
        bool tieneCollider = player.GetComponent<CapsuleCollider>() != null
                          || player.GetComponent<CharacterController>() != null
                          || player.GetComponent<BoxCollider>() != null;
        if (tieneCollider)
            sb.AppendLine("✅ Collider de física presente");
        else
            sb.AppendLine("⚠️ Sin collider de física  ← añade CapsuleCollider para colisiones");

        // Animator — opcional pero esperado por UpdateAnimations()
        if (player.GetComponent<Animator>() != null)
            sb.AppendLine("✅ Animator presente");
        else
            sb.AppendLine("⚠️ Animator ausente  ← animaciones desactivadas (Speed / IsMoving)");

        // ── Hijo: zona de interés ────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("=== HIJO (InterestZone) ===");

        var interestZone = player.GetComponentInChildren<PlayerInterestZone>();
        if (interestZone != null)
        {
            sb.AppendLine("✅ PlayerInterestZone presente");
            var sc = interestZone.GetComponent<SphereCollider>();
            if (sc != null)
                sb.AppendLine(sc.isTrigger ? "✅ SphereCollider (IsTrigger = true)" : "❌ SphereCollider existe pero IsTrigger = false");
            else
                sb.AppendLine("❌ SphereCollider FALTA en el hijo con PlayerInterestZone");
        }
        else
        {
            sb.AppendLine("❌ PlayerInterestZone FALTA");
            sb.AppendLine("   → Crea un hijo, añade PlayerInterestZone + SphereCollider (IsTrigger = true)");
        }

        // ── Resumen de estructura esperada ───────────────────────────────
        sb.AppendLine();
        sb.AppendLine("=== ESTRUCTURA COMPLETA ESPERADA ===");
        sb.AppendLine("[Root]");
        sb.AppendLine("  EntityController   (ClaseData, isPlayerOwned=true)");
        sb.AppendLine("  EntityStats");
        sb.AppendLine("  NavMeshAgent");
        sb.AppendLine("  PlayerMovementController");
        sb.AppendLine("  CapsuleCollider    (para física)");
        sb.AppendLine("  Animator           (opcional)");
        sb.AppendLine("[Child: InterestZone]");
        sb.AppendLine("  PlayerInterestZone");
        sb.AppendLine("  SphereCollider     (IsTrigger = true)");

        EditorUtility.DisplayDialog("Verificación del Player", sb.ToString(), "OK");
    }
    
    private void SetupCompleto()
    {
        var resumen = new System.Text.StringBuilder();

        // --- CombatRules ---
        if (Resources.Load<CombatRules>("CombatRules") == null)
        {
            Directory.CreateDirectory("Assets/Resources");
            var rules = ScriptableObject.CreateInstance<CombatRules>();
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
            AssetDatabase.CreateAsset(rules, "Assets/Resources/CombatRules.asset");
            AssetDatabase.SaveAssets();
            resumen.AppendLine("✅ CombatRules creado en Resources");
        }
        else
        {
            resumen.AppendLine("✔ CombatRules ya existe");
        }

        // --- GameConfig ---
        if (Resources.Load<GameConfig>("GameConfig") == null)
            resumen.AppendLine("❌ GameConfig NO encontrado — créalo y muévelo a Resources/");
        else
            resumen.AppendLine("✔ GameConfig ya existe");

        // --- Managers en escena ---
        resumen.AppendLine("");
        EnsureSceneManagers(resumen);

        // --- Cámara ---
        resumen.AppendLine("");
        EnsureCamera(resumen);

        Debug.Log("=== SETUP COMPLETO ===\n" + resumen);

        EditorUtility.DisplayDialog("⚡ Setup Completo — Resultado", resumen.ToString() +
            "\n\nRevisa los ⚠️ o ❌ y configura las referencias en el Inspector.", "OK");
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

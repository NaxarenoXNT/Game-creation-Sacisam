using UnityEngine;
using UnityEditor;

/// <summary>
/// Herramienta de editor para configurar automáticamente el player con cámara y movimiento.
/// Menú: Tools > Setup Player and Camera
/// </summary>
public class PlayerCameraSetup : EditorWindow
{
    [MenuItem("Tools/Setup Player and Camera")]
    public static void ShowWindow()
    {
        GetWindow<PlayerCameraSetup>("Player & Camera Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Setup de Player y Cámara", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Esta herramienta configura automáticamente:\n" +
            "• Cámara isométrica que sigue al player\n" +
            "• Sistema de input (GameInputManager)\n" +
            "• Componentes de movimiento en el player\n" +
            "• NavMeshAgent para pathfinding",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. Crear Cámara Isométrica", GUILayout.Height(30)))
        {
            CrearCamaraIsometrica();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("2. Crear GameInputManager", GUILayout.Height(30)))
        {
            CrearGameInputManager();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("3. Configurar Player Seleccionado", GUILayout.Height(30)))
        {
            ConfigurarPlayerSeleccionado();
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("⚡ SETUP COMPLETO", GUILayout.Height(50)))
        {
            SetupCompleto();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "IMPORTANTE: Necesitas un NavMesh bakeado en tu escena para que el movimiento funcione.\n" +
            "Window > AI > Navigation > Bake",
            MessageType.Warning
        );
    }
    
    private void CrearCamaraIsometrica()
    {
        // Buscar si ya existe
        var existing = FindFirstObjectByType<Camera.IsometricCameraController>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("✅ Cámara Existe", 
                $"IsometricCameraController ya existe en: {existing.gameObject.name}", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        
        // Crear GameObject con cámara
        var cameraGO = new GameObject("MainCamera");
        cameraGO.tag = "MainCamera";
        
        // Agregar Camera component
        var cam = cameraGO.AddComponent<UnityEngine.Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        
        // Agregar AudioListener
        cameraGO.AddComponent<AudioListener>();
        
        // Agregar el controller
        var controller = cameraGO.AddComponent<Camera.IsometricCameraController>();
        
        // Crear CameraSettings si no existe
        var settings = Resources.Load<Camera.CameraSettings>("CameraSettings");
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<Camera.CameraSettings>();
            
            // Crear directorio Resources si no existe
            if (!System.IO.Directory.Exists("Assets/Resources"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources");
            }
            
            AssetDatabase.CreateAsset(settings, "Assets/Resources/CameraSettings.asset");
            AssetDatabase.SaveAssets();
            
            Debug.Log("✅ CameraSettings.asset creado en Resources/");
        }
        
        // Posicionar la cámara en una posición isométrica inicial
        cameraGO.transform.position = new Vector3(0, 15, -10);
        cameraGO.transform.rotation = Quaternion.Euler(45, 0, 0);
        
        EditorUtility.DisplayDialog("✅ Cámara Creada", 
            "IsometricCameraController creado con éxito.\n\n" +
            "La cámara seguirá automáticamente al Main Character del PlayerPartyManager.", "OK");
        
        Selection.activeGameObject = cameraGO;
    }
    
    private void CrearGameInputManager()
    {
        var existing = FindFirstObjectByType<GameInput.GameInputManager>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("✅ GameInputManager Existe", 
                $"GameInputManager ya existe en: {existing.gameObject.name}", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        
        var go = new GameObject("GameInputManager");
        var inputManager = go.AddComponent<GameInput.GameInputManager>();
        
        EditorUtility.DisplayDialog("✅ GameInputManager Creado", 
            "GameInputManager creado con éxito.\n\n" +
            "Gestiona input de teclado/mouse para movimiento y selección.", "OK");
        
        Selection.activeGameObject = go;
    }
    
    private void ConfigurarPlayerSeleccionado()
    {
        var selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("❌ Ningún GameObject Seleccionado", 
                "Selecciona el GameObject del player en la Hierarchy primero.", "OK");
            return;
        }
        
        string mensaje = $"Configurando: {selected.name}\n\n";
        bool cambios = false;
        
        // 1. NavMeshAgent
        var agent = selected.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null)
        {
            agent = selected.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 5f;
            agent.angularSpeed = 720f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.1f;
            mensaje += "✅ NavMeshAgent agregado\n";
            cambios = true;
        }
        else
        {
            mensaje += "ℹ️ NavMeshAgent ya existe\n";
        }
        
        // 2. PlayerMovementController
        var movement = selected.GetComponent<Movement.PlayerMovementController>();
        if (movement == null)
        {
            movement = selected.AddComponent<Movement.PlayerMovementController>();
            mensaje += "✅ PlayerMovementController agregado\n";
            cambios = true;
        }
        else
        {
            mensaje += "ℹ️ PlayerMovementController ya existe\n";
        }
        
        // 3. PlayerInitializer
        var initializer = selected.GetComponent<PlayerInitializer>();
        if (initializer == null)
        {
            initializer = selected.AddComponent<PlayerInitializer>();
            mensaje += "✅ PlayerInitializer agregado\n";
            cambios = true;
        }
        else
        {
            mensaje += "ℹ️ PlayerInitializer ya existe\n";
        }
        
        // 4. Verificar Tag
        if (selected.tag != "Player")
        {
            selected.tag = "Player";
            mensaje += "✅ Tag 'Player' asignado\n";
            cambios = true;
        }
        else
        {
            mensaje += "ℹ️ Tag 'Player' ya asignado\n";
        }
        
        if (cambios)
        {
            EditorUtility.SetDirty(selected);
            mensaje += "\n✅ Player configurado correctamente!";
        }
        else
        {
            mensaje += "\nℹ️ Player ya estaba configurado.";
        }
        
        EditorUtility.DisplayDialog("Configuración del Player", mensaje, "OK");
    }
    
    private void SetupCompleto()
    {
        Debug.Log("=== SETUP COMPLETO INICIADO ===");
        
        CrearCamaraIsometrica();
        CrearGameInputManager();
        
        // Intentar encontrar y configurar el player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Buscar por nombre
            player = GameObject.Find("Caballero");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
        }
        
        if (player != null)
        {
            Selection.activeGameObject = player;
            ConfigurarPlayerSeleccionado();
        }
        else
        {
            EditorUtility.DisplayDialog("⚠️ Player No Encontrado", 
                "No se encontró el GameObject del player.\n\n" +
                "Por favor:\n" +
                "1. Arrastra tu prefab de player a la escena\n" +
                "2. Selecciónalo\n" +
                "3. Ejecuta '3. Configurar Player Seleccionado'", "OK");
        }
        
        Debug.Log("=== SETUP COMPLETO FINALIZADO ===");
        Debug.Log("SIGUIENTE PASO: Bake el NavMesh (Window > AI > Navigation > Bake)");
    }
    
    private static new T FindFirstObjectByType<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>();
    }
}

using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

/// <summary>
/// Script de limpieza para remover componentes del player que fueron agregados
/// incorrectamente a la MainCamera por la herramienta de setup.
/// </summary>
public class CleanupCameraComponents : EditorWindow
{
    [MenuItem("Tools/Cleanup Camera Components")]
    public static void ShowWindow()
    {
        var window = GetWindow<CleanupCameraComponents>("Camera Cleanup");
        window.minSize = new Vector2(400, 300);
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Limpieza de Componentes de Cámara", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Esta herramienta remueve componentes de player que fueron agregados " +
            "incorrectamente a la MainCamera durante el setup automático.\n\n" +
            "Componentes que se removerán de MainCamera:\n" +
            "• NavMeshAgent\n" +
            "• PlayerMovementController\n" +
            "• PlayerInitializer (duplicado)",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Diagnosticar Cámara", GUILayout.Height(30)))
        {
            DiagnosticarCamera();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Limpiar MainCamera", GUILayout.Height(40)))
        {
            LimpiarMainCamera();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Diagnosticar Player", GUILayout.Height(30)))
        {
            DiagnosticarPlayer();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Verificar Player Tiene Componentes Necesarios", GUILayout.Height(30)))
        {
            VerificarPlayerCompleto();
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("⚡ LIMPIEZA COMPLETA Y VERIFICACIÓN", GUILayout.Height(50)))
        {
            LimpiezaCompleta();
        }
    }
    
    private void DiagnosticarCamera()
    {
        var camera = GameObject.FindGameObjectWithTag("MainCamera");
        
        if (camera == null)
        {
            EditorUtility.DisplayDialog("❌ Cámara No Encontrada", 
                "No se encontró ningún GameObject con tag 'MainCamera'.", "OK");
            return;
        }
        
        string mensaje = $"MainCamera: {camera.name}\n\n";
        
        // Componentes que DEBEN estar
        mensaje += "=== COMPONENTES CORRECTOS ===\n";
        mensaje += CheckComponent<UnityEngine.Camera>(camera, "Camera") + "\n";
        mensaje += CheckComponent<AudioListener>(camera, "AudioListener") + "\n";
        mensaje += CheckComponent<Camera.IsometricCameraController>(camera, "IsometricCameraController") + "\n";
        
        // Componentes que NO deben estar
        mensaje += "\n=== COMPONENTES INCORRECTOS ===\n";
        var navMesh = camera.GetComponent<NavMeshAgent>();
        var movement = camera.GetComponent<Movement.PlayerMovementController>();
        var initializer = camera.GetComponent<PlayerInitializer>();
        
        if (navMesh != null)
            mensaje += "❌ NavMeshAgent (NO debería estar aquí)\n";
        else
            mensaje += "✅ NavMeshAgent (no encontrado - correcto)\n";
            
        if (movement != null)
            mensaje += "❌ PlayerMovementController (NO debería estar aquí)\n";
        else
            mensaje += "✅ PlayerMovementController (no encontrado - correcto)\n";
            
        if (initializer != null)
            mensaje += "❌ PlayerInitializer (NO debería estar aquí)\n";
        else
            mensaje += "✅ PlayerInitializer (no encontrado - correcto)\n";
        
        Debug.Log(mensaje);
        EditorUtility.DisplayDialog("Diagnóstico de Cámara", mensaje, "OK");
    }
    
    private void DiagnosticarPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            // Buscar por nombre
            player = GameObject.Find("Caballero");
            if (player == null)
            {
                EditorUtility.DisplayDialog("❌ Player No Encontrado", 
                    "No se encontró el GameObject del player.", "OK");
                return;
            }
        }
        
        string mensaje = $"Player: {player.name}\n\n";
        
        mensaje += "=== COMPONENTES NECESARIOS ===\n";
        mensaje += CheckComponent<EntityController>(player, "EntityController") + "\n";
        mensaje += CheckComponent<EntityStats>(player, "EntityStats") + "\n";
        mensaje += CheckComponent<NavMeshAgent>(player, "NavMeshAgent") + "\n";
        mensaje += CheckComponent<Movement.PlayerMovementController>(player, "PlayerMovementController") + "\n";
        mensaje += CheckComponent<PlayerInitializer>(player, "PlayerInitializer") + "\n";
        
        Debug.Log(mensaje);
        EditorUtility.DisplayDialog("Diagnóstico del Player", mensaje, "OK");
    }
    
    private string CheckComponent<T>(GameObject obj, string name) where T : Component
    {
        var component = obj.GetComponent<T>();
        return component != null ? $"✅ {name}" : $"❌ {name} (FALTA)";
    }
    
    private void LimpiarMainCamera()
    {
        var camera = GameObject.FindGameObjectWithTag("MainCamera");
        
        if (camera == null)
        {
            EditorUtility.DisplayDialog("❌ Error", 
                "No se encontró MainCamera.", "OK");
            return;
        }
        
        int removidos = 0;
        string log = $"Limpiando MainCamera: {camera.name}\n\n";
        
        // Remover NavMeshAgent
        var navMesh = camera.GetComponent<NavMeshAgent>();
        if (navMesh != null)
        {
            DestroyImmediate(navMesh);
            log += "✅ NavMeshAgent removido\n";
            removidos++;
        }
        
        // Remover PlayerMovementController
        var movement = camera.GetComponent<Movement.PlayerMovementController>();
        if (movement != null)
        {
            DestroyImmediate(movement);
            log += "✅ PlayerMovementController removido\n";
            removidos++;
        }
        
        // Remover PlayerInitializer
        var initializer = camera.GetComponent<PlayerInitializer>();
        if (initializer != null)
        {
            DestroyImmediate(initializer);
            log += "✅ PlayerInitializer removido\n";
            removidos++;
        }
        
        if (removidos == 0)
        {
            log += "ℹ️ No había componentes para remover.\n";
        }
        else
        {
            log += $"\n🎉 {removidos} componente(s) removido(s) exitosamente.";
            EditorUtility.SetDirty(camera);
        }
        
        Debug.Log(log);
        EditorUtility.DisplayDialog("Limpieza Completada", log, "OK");
    }
    
    private void VerificarPlayerCompleto()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            player = GameObject.Find("Caballero");
        }
        
        if (player == null)
        {
            EditorUtility.DisplayDialog("❌ Player No Encontrado", 
                "No se encontró el GameObject del player.\n\n" +
                "Asegúrate de que tenga tag 'Player' o nombre 'Caballero'.", "OK");
            return;
        }
        
        string mensaje = $"Verificando: {player.name}\n\n";
        bool todoOk = true;
        
        // Verificar componentes necesarios
        if (player.GetComponent<EntityController>() == null)
        {
            mensaje += "❌ FALTA: EntityController\n";
            todoOk = false;
        }
        else
        {
            mensaje += "✅ EntityController\n";
        }
        
        if (player.GetComponent<EntityStats>() == null)
        {
            mensaje += "❌ FALTA: EntityStats\n";
            todoOk = false;
        }
        else
        {
            mensaje += "✅ EntityStats\n";
        }
        
        if (player.GetComponent<NavMeshAgent>() == null)
        {
            mensaje += "❌ FALTA: NavMeshAgent\n";
            player.AddComponent<NavMeshAgent>();
            mensaje += "  → AGREGADO automáticamente\n";
            todoOk = false;
        }
        else
        {
            mensaje += "✅ NavMeshAgent\n";
        }
        
        if (player.GetComponent<Movement.PlayerMovementController>() == null)
        {
            mensaje += "❌ FALTA: PlayerMovementController\n";
            player.AddComponent<Movement.PlayerMovementController>();
            mensaje += "  → AGREGADO automáticamente\n";
            todoOk = false;
        }
        else
        {
            mensaje += "✅ PlayerMovementController\n";
        }
        
        if (player.GetComponent<PlayerInitializer>() == null)
        {
            mensaje += "❌ FALTA: PlayerInitializer\n";
            player.AddComponent<PlayerInitializer>();
            mensaje += "  → AGREGADO automáticamente\n";
            todoOk = false;
        }
        else
        {
            mensaje += "✅ PlayerInitializer\n";
        }
        
        if (todoOk)
        {
            mensaje += "\n✅ Player está completamente configurado!";
        }
        else
        {
            mensaje += "\n⚠️ Algunos componentes fueron agregados o faltaban.";
            EditorUtility.SetDirty(player);
        }
        
        Debug.Log(mensaje);
        EditorUtility.DisplayDialog("Verificación del Player", mensaje, "OK");
    }
    
    private void LimpiezaCompleta()
    {
        Debug.Log("========== LIMPIEZA COMPLETA ==========");
        
        // 1. Limpiar cámara
        Debug.Log("\n1. Limpiando MainCamera...");
        LimpiarMainCamera();
        
        // 2. Verificar y arreglar player
        Debug.Log("\n2. Verificando Player...");
        VerificarPlayerCompleto();
        
        Debug.Log("\n========== LIMPIEZA FINALIZADA ==========");
        
        EditorUtility.DisplayDialog("✅ Limpieza Completa", 
            "La limpieza y verificación se completaron.\n\n" +
            "Revisa la Console para ver los detalles.", "OK");
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace World.ChunkSystem.Editor
{
    /// <summary>
    /// Herramienta para reparar problemas comunes del sistema de chunks.
    /// </summary>
    public class ChunkRepairTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private float targetChunkSize = 256f;
        private bool foundProblems = false;
        private string repairLog = "";
        
        [MenuItem("Tools/Chunk System/🔧 Reparar Chunks")]
        public static void ShowWindow()
        {
            var window = GetWindow<ChunkRepairTool>("Reparar Chunks");
            window.minSize = new Vector2(500, 400);
        }
        
        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("🔧 HERRAMIENTA DE REPARACIÓN", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Esta herramienta corrige problemas comunes del sistema de chunks", MessageType.Info);
            EditorGUILayout.Space(10);
            
            // Sección 1: Sincronizar ChunkSize
            DrawChunkSizeSection();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
            
            // Sección 2: Configurar ChunkLoader
            DrawChunkLoaderSection();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
            
            // Sección 3: Validar spawns
            DrawValidateSpawnsSection();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
            
            // Sección 4: Limpiar spawns sin EnemyData
            DrawCleanupSection();
            
            // Log de reparación
            if (!string.IsNullOrEmpty(repairLog))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("📋 LOG DE REPARACIÓN", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(repairLog, GUILayout.Height(150));
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawChunkSizeSection()
        {
            EditorGUILayout.LabelField("1️⃣ Sincronizar ChunkSize", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Si tus chunks fueron generados con un tamaño diferente al configurado en WorldChunkManager, habrá problemas de carga.", MessageType.Warning);
            
            targetChunkSize = EditorGUILayout.FloatField("ChunkSize correcto:", targetChunkSize);
            
            var manager = FindObjectOfType<WorldChunkManager>();
            if (manager != null)
            {
                EditorGUILayout.LabelField($"ChunkSize actual en WorldChunkManager: {manager.ChunkSize}");
                
                if (Mathf.Abs(manager.ChunkSize - targetChunkSize) > 0.1f)
                {
                    EditorGUILayout.HelpBox($"⚠️ DESINCRONIZADO: WorldChunkManager tiene {manager.ChunkSize} pero debería ser {targetChunkSize}", MessageType.Error);
                    
                    if (GUILayout.Button("🔧 Corregir ChunkSize en WorldChunkManager"))
                    {
                        FixChunkSize(manager);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("✅ ChunkSize correcto", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("❌ No hay WorldChunkManager en la escena", MessageType.Error);
                
                if (GUILayout.Button("➕ Crear WorldChunkManager"))
                {
                    CreateWorldChunkManager();
                }
            }
        }
        
        private void FixChunkSize(WorldChunkManager manager)
        {
            var so = new SerializedObject(manager);
            var chunkSizeProp = so.FindProperty("chunkSize");
            
            if (chunkSizeProp != null)
            {
                float oldSize = chunkSizeProp.floatValue;
                chunkSizeProp.floatValue = targetChunkSize;
                so.ApplyModifiedProperties();
                
                repairLog = $"✅ ChunkSize corregido: {oldSize} → {targetChunkSize}\n";
                repairLog += "⚠️ IMPORTANTE: Si ya tenías chunks cargados, reinicia el Play Mode\n";
                
                EditorUtility.SetDirty(manager);
                Debug.Log($"✅ ChunkSize corregido en WorldChunkManager: {oldSize} → {targetChunkSize}");
            }
            else
            {
                repairLog = "❌ Error: No se pudo encontrar la propiedad chunkSize\n";
            }
        }
        
        private void CreateWorldChunkManager()
        {
            GameObject managerGO = new GameObject("_WorldChunkManager");
            var manager = managerGO.AddComponent<WorldChunkManager>();
            
            // Configurar con SerializedObject para acceder a campos privados
            var so = new SerializedObject(manager);
            var chunkSizeProp = so.FindProperty("chunkSize");
            if (chunkSizeProp != null)
            {
                chunkSizeProp.floatValue = targetChunkSize;
            }
            so.ApplyModifiedProperties();
            
            Selection.activeGameObject = managerGO;
            
            repairLog = "✅ WorldChunkManager creado y configurado\n";
            repairLog += $"   ChunkSize: {targetChunkSize}\n";
            repairLog += "⚠️ IMPORTANTE: Asegúrate de tener un ChunkLoader también\n";
            
            Debug.Log("✅ WorldChunkManager creado con éxito");
        }
        
        private void DrawChunkLoaderSection()
        {
            EditorGUILayout.LabelField("2️⃣ Configurar ChunkLoader", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("El ChunkLoader es necesario para registrar los ChunkDataAssets en el WorldChunkManager.", MessageType.Info);
            
            var loader = FindObjectOfType<ChunkLoader>();
            
            if (loader == null)
            {
                EditorGUILayout.HelpBox("❌ No hay ChunkLoader en la escena", MessageType.Error);
                
                if (GUILayout.Button("➕ Crear ChunkLoader"))
                {
                    CreateChunkLoader();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✅ ChunkLoader encontrado", MessageType.Info);
                
                if (GUILayout.Button("🔄 Auto-Cargar Chunks desde Resources"))
                {
                    AutoLoadChunksInLoader(loader);
                }
                
                if (GUILayout.Button("📍 Ir a ChunkLoader"))
                {
                    Selection.activeGameObject = loader.gameObject;
                    EditorGUIUtility.PingObject(loader.gameObject);
                }
            }
        }
        
        private void CreateChunkLoader()
        {
            GameObject loaderGO = new GameObject("_ChunkLoader");
            var loader = loaderGO.AddComponent<ChunkLoader>();
            
            Selection.activeGameObject = loaderGO;
            
            repairLog = "✅ ChunkLoader creado\n";
            repairLog += "⚠️ SIGUIENTE PASO: Usa el botón 'Auto-Cargar Chunks desde Resources'\n";
            
            Debug.Log("✅ ChunkLoader creado con éxito");
        }
        
        private void AutoLoadChunksInLoader(ChunkLoader loader)
        {
            // Usar reflexión para llamar al método
            var method = typeof(ChunkLoader).GetMethod("AutoLoadFromResources");
            if (method != null)
            {
                method.Invoke(loader, null);
                
                repairLog = "✅ Chunks auto-cargados en el ChunkLoader\n";
                repairLog += "   Revisa el Inspector del ChunkLoader para ver los chunks detectados\n";
                
                Debug.Log("✅ Chunks auto-cargados en ChunkLoader");
            }
            else
            {
                repairLog = "❌ Error: No se pudo ejecutar AutoLoadFromResources\n";
            }
        }
        
        private void DrawValidateSpawnsSection()
        {
            EditorGUILayout.LabelField("3️⃣ Validar Spawns", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Escanea todos los chunks y detecta problemas con los spawns.", MessageType.Info);
            
            if (GUILayout.Button("🔍 Escanear Todos los Chunks"))
            {
                ValidateAllSpawns();
            }
        }
        
        private void ValidateAllSpawns()
        {
            string[] guids = AssetDatabase.FindAssets($"t:ChunkDataAsset");
            List<ChunkDataAsset> allChunks = new List<ChunkDataAsset>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ChunkDataAsset>(path);
                if (asset != null)
                {
                    allChunks.Add(asset);
                }
            }
            
            repairLog = "=== VALIDACIÓN DE SPAWNS ===\n\n";
            repairLog += $"📦 Total de chunks: {allChunks.Count}\n\n";
            
            int totalProblems = 0;
            int chunksWithProblems = 0;
            
            foreach (var chunk in allChunks)
            {
                int chunkProblems = 0;
                
                for (int i = 0; i < chunk.enemySpawns.Count; i++)
                {
                    var spawn = chunk.enemySpawns[i];
                    
                    // Problema 1: Sin EnemyData
                    if (spawn.enemyData == null)
                    {
                        if (chunkProblems == 0)
                        {
                            repairLog += $"⚠️ Chunk ({chunk.coordinates.x}, {chunk.coordinates.y}) - {chunk.name}:\n";
                        }
                        repairLog += $"  • Spawn {i}: ❌ Sin EnemyData asignado\n";
                        chunkProblems++;
                    }
                    
                    // Problema 2: Posición en (0,0,0)
                    if (spawn.spawnPosition == Vector3.zero)
                    {
                        if (chunkProblems == 0)
                        {
                            repairLog += $"⚠️ Chunk ({chunk.coordinates.x}, {chunk.coordinates.y}) - {chunk.name}:\n";
                        }
                        repairLog += $"  • Spawn {i}: ⚠️ Posición en origen (0,0,0)\n";
                        chunkProblems++;
                    }
                    
                    // Problema 3: Rotación inválida
                    if (float.IsNaN(spawn.spawnRotation.x) || float.IsNaN(spawn.spawnRotation.y) || 
                        float.IsNaN(spawn.spawnRotation.z) || float.IsNaN(spawn.spawnRotation.w))
                    {
                        if (chunkProblems == 0)
                        {
                            repairLog += $"⚠️ Chunk ({chunk.coordinates.x}, {chunk.coordinates.y}) - {chunk.name}:\n";
                        }
                        repairLog += $"  • Spawn {i}: ❌ Rotación inválida (NaN)\n";
                        chunkProblems++;
                    }
                }
                
                if (chunkProblems > 0)
                {
                    repairLog += $"  Total de problemas: {chunkProblems}\n\n";
                    chunksWithProblems++;
                    totalProblems += chunkProblems;
                }
            }
            
            if (totalProblems == 0)
            {
                repairLog += "✅ No se encontraron problemas\n";
            }
            else
            {
                repairLog += $"📊 Resumen:\n";
                repairLog += $"  • Chunks con problemas: {chunksWithProblems}\n";
                repairLog += $"  • Total de problemas: {totalProblems}\n\n";
                repairLog += "💡 Usa la sección '4️⃣ Limpiar Spawns' para corregir automáticamente\n";
            }
            
            foundProblems = totalProblems > 0;
            
            Debug.Log(repairLog);
        }
        
        private void DrawCleanupSection()
        {
            EditorGUILayout.LabelField("4️⃣ Limpiar Spawns Inválidos", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Elimina o corrige spawns con problemas.", MessageType.Info);
            
            if (GUILayout.Button("🧹 Eliminar Spawns sin EnemyData"))
            {
                RemoveInvalidSpawns();
            }
            
            if (GUILayout.Button("🔧 Reparar Rotaciones Inválidas"))
            {
                FixInvalidRotations();
            }
        }
        
        private void RemoveInvalidSpawns()
        {
            if (!EditorUtility.DisplayDialog("Confirmar", 
                "Esto eliminará todos los spawns que no tengan EnemyData asignado. ¿Continuar?", 
                "Sí", "Cancelar"))
            {
                return;
            }
            
            string[] guids = AssetDatabase.FindAssets($"t:ChunkDataAsset");
            int totalRemoved = 0;
            int chunksModified = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var chunk = AssetDatabase.LoadAssetAtPath<ChunkDataAsset>(path);
                
                if (chunk != null)
                {
                    int beforeCount = chunk.enemySpawns.Count;
                    chunk.enemySpawns.RemoveAll(spawn => spawn.enemyData == null);
                    int afterCount = chunk.enemySpawns.Count;
                    
                    if (beforeCount != afterCount)
                    {
                        EditorUtility.SetDirty(chunk);
                        totalRemoved += (beforeCount - afterCount);
                        chunksModified++;
                    }
                }
            }
            
            if (chunksModified > 0)
            {
                AssetDatabase.SaveAssets();
            }
            
            repairLog = $"✅ Limpieza completada:\n";
            repairLog += $"  • Spawns eliminados: {totalRemoved}\n";
            repairLog += $"  • Chunks modificados: {chunksModified}\n";
            
            Debug.Log($"✅ Limpieza completada: {totalRemoved} spawns eliminados de {chunksModified} chunks");
        }
        
        private void FixInvalidRotations()
        {
            string[] guids = AssetDatabase.FindAssets($"t:ChunkDataAsset");
            int totalFixed = 0;
            int chunksModified = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var chunk = AssetDatabase.LoadAssetAtPath<ChunkDataAsset>(path);
                
                if (chunk != null)
                {
                    bool modified = false;
                    
                    for (int i = 0; i < chunk.enemySpawns.Count; i++)
                    {
                        var spawn = chunk.enemySpawns[i];
                        
                        if (float.IsNaN(spawn.spawnRotation.x) || float.IsNaN(spawn.spawnRotation.y) || 
                            float.IsNaN(spawn.spawnRotation.z) || float.IsNaN(spawn.spawnRotation.w))
                        {
                            spawn.spawnRotation = Quaternion.identity;
                            chunk.enemySpawns[i] = spawn;
                            totalFixed++;
                            modified = true;
                        }
                    }
                    
                    if (modified)
                    {
                        EditorUtility.SetDirty(chunk);
                        chunksModified++;
                    }
                }
            }
            
            if (chunksModified > 0)
            {
                AssetDatabase.SaveAssets();
            }
            
            repairLog = $"✅ Rotaciones reparadas:\n";
            repairLog += $"  • Spawns corregidos: {totalFixed}\n";
            repairLog += $"  • Chunks modificados: {chunksModified}\n";
            
            Debug.Log($"✅ Reparación completada: {totalFixed} rotaciones corregidas en {chunksModified} chunks");
        }
    }
}

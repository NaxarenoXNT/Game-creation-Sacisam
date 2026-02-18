using UnityEngine;
using UnityEditor;
using World.ChunkSystem;

namespace World.ChunkSystem.Editor
{
    /// <summary>
    /// Editor personalizado para ChunkSpawnTemplate con preview visual.
    /// </summary>
    [CustomEditor(typeof(ChunkSpawnTemplate))]
    public class ChunkSpawnTemplateEditor : UnityEditor.Editor
    {
        private ChunkSpawnTemplate template;
        private Vector2 scrollPosition;
        
        void OnEnable()
        {
            template = (ChunkSpawnTemplate)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Header personalizado
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📋 Plantilla de Spawns", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("Define cómo se distribuyen los enemigos en los chunks", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Dibujar propiedades por defecto
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // Estadísticas
            DrawStatistics();
            
            EditorGUILayout.Space(10);
            
            // Botón de preview
            if (GUILayout.Button("🔍 Preview Distribución", GUILayout.Height(35)))
            {
                PreviewDistribution();
            }
            
            EditorGUILayout.Space(5);
            
            // Botón de test
            if (GUILayout.Button("🧪 Test en Chunk (0,0)", GUILayout.Height(30)))
            {
                TestTemplate();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📊 Estadísticas", EditorStyles.boldLabel);
            
            int totalEnemies = 0;
            int uniqueEnemies = 0;
            int typesOfEnemies = 0;
            
            foreach (var def in template.spawnDefinitions)
            {
                if (def.enemyData != null)
                {
                    totalEnemies += def.count;
                    typesOfEnemies++;
                    if (def.isUnique)
                        uniqueEnemies += def.count;
                }
            }
            
            EditorGUILayout.LabelField($"👹 Total de enemigos: {totalEnemies}");
            EditorGUILayout.LabelField($"🎭 Tipos diferentes: {typesOfEnemies}");
            EditorGUILayout.LabelField($"⭐ Enemigos únicos: {uniqueEnemies}");
            EditorGUILayout.LabelField($"📐 Distribución: {template.distributionType}");
            EditorGUILayout.LabelField($"🚶 Estado IA por defecto: {template.defaultAIState}");
            
            if (template.autoGenerateWaypoints)
            {
                EditorGUILayout.LabelField($"🗺️ Waypoints automáticos: {template.waypointsPerEnemy} por enemigo");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void PreviewDistribution()
        {
            if (template.spawnDefinitions.Count == 0)
            {
                EditorUtility.DisplayDialog("Sin Definiciones", 
                    "Agrega al menos una definición de spawn para ver el preview.", "Ok");
                return;
            }
            
            // Generar configuraciones de prueba
            var configs = template.GenerateSpawnConfigs(Vector2Int.zero, 256);
            
            string report = $"=== PREVIEW DE DISTRIBUCIÓN ===\n\n";
            report += $"Plantilla: {template.templateName}\n";
            report += $"Tipo: {template.distributionType}\n";
            report += $"Total de spawns generados: {configs.Count}\n\n";
            
            report += "POSICIONES:\n";
            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                report += $"{i + 1}. {config.enemyData?.nombreEnemigo ?? "Sin asignar"} en {config.spawnPosition}\n";
                
                if (config.patrolWaypoints != null && config.patrolWaypoints.Count > 0)
                {
                    report += $"   └─ {config.patrolWaypoints.Count} waypoints\n";
                }
            }
            
            Debug.Log(report);
            EditorUtility.DisplayDialog("✅ Preview Generado", 
                $"Se generó un preview en la consola.\n\n" +
                $"Total de enemigos: {configs.Count}\n" +
                $"Revisa la consola para ver las posiciones detalladas.", "Ok");
        }
        
        private void TestTemplate()
        {
            if (template.spawnDefinitions.Count == 0)
            {
                EditorUtility.DisplayDialog("Sin Definiciones", 
                    "Agrega al menos una definición de spawn antes de testear.", "Ok");
                return;
            }
            
            // Verificar si hay EnemigoData sin asignar
            int unassigned = 0;
            foreach (var def in template.spawnDefinitions)
            {
                if (def.enemyData == null) unassigned++;
            }
            
            if (unassigned > 0)
            {
                EditorUtility.DisplayDialog("⚠️ Advertencia", 
                    $"Hay {unassigned} definiciones sin EnemigoData asignado.\n" +
                    "Asigna los datos antes de usar la plantilla.", "Ok");
                return;
            }
            
            // Generar test
            var configs = template.GenerateSpawnConfigs(Vector2Int.zero, 256);
            
            EditorUtility.DisplayDialog("✅ Test Exitoso", 
                $"La plantilla generó correctamente {configs.Count} configuraciones de spawn.\n\n" +
                "Detalles en la consola.", "Ok");
            
            Debug.Log($"<color=green>✅ Test exitoso para '{template.name}':</color> {configs.Count} spawns generados");
        }
        
        // Preview en Scene View
        private void OnSceneGUI()
        {
            if (!template) return;
            
            // Dibujar preview en Scene View si está seleccionado
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 250, 100), EditorStyles.helpBox);
            GUILayout.Label($"📋 {template.templateName}", EditorStyles.boldLabel);
            
            int totalEnemies = 0;
            foreach (var def in template.spawnDefinitions)
            {
                if (def.enemyData != null)
                    totalEnemies += def.count;
            }
            
            GUILayout.Label($"👹 {totalEnemies} enemigos");
            GUILayout.Label($"📐 {template.distributionType}");
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}

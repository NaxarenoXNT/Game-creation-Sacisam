using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using World.ChunkSystem;

namespace World.ChunkSystem.Editor
{
    /// <summary>
    /// Editor visual para configurar enemigos y waypoints en chunks.
    /// Permite pintar enemigos, configurar patrullas y comportamientos.
    /// </summary>
    [CustomEditor(typeof(ChunkDataAsset))]
    public class ChunkDataAssetEditor : UnityEditor.Editor
    {
        private ChunkDataAsset chunk;
        private int selectedSpawnIndex = -1;
        
        // Paint Mode
        private bool paintMode = false;
        private EnemigoData paintEnemyData;
        private EnemyAIState paintAIState = EnemyAIState.Patrolling;
        private bool paintAutoWaypoints = true;
        
        // Waypoint Mode
        private bool waypointMode = false;
        
        // Delete Mode
        private bool deleteMode = false;
        
        // Visualization
        private bool showSpawnLabels = true;
        private bool showWaypoints = true;
        private float spawnIconSize = 1f;
        
        private void OnEnable()
        {
            chunk = (ChunkDataAsset)target;
            
            // VALIDAR TODOS LOS SPAWNS AL CARGAR
            ValidateAllSpawns();
            
            // Suscribirse a SceneView para dibujar en la escena
            // (OnSceneGUI no es confiable para editores de ScriptableObjects)
            SceneView.duringSceneGui -= OnSceneGUIHandler;
            SceneView.duringSceneGui += OnSceneGUIHandler;
        }
        
        private void OnDisable()
        {
            // Desuscribirse para evitar memory leaks
            SceneView.duringSceneGui -= OnSceneGUIHandler;
        }
        
        private void OnSceneGUIHandler(SceneView sceneView)
        {
            if (chunk == null) return;
            
            // CRITICAL: Usar un hint estable para que el controlId sea CONSISTENTE
            // entre los pases Layout, Repaint y MouseDown del IMGUI.
            // Sin hint, GetControlID devuelve IDs distintos en cada pase y
            // AddDefaultControl no funciona.
            int controlId = GUIUtility.GetControlID("ChunkPaintTool".GetHashCode(), FocusType.Passive);
            
            // Cuando estamos en un modo interactivo, reclamar el control por defecto.
            // Esto PREVIENE que las herramientas de Unity (selección, terreno)
            // consuman el evento antes que nosotros.
            if (paintMode || waypointMode || deleteMode)
            {
                HandleUtility.AddDefaultControl(controlId);
            }
            
            // Calcular posición base del chunk
            // Obtener tamaño del WorldChunkManager si existe, sino usar 256 por defecto
            float chunkSize = WorldChunkManager.Instance != null ? WorldChunkManager.Instance.ChunkSize : 256f;
            
            Vector3 chunkWorldPos = new Vector3(
                chunk.coordinates.x * chunkSize, 
                0, 
                chunk.coordinates.y * chunkSize
            );
            
            // Dibujar bounds del chunk
            Handles.color = chunk.gizmoColor;
            Vector3 center = chunkWorldPos + new Vector3(chunkSize / 2, 0, chunkSize / 2);
            Handles.DrawWireCube(center, new Vector3(chunkSize, 2, chunkSize));
            
            // Dibujar grid
            DrawChunkGrid(chunkWorldPos, chunkSize);
            
            // Dibujar spawns
            for (int i = 0; i < chunk.enemySpawns.Count; i++)
            {
                DrawSpawn(chunk.enemySpawns[i], i);
            }
            
            // Manejar interacción según el modo (pasar controlId)
            HandleSceneInput(sceneView, chunkWorldPos, chunkSize, controlId);
            
            // Instrucciones en pantalla
            DrawModeInstructions(sceneView);
        }
        
        /// <summary>
        /// Valida todos los spawns del chunk para evitar errores.
        /// </summary>
        private void ValidateAllSpawns()
        {
            if (chunk == null || chunk.enemySpawns == null) return;
            
            bool needsSave = false;
            
            foreach (var spawn in chunk.enemySpawns)
            {
                // Validar quaternion
                if (float.IsNaN(spawn.spawnRotation.x) || spawn.spawnRotation == new Quaternion(0, 0, 0, 0))
                {
                    spawn.spawnRotation = Quaternion.identity;
                    needsSave = true;
                }
            }
            
            if (needsSave)
            {
                EditorUtility.SetDirty(chunk);
                Debug.LogWarning($"⚠️ Se corrigieron quaternions inválidos en {chunk.name}");
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // ========== HEADER ==========
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📦 Chunk Editor - {chunk.name}", EditorStyles.largeLabel);
            EditorGUILayout.LabelField($"Coordenadas: ({chunk.coordinates.x}, {chunk.coordinates.y})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Enemigos: {chunk.enemySpawns.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // ========== PAINT MODE ==========
            DrawPaintModeSection();
            
            EditorGUILayout.Space(10);
            
            // ========== WAYPOINT MODE ==========
            DrawWaypointModeSection();
            
            EditorGUILayout.Space(10);
            
            // ========== DELETE MODE ==========
            DrawDeleteModeSection();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
            
            // ========== HERRAMIENTAS RÁPIDAS ==========
            DrawQuickToolsSection();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
            
            // ========== LISTA DE SPAWNS ==========
            DrawSpawnListSection();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
            
            // ========== VISUALIZACIÓN ==========
            DrawVisualizationSection();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);
            
            // ========== PROPIEDADES POR DEFECTO ==========
            EditorGUILayout.LabelField("⚙️ Propiedades del Chunk", EditorStyles.boldLabel);
            DrawDefaultInspector();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawPaintModeSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUI.backgroundColor = paintMode ? new Color(0.5f, 1f, 0.5f) : Color.white;
            if (GUILayout.Button(paintMode ? "🎨 MODO PINTAR: ACTIVO" : "🎨 Modo Pintar", GUILayout.Height(35)))
            {
                paintMode = !paintMode;
                if (paintMode)
                {
                    waypointMode = false;
                    deleteMode = false;
                }
            }
            GUI.backgroundColor = Color.white;
            
            if (paintMode)
            {
                EditorGUILayout.HelpBox("Click en el Scene View para colocar enemigos", MessageType.Info);
                
                paintEnemyData = (EnemigoData)EditorGUILayout.ObjectField(
                    "Enemigo a Pintar", 
                    paintEnemyData, 
                    typeof(EnemigoData), 
                    false
                );
                
                paintAIState = (EnemyAIState)EditorGUILayout.EnumPopup("Estado IA", paintAIState);
                paintAutoWaypoints = EditorGUILayout.Toggle("Auto-generar Waypoints", paintAutoWaypoints);
                
                if (paintEnemyData == null)
                {
                    EditorGUILayout.HelpBox("⚠️ Asigna un EnemigoData para pintar", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawWaypointModeSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUI.backgroundColor = waypointMode ? new Color(1f, 1f, 0.5f) : Color.white;
            if (GUILayout.Button(waypointMode ? "🗺️ MODO WAYPOINT: ACTIVO" : "🗺️ Modo Waypoints", GUILayout.Height(35)))
            {
                waypointMode = !waypointMode;
                if (waypointMode)
                {
                    paintMode = false;
                    deleteMode = false;
                }
            }
            GUI.backgroundColor = Color.white;
            
            if (waypointMode)
            {
                if (selectedSpawnIndex >= 0 && selectedSpawnIndex < chunk.enemySpawns.Count)
                {
                    var spawn = chunk.enemySpawns[selectedSpawnIndex];
                    string enemyName = spawn.enemyData?.nombreEnemigo ?? "Sin asignar";
                    
                    EditorGUILayout.HelpBox($"✅ Editando waypoints de: #{selectedSpawnIndex} ({enemyName})\nClick en Scene View para agregar waypoints", MessageType.Info);
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"📍 Total waypoints: {spawn.patrolWaypoints.Count}", EditorStyles.boldLabel);
                    
                    spawn.patrolBehavior = (PatrolBehavior)EditorGUILayout.EnumPopup("Comportamiento", spawn.patrolBehavior);
                    
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Limpiar Waypoints", GUILayout.Height(25)))
                    {
                        if (EditorUtility.DisplayDialog("Confirmar", "¿Eliminar todos los waypoints?", "Sí", "No"))
                        {
                            spawn.patrolWaypoints.Clear();
                            EditorUtility.SetDirty(chunk);
                        }
                    }
                    
                    if (spawn.patrolWaypoints.Count > 0 && GUILayout.Button("Borrar Último", GUILayout.Height(25)))
                    {
                        spawn.patrolWaypoints.RemoveAt(spawn.patrolWaypoints.Count - 1);
                        EditorUtility.SetDirty(chunk);
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("⚠️ PRIMERO DEBES SELECCIONAR UN ENEMIGO\n\n1. Ve a 'Lista de Enemigos' abajo\n2. Click en el botón #0, #1, etc.\n3. Luego podrás agregar waypoints", MessageType.Warning);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDeleteModeSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUI.backgroundColor = deleteMode ? new Color(1f, 0.5f, 0.5f) : Color.white;
            if (GUILayout.Button(deleteMode ? "🗑️ MODO BORRAR: ACTIVO" : "🗑️ Modo Borrar", GUILayout.Height(35)))
            {
                deleteMode = !deleteMode;
                if (deleteMode)
                {
                    paintMode = false;
                    waypointMode = false;
                }
            }
            GUI.backgroundColor = Color.white;
            
            if (deleteMode)
            {
                EditorGUILayout.HelpBox("Click sobre un enemigo en Scene View para eliminarlo", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawQuickToolsSection()
        {
            EditorGUILayout.LabelField("🔧 Herramientas Rápidas", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Auto-Grid"))
            {
                AutoPositionSpawns();
            }
            
            if (GUILayout.Button("Círculo"))
            {
                AutoPositionCircle();
            }
            
            if (GUILayout.Button("Línea"))
            {
                AutoPositionLine();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Generar Waypoints"))
            {
                AutoGenerateAllWaypoints();
            }
            
            if (GUILayout.Button("Limpiar Todo"))
            {
                if (EditorUtility.DisplayDialog("Confirmar", "¿Borrar todos los spawns?", "Sí", "No"))
                {
                    chunk.enemySpawns.Clear();
                    selectedSpawnIndex = -1;
                    EditorUtility.SetDirty(chunk);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSpawnListSection()
        {
            EditorGUILayout.LabelField($"📋 Lista de Enemigos ({chunk.enemySpawns.Count})", EditorStyles.boldLabel);
            
            if (chunk.enemySpawns.Count == 0)
            {
                EditorGUILayout.HelpBox("No hay enemigos. Usa el Modo Pintar para agregar.", MessageType.Info);
                return;
            }
            
            for (int i = 0; i < chunk.enemySpawns.Count; i++)
            {
                var spawn = chunk.enemySpawns[i];
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                
                // Botón de selección
                GUI.backgroundColor = selectedSpawnIndex == i ? new Color(0.5f, 1f, 0.5f) : Color.white;
                if (GUILayout.Button($"#{i}", GUILayout.Width(40)))
                {
                    selectedSpawnIndex = i;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
                GUI.backgroundColor = Color.white;
                
                // Información
                string enemyName = spawn.enemyData != null ? spawn.enemyData.nombreEnemigo : "Sin Data";
                EditorGUILayout.LabelField($"{enemyName} | {spawn.initialAIState} | WP: {spawn.patrolWaypoints.Count}");
                
                // Botón de eliminar
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    chunk.enemySpawns.RemoveAt(i);
                    if (selectedSpawnIndex == i) selectedSpawnIndex = -1;
                    else if (selectedSpawnIndex > i) selectedSpawnIndex--;
                    EditorUtility.SetDirty(chunk);
                    break;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
                
                // Si está seleccionado, mostrar configuración rápida
                if (selectedSpawnIndex == i)
                {
                    EditorGUI.indentLevel++;
                    
                    spawn.enemyData = (EnemigoData)EditorGUILayout.ObjectField("Enemy Data", spawn.enemyData, typeof(EnemigoData), false);
                    spawn.initialAIState = (EnemyAIState)EditorGUILayout.EnumPopup("Estado IA", spawn.initialAIState);
                    spawn.patrolBehavior = (PatrolBehavior)EditorGUILayout.EnumPopup("Comportamiento", spawn.patrolBehavior);
                    spawn.isUnique = EditorGUILayout.Toggle("Es Único (Boss)", spawn.isUnique);
                    
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("📍 Editar Waypoints", GUILayout.Height(30)))
                    {
                        waypointMode = true;
                        paintMode = false;
                        deleteMode = false;
                        SceneView.lastActiveSceneView.Frame(new Bounds(spawn.spawnPosition, Vector3.one * 20), false);
                    }
                    
                    if (GUILayout.Button("🔍 Ver en Scene", GUILayout.Height(30)))
                    {
                        SceneView.lastActiveSceneView.Frame(new Bounds(spawn.spawnPosition, Vector3.one * 10), false);
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawVisualizationSection()
        {
            EditorGUILayout.LabelField("👁️ Visualización", EditorStyles.boldLabel);
            
            showSpawnLabels = EditorGUILayout.Toggle("Mostrar Labels", showSpawnLabels);
            showWaypoints = EditorGUILayout.Toggle("Mostrar Waypoints", showWaypoints);
            spawnIconSize = EditorGUILayout.Slider("Tamaño Iconos", spawnIconSize, 0.5f, 3f);
        }
        
        private void DrawChunkGrid(Vector3 basePos, float size)
        {
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            int gridLines = 10;
            float cellSize = size / gridLines;
            
            for (int i = 0; i <= gridLines; i++)
            {
                float offset = i * cellSize;
                
                // Líneas verticales
                Handles.DrawLine(
                    basePos + new Vector3(offset, 0, 0),
                    basePos + new Vector3(offset, 0, size)
                );
                
                // Líneas horizontales
                Handles.DrawLine(
                    basePos + new Vector3(0, 0, offset),
                    basePos + new Vector3(size, 0, offset)
                );
            }
        }
        
        private void DrawSpawn(EnemySpawnConfig spawn, int index)
        {
            bool isSelected = selectedSpawnIndex == index;
            
            // VALIDAR Y ARREGLAR QUATERNION INVÁLIDO
            if (float.IsNaN(spawn.spawnRotation.x) || spawn.spawnRotation == new Quaternion(0, 0, 0, 0))
            {
                spawn.spawnRotation = Quaternion.identity;
                EditorUtility.SetDirty(chunk);
            }
            
            // Color según estado
            if (deleteMode)
                Handles.color = Color.red;
            else if (isSelected)
                Handles.color = Color.green;
            else
                Handles.color = Color.cyan;
            
            // Dibujar icono
            Handles.SphereHandleCap(0, spawn.spawnPosition, Quaternion.identity, spawnIconSize, EventType.Repaint);
            
            // Dibujar dirección
            Handles.color = Color.blue;
            Vector3 forward = spawn.spawnRotation * Vector3.forward * 2f;
            Handles.DrawLine(spawn.spawnPosition, spawn.spawnPosition + forward);
            
            // Asegurar que el quaternion está normalizado antes de usarlo
            Quaternion safeRotation = spawn.spawnRotation;
            float magnitude = Mathf.Sqrt(safeRotation.x * safeRotation.x + safeRotation.y * safeRotation.y + 
                                        safeRotation.z * safeRotation.z + safeRotation.w * safeRotation.w);
            if (magnitude < 0.9f) // Si no está normalizado
                safeRotation = Quaternion.identity;
            
            Handles.ConeHandleCap(0, spawn.spawnPosition + forward, safeRotation, 0.5f, EventType.Repaint);
            
            // Label
            if (showSpawnLabels)
            {
                string label = spawn.enemyData != null ? spawn.enemyData.nombreEnemigo : $"Spawn {index}";
                Handles.Label(spawn.spawnPosition + Vector3.up * 2, 
                    $"{index}: {label}\n{spawn.initialAIState}",
                    EditorStyles.whiteLabel);
            }
            
            // Handle de posición (solo si está seleccionado y no estamos en un modo especial)
            if (isSelected && !paintMode && !waypointMode && !deleteMode)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(spawn.spawnPosition, spawn.spawnRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(chunk, "Move Spawn");
                    spawn.spawnPosition = newPos;
                    EditorUtility.SetDirty(chunk);
                }
                
                // Handle de rotación
                EditorGUI.BeginChangeCheck();
                Quaternion newRot = Handles.RotationHandle(spawn.spawnRotation, spawn.spawnPosition);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(chunk, "Rotate Spawn");
                    spawn.spawnRotation = newRot;
                    EditorUtility.SetDirty(chunk);
                }
            }
            
            // Dibujar waypoints
            if (showWaypoints && (isSelected || !waypointMode))
            {
                DrawWaypoints(spawn, isSelected);
            }
        }
        
        private void DrawWaypoints(EnemySpawnConfig spawn, bool isSelected)
        {
            if (spawn.patrolWaypoints.Count == 0) return;
            
            Handles.color = isSelected ? Color.yellow : new Color(1f, 1f, 0f, 0.5f);
            
            // Línea desde spawn a primer waypoint
            Handles.DrawDottedLine(spawn.spawnPosition, spawn.patrolWaypoints[0], 2f);
            
            // Líneas entre waypoints
            for (int i = 0; i < spawn.patrolWaypoints.Count - 1; i++)
            {
                Handles.DrawLine(spawn.patrolWaypoints[i], spawn.patrolWaypoints[i + 1]);
            }
            
            // Línea de cierre si es Loop
            if (spawn.patrolBehavior == PatrolBehavior.Loop && spawn.patrolWaypoints.Count > 2)
            {
                Handles.DrawDottedLine(
                    spawn.patrolWaypoints[spawn.patrolWaypoints.Count - 1], 
                    spawn.patrolWaypoints[0], 
                    2f
                );
            }
            
            // Dibujar cada waypoint
            for (int i = 0; i < spawn.patrolWaypoints.Count; i++)
            {
                Vector3 wp = spawn.patrolWaypoints[i];
                
                // Tamaño variable: el primero es más grande
                float wpSize = i == 0 ? 0.8f : 0.5f;
                
                // Icono
                Handles.color = isSelected ? Color.yellow : new Color(1f, 1f, 0f, 0.5f);
                Handles.SphereHandleCap(0, wp, Quaternion.identity, wpSize, EventType.Repaint);
                
                // Label con número (siempre visible para waypoints)
                GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteBoldLabel);
                labelStyle.fontSize = 14;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.normal.textColor = isSelected ? Color.yellow : new Color(1f, 1f, 0f, 0.8f);
                
                Handles.Label(wp + Vector3.up * 1.5f, $"WP{i}", labelStyle);
                
                // Handle de posición (solo si el spawn está seleccionado)
                if (isSelected && waypointMode)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWP = Handles.PositionHandle(wp, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(chunk, "Move Waypoint");
                        spawn.patrolWaypoints[i] = newWP;
                        EditorUtility.SetDirty(chunk);
                    }
                }
            }
        }
        
        private void HandleSceneInput(SceneView sceneView, Vector3 chunkBasePos, float chunkSize, int controlId)
        {
            Event e = Event.current;
            
            // No procesar si no estamos en ningún modo interactivo
            if (!paintMode && !waypointMode && !deleteMode) return;
            
            // Alt+click = rotar cámara, no interceptar
            if (e.alt) return;
            
            // CRITICAL: Usar GetTypeForControl en vez de e.type.
            // e.type puede ser EventType.Used si Unity ya consumió el evento.
            // GetTypeForControl devuelve el tipo REAL para nuestro control registrado
            // con AddDefaultControl, incluso si otros controles lo ignoraron.
            EventType eventForControl = e.GetTypeForControl(controlId);
            
            if (eventForControl == EventType.MouseDown && e.button == 0)
            {
                // Obtener punto de impacto en el mundo
                Vector3 hitPoint;
                bool gotHit = false;
                
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                
                // PRIMERO intentar Physics.Raycast (impacta contra el terreno real)
                // Esto coloca los enemigos SOBRE la superficie del terreno
                if (Physics.Raycast(ray, out RaycastHit physicsHit, 5000f))
                {
                    hitPoint = physicsHit.point;
                    gotHit = true;
                }
                else
                {
                    // FALLBACK: plano matemático en Y=0 (si no hay terreno)
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    if (groundPlane.Raycast(ray, out float enter))
                    {
                        hitPoint = ray.GetPoint(enter);
                        gotHit = true;
                    }
                    else
                    {
                        hitPoint = Vector3.zero;
                    }
                }
                
                if (gotHit && IsPointInChunk(hitPoint, chunkBasePos, chunkSize))
                {
                    if (paintMode)
                    {
                        HandlePaintClick(hitPoint);
                    }
                    else if (waypointMode)
                    {
                        HandleWaypointClick(hitPoint);
                    }
                    else if (deleteMode)
                    {
                        HandleDeleteClick(hitPoint);
                    }
                    
                    // Reclamar hotControl para que Unity no procese el evento
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
            }
            else if (eventForControl == EventType.MouseUp && e.button == 0)
            {
                // Liberar hotControl al soltar el botón
                if (GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
            }
        }
        
        private bool IsPointInChunk(Vector3 point, Vector3 chunkBasePos, float chunkSize)
        {
            return point.x >= chunkBasePos.x && point.x <= chunkBasePos.x + chunkSize &&
                   point.z >= chunkBasePos.z && point.z <= chunkBasePos.z + chunkSize;
        }
        
        private void HandlePaintClick(Vector3 position)
        {
            if (paintEnemyData == null)
            {
                Debug.LogWarning("⚠️ Asigna un EnemigoData antes de pintar");
                return;
            }
            
            // Generar rotación válida
            float randomYRotation = Random.Range(0f, 360f);
            Quaternion rotation = Quaternion.Euler(0, randomYRotation, 0);
            
            // VALIDAR que el quaternion sea válido
            if (float.IsNaN(rotation.x) || rotation == new Quaternion(0, 0, 0, 0))
            {
                rotation = Quaternion.identity;
            }
            
            var newSpawn = new EnemySpawnConfig
            {
                spawnId = $"{chunk.name}_spawn_{chunk.enemySpawns.Count}",
                enemyData = paintEnemyData,
                spawnPosition = position,
                spawnRotation = rotation,
                initialAIState = paintAIState,
                patrolBehavior = PatrolBehavior.Loop
            };
            
            // Auto-generar waypoints si está habilitado
            if (paintAutoWaypoints && paintAIState == EnemyAIState.Patrolling)
            {
                newSpawn.patrolWaypoints = GenerateCircularWaypoints(position, 10f, 4);
            }
            
            Undo.RecordObject(chunk, "Paint Enemy");
            chunk.enemySpawns.Add(newSpawn);
            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
            Repaint();
            
            Debug.Log($"✅ {paintEnemyData.nombreEnemigo} colocado en {position} con rotación {randomYRotation}°");
        }
        
        private void HandleWaypointClick(Vector3 position)
        {
            if (selectedSpawnIndex < 0 || selectedSpawnIndex >= chunk.enemySpawns.Count)
            {
                EditorUtility.DisplayDialog("⚠️ Sin selección", 
                    "Primero selecciona un enemigo de la lista en el Inspector.\n\n" +
                    "1. Ve a la sección 'Lista de Enemigos'\n" +
                    "2. Click en el botón #0, #1, etc.\n" +
                    "3. Luego podrás agregar waypoints", 
                    "Entendido");
                return;
            }
            
            var spawn = chunk.enemySpawns[selectedSpawnIndex];
            
            Undo.RecordObject(chunk, "Add Waypoint");
            spawn.patrolWaypoints.Add(position);
            EditorUtility.SetDirty(chunk);
            SceneView.RepaintAll();
            Repaint();
            
            Debug.Log($"✅ Waypoint {spawn.patrolWaypoints.Count} agregado a #{selectedSpawnIndex}");
        }
        
        private void HandleDeleteClick(Vector3 clickPosition)
        {
            // Buscar el spawn más cercano al click
            int closestIndex = -1;
            float closestDist = float.MaxValue;
            
            for (int i = 0; i < chunk.enemySpawns.Count; i++)
            {
                float dist = Vector3.Distance(clickPosition, chunk.enemySpawns[i].spawnPosition);
                if (dist < 2f && dist < closestDist) // Radio de 2 metros
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }
            
            if (closestIndex >= 0)
            {
                string enemyName = chunk.enemySpawns[closestIndex].enemyData?.nombreEnemigo ?? "Spawn";
                
                Undo.RecordObject(chunk, "Delete Spawn");
                chunk.enemySpawns.RemoveAt(closestIndex);
                
                if (selectedSpawnIndex == closestIndex)
                    selectedSpawnIndex = -1;
                else if (selectedSpawnIndex > closestIndex)
                    selectedSpawnIndex--;
                
                EditorUtility.SetDirty(chunk);
                SceneView.RepaintAll();
                Repaint();
                Debug.Log($"🗑️ {enemyName} eliminado");
            }
        }
        
        private void HandleSelectClick(Vector3 clickPosition)
        {
            // Buscar el spawn más cercano al click
            int closestIndex = -1;
            float closestDist = float.MaxValue;
            
            for (int i = 0; i < chunk.enemySpawns.Count; i++)
            {
                float dist = Vector3.Distance(clickPosition, chunk.enemySpawns[i].spawnPosition);
                if (dist < 2f && dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }
            
            if (closestIndex >= 0)
            {
                selectedSpawnIndex = closestIndex;
                Repaint();
            }
        }
        
        private void DrawModeInstructions(SceneView sceneView)
        {
            Handles.BeginGUI();
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 120), EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = Color.white;
            GUILayout.Label($"📦 {chunk.name}", titleStyle);
            
            GUIStyle infoStyle = new GUIStyle(EditorStyles.label);
            infoStyle.normal.textColor = Color.white;
            
            if (paintMode)
            {
                GUILayout.Label("🎨 MODO PINTAR", titleStyle);
                GUILayout.Label("Click para colocar enemigos", infoStyle);
                if (paintEnemyData != null)
                    GUILayout.Label($"Pintando: {paintEnemyData.nombreEnemigo}", infoStyle);
            }
            else if (waypointMode)
            {
                GUILayout.Label("🗺️ MODO WAYPOINT", titleStyle);
                if (selectedSpawnIndex >= 0 && selectedSpawnIndex < chunk.enemySpawns.Count)
                {
                    var spawn = chunk.enemySpawns[selectedSpawnIndex];
                    string enemyName = spawn.enemyData?.nombreEnemigo ?? "Spawn";
                    GUILayout.Label("Click para agregar waypoints", infoStyle);
                    GUILayout.Label($"Editando: #{selectedSpawnIndex} ({enemyName})", infoStyle);
                    GUILayout.Label($"Waypoints: {spawn.patrolWaypoints.Count}", infoStyle);
                }
                else
                {
                    GUILayout.Label("⚠️ SELECCIONA UN ENEMIGO", titleStyle);
                    GUILayout.Label("Usa la lista del Inspector", infoStyle);
                }
            }
            else if (deleteMode)
            {
                GUILayout.Label("🗑️ MODO BORRAR", titleStyle);
                GUILayout.Label("Click sobre enemigo para eliminar", infoStyle);
            }
            else
            {
                GUILayout.Label($"Enemigos: {chunk.enemySpawns.Count}", infoStyle);
                if (selectedSpawnIndex >= 0)
                    GUILayout.Label($"Seleccionado: #{selectedSpawnIndex}", infoStyle);
            }
            
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
        
        // ========== HELPER FUNCTIONS ==========
        
        private void CreateSpawnFromSelection()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Selecciona un GameObject en la escena", "OK");
                return;
            }
            
            var go = Selection.activeGameObject;
            
            var newSpawn = new EnemySpawnConfig
            {
                spawnId = $"{chunk.name}_spawn_{chunk.enemySpawns.Count}",
                spawnPosition = go.transform.position,
                spawnRotation = go.transform.rotation
            };
            
            chunk.enemySpawns.Add(newSpawn);
            EditorUtility.SetDirty(chunk);
            
            Debug.Log($"✅ Spawn creado en {go.transform.position}");
        }
        
        private void AddWaypointAtSceneView(EnemySpawnConfig spawn)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                Vector3 position = sceneView.camera.transform.position + sceneView.camera.transform.forward * 10f;
                position.y = 0;
                
                spawn.patrolWaypoints.Add(position);
                EditorUtility.SetDirty(chunk);
                
                Debug.Log($"✅ Waypoint agregado en {position}");
            }
        }
        
        private void AutoPositionSpawns()
        {
            if (chunk.enemySpawns.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No hay spawns para posicionar", "OK");
                return;
            }
            
            Vector3 chunkCenter = new Vector3(
                chunk.coordinates.x * 256 + 128, 
                0, 
                chunk.coordinates.y * 256 + 128
            );
            
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(chunk.enemySpawns.Count));
            float cellSize = 220f / gridSize; // 220m usable area
            float startOffset = -110f; // Start from corner
            
            for (int i = 0; i < chunk.enemySpawns.Count; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;
                
                Vector3 position = chunkCenter + new Vector3(
                    startOffset + x * cellSize + cellSize / 2,
                    0,
                    startOffset + y * cellSize + cellSize / 2
                );
                
                chunk.enemySpawns[i].spawnPosition = position;
            }
            
            EditorUtility.SetDirty(chunk);
            Debug.Log($"✅ {chunk.enemySpawns.Count} spawns posicionados en grid");
        }
        
        private void AutoPositionCircle()
        {
            if (chunk.enemySpawns.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No hay spawns para posicionar", "OK");
                return;
            }
            
            Vector3 chunkCenter = new Vector3(
                chunk.coordinates.x * 256 + 128, 
                0, 
                chunk.coordinates.y * 256 + 128
            );
            
            float radius = 90f;
            float angleStep = 360f / chunk.enemySpawns.Count;
            
            for (int i = 0; i < chunk.enemySpawns.Count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                chunk.enemySpawns[i].spawnPosition = chunkCenter + offset;
                chunk.enemySpawns[i].spawnRotation = Quaternion.LookRotation(offset.normalized);
            }
            
            EditorUtility.SetDirty(chunk);
            Debug.Log($"✅ {chunk.enemySpawns.Count} spawns posicionados en círculo");
        }
        
        private void AutoPositionLine()
        {
            if (chunk.enemySpawns.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No hay spawns para posicionar", "OK");
                return;
            }
            
            Vector3 chunkStart = new Vector3(
                chunk.coordinates.x * 256 + 10,
                0,
                chunk.coordinates.y * 256 + 128
            );
            
            float spacing = 220f / Mathf.Max(1, chunk.enemySpawns.Count - 1);
            
            for (int i = 0; i < chunk.enemySpawns.Count; i++)
            {
                chunk.enemySpawns[i].spawnPosition = chunkStart + new Vector3(spacing * i, 0, 0);
                chunk.enemySpawns[i].spawnRotation = Quaternion.Euler(0, 90, 0);
            }
            
            EditorUtility.SetDirty(chunk);
            Debug.Log($"✅ {chunk.enemySpawns.Count} spawns posicionados en línea");
        }
        
        private void AutoGenerateAllWaypoints()
        {
            if (chunk.enemySpawns.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No hay spawns", "OK");
                return;
            }
            
            int count = 0;
            foreach (var spawn in chunk.enemySpawns)
            {
                if (spawn.initialAIState == EnemyAIState.Patrolling || spawn.initialAIState == EnemyAIState.Idle)
                {
                    spawn.patrolWaypoints = GenerateCircularWaypoints(spawn.spawnPosition, 10f, 4);
                    count++;
                }
            }
            
            EditorUtility.SetDirty(chunk);
            Debug.Log($"✅ Waypoints generados para {count} enemigos");
        }
        
        private List<Vector3> GenerateCircularWaypoints(Vector3 center, float radius, int count)
        {
            List<Vector3> waypoints = new List<Vector3>();
            float angleStep = 360f / count;
            
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 waypoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                waypoints.Add(waypoint);
            }
            
            return waypoints;
        }
    }
}
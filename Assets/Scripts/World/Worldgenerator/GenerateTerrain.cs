using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using World.ChunkSystem; // Asegúrate de que este namespace coincida con tus scripts

public class WorldGeneratorPro : EditorWindow
{
    // --- Configuración de Posición y Tamaño ---
    private int startX = 0;             // Coordenada X global donde empieza este lote
    private int startY = 0;             // Coordenada Y global donde empieza este lote
    private int batchSize = 5;          // Cuántos chunks de ancho/alto tiene ESTE lote
    
    /// <summary>
    /// Tamaño del chunk en metros. Se sincroniza automáticamente con WorldChunkManager.
    /// Si no existe WorldChunkManager, usa 256 por defecto.
    /// </summary>
    private int ChunkSize 
    {
        get 
        {
            if (World.ChunkSystem.WorldChunkManager.Instance != null)
            {
                return (int)World.ChunkSystem.WorldChunkManager.Instance.ChunkSize;
            }
            return 256; // Fallback
        }
    }
    
    // --- Configuración Visual ---
    private float terrainHeight = 50f;  // Altura máxima de las montañas
    public Texture2D heightmap;         // La imagen para este lote específico
    [Range(0.1f, 1f)] 
    public float blendStrength = 0.5f;  // Suavizado de bordes (0.5 es equilibrado)
    
    // --- Configuración de Enemigos ---
    public ChunkSpawnTemplate spawnTemplate; // Plantilla de spawns a aplicar

    // --- Rutas de Archivos ---
    private string terrainDataPath = "Assets/World/TerrainData";
    private string chunkDataPath = "Assets/Resources/World/Chunks";
    
    // --- Sistema de Visualización ---
    private Vector2 scrollPosition;
    private Dictionary<Vector2Int, bool> existingChunks = new Dictionary<Vector2Int, bool>();
    private int minX, maxX, minY, maxY;
    private bool showGrid = true;
    private int gridViewRadius = 10;     // Cuántos chunks mostrar alrededor del punto actual

    [MenuItem("Tools/Generador de Mundo PRO")]
    public static void ShowWindow()
    {
        var window = GetWindow<WorldGeneratorPro>("Generador Pro");
        window.minSize = new Vector2(600, 700);
        window.ScanExistingChunks();
    }

    private void OnEnable()
    {
        ScanExistingChunks();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // ========== SECCIÓN DE VISUALIZACIÓN ==========
        DrawVisualizationSection();
        
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        // ========== SECCIÓN DE NAVEGACIÓN ==========
        DrawNavigationSection();
        
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        // ========== CONFIGURACIÓN DEL LOTE ==========
        GUILayout.Label("📍 Coordenadas de Inicio", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        startX = EditorGUILayout.IntField("Inicio X", startX);
        startY = EditorGUILayout.IntField("Inicio Y", startY);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        GUILayout.Label("⚙️ Configuración del Lote", EditorStyles.boldLabel);
        batchSize = EditorGUILayout.IntField("Tamaño Lote (ej. 5)", batchSize);
        
        // Mostrar chunk size sincronizado con WorldChunkManager (solo lectura)
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("Tamaño Chunk (Auto-Sync)", ChunkSize);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.HelpBox($"Sincronizado con WorldChunkManager: {ChunkSize} unidades", MessageType.Info);
        
        terrainHeight = EditorGUILayout.FloatField("Altura Máxima", terrainHeight);

        EditorGUILayout.Space(10);
        
        GUILayout.Label("🎨 Visual y Mezcla", EditorStyles.boldLabel);
        heightmap = (Texture2D)EditorGUILayout.ObjectField("Heightmap (R/W Enabled)", heightmap, typeof(Texture2D), false);
        blendStrength = EditorGUILayout.Slider("Fuerza de Unión", blendStrength, 0.1f, 1f);
        
        EditorGUILayout.Space(10);
        
        GUILayout.Label("👹 Enemigos y Spawns", EditorStyles.boldLabel);
        spawnTemplate = (ChunkSpawnTemplate)EditorGUILayout.ObjectField(
            "Plantilla de Spawns", 
            spawnTemplate, 
            typeof(ChunkSpawnTemplate), 
            false
        );
        
        if (spawnTemplate != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📋 " + spawnTemplate.templateName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(spawnTemplate.description, EditorStyles.wordWrappedMiniLabel);
            
            // Contar enemigos
            int totalEnemies = 0;
            foreach (var def in spawnTemplate.spawnDefinitions)
            {
                totalEnemies += def.count;
            }
            EditorGUILayout.LabelField($"🎯 {totalEnemies} enemigos por chunk | Distribución: {spawnTemplate.distributionType}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Sin plantilla asignada. Los chunks se crearán sin enemigos.", MessageType.Info);
        }

        EditorGUILayout.Space(20);

        // Botón con color para destacar
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); 
        if (GUILayout.Button("🚀 GENERAR LOTE (Con Seguridad)", GUILayout.Height(40)))
        {
            if (ValidateInputs())
            {
                // Si pasa el chequeo de seguridad (no sobreescribir sin permiso), generamos
                if (CheckForOverlaps()) 
                {
                    GenerateBatch();
                    ScanExistingChunks(); // Actualizar vista después de generar
                }
            }
        }
        GUI.backgroundColor = Color.white; // Restaurar color

        EditorGUILayout.Space(10);
        if (GUILayout.Button("🔄 Actualizar Mapa de Chunks"))
        {
            ScanExistingChunks();
        }
        
        if (GUILayout.Button("🧹 Limpiar Referencias Perdidas"))
        {
            Resources.UnloadUnusedAssets();
        }
        
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        // ========== SECCIÓN DE BORRADO ==========
        GUILayout.Label("🗑️ HERRAMIENTAS DE BORRADO", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("⚠️ Estas acciones son IRREVERSIBLES. Los archivos se eliminarán permanentemente.", MessageType.Warning);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.7f, 0.3f); // Naranja
        if (GUILayout.Button("🗑️ Borrar Lote Actual", GUILayout.Height(35)))
        {
            DeleteCurrentBatch();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Rojo claro
        if (GUILayout.Button("💥 Borrar TODOS los Chunks", GUILayout.Height(35)))
        {
            DeleteAllChunks();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.7f, 0.7f, 1f); // Azul claro
        if (GUILayout.Button("🧹 Limpiar GameObjects de Escena", GUILayout.Height(35)))
        {
            CleanSceneObjects();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
    }
    
    // ========== NUEVA SECCIÓN: VISUALIZACIÓN ==========
    private void DrawVisualizationSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        GUILayout.Label("🗺️ MAPA DE CHUNKS EXISTENTES", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        showGrid = EditorGUILayout.Toggle("Mostrar Grid", showGrid);
        gridViewRadius = EditorGUILayout.IntSlider("Radio de Vista", gridViewRadius, 5, 20);
        EditorGUILayout.EndHorizontal();
        
        if (showGrid && existingChunks.Count > 0)
        {
            DrawChunkGrid();
        }
        else if (existingChunks.Count == 0)
        {
            EditorGUILayout.HelpBox("No se encontraron chunks existentes. ¡Comienza creando tu primer lote!", MessageType.Info);
        }
        
        // Estadísticas
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"📊 Total de Chunks: {existingChunks.Count}", EditorStyles.miniLabel);
        if (existingChunks.Count > 0)
        {
            EditorGUILayout.LabelField($"   Rango X: [{minX}, {maxX}] | Rango Y: [{minY}, {maxY}]", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawChunkGrid()
    {
        int centerX = startX + batchSize / 2;
        int centerY = startY + batchSize / 2;
        
        int displayMinX = centerX - gridViewRadius;
        int displayMaxX = centerX + gridViewRadius;
        int displayMinY = centerY - gridViewRadius;
        int displayMaxY = centerY + gridViewRadius;
        
        EditorGUILayout.Space(5);
        
        float cellSize = 18f;
        int gridWidth = displayMaxX - displayMinX + 1;
        int gridHeight = displayMaxY - displayMinY + 1;
        
        // Calcular centro del área disponible
        Rect gridRect = GUILayoutUtility.GetRect(gridWidth * cellSize, gridHeight * cellSize);
        
        // Dibujar desde arriba hacia abajo (Y invertida para que Y+ esté arriba)
        for (int y = displayMaxY; y >= displayMinY; y--)
        {
            for (int x = displayMinX; x <= displayMaxX; x++)
            {
                int gridX = x - displayMinX;
                int gridY = displayMaxY - y;
                
                Rect cellRect = new Rect(
                    gridRect.x + gridX * cellSize,
                    gridRect.y + gridY * cellSize,
                    cellSize - 1,
                    cellSize - 1
                );
                
                bool exists = existingChunks.ContainsKey(new Vector2Int(x, y));
                bool isInNewBatch = (x >= startX && x < startX + batchSize && 
                                    y >= startY && y < startY + batchSize);
                
                // Colorear celdas
                Color cellColor;
                if (isInNewBatch && exists)
                    cellColor = new Color(1f, 0.5f, 0f, 0.8f); // Naranja: va a sobreescribir
                else if (isInNewBatch)
                    cellColor = new Color(0.5f, 1f, 0.5f, 0.6f); // Verde claro: nuevo
                else if (exists)
                    cellColor = new Color(0.3f, 0.6f, 1f, 0.8f); // Azul: existente
                else
                    cellColor = new Color(0.2f, 0.2f, 0.2f, 0.3f); // Gris oscuro: vacío
                
                EditorGUI.DrawRect(cellRect, cellColor);
                
                // Dibujar borde
                Handles.color = Color.black * 0.5f;
                Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.black * 0.3f);
                
                // Tooltip
                if (cellRect.Contains(Event.current.mousePosition))
                {
                    string tooltip = $"({x}, {y})";
                    if (exists) tooltip += " ✓";
                    if (isInNewBatch) tooltip += " [NUEVO]";
                    GUI.Label(cellRect, new GUIContent("", tooltip));
                }
            }
        }
        
        // Leyenda
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        DrawLegendItem("Vacío", new Color(0.2f, 0.2f, 0.2f, 0.3f));
        DrawLegendItem("Existente", new Color(0.3f, 0.6f, 1f, 0.8f));
        DrawLegendItem("Nuevo", new Color(0.5f, 1f, 0.5f, 0.6f));
        DrawLegendItem("⚠️ Sobreescribir", new Color(1f, 0.5f, 0f, 0.8f));
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawLegendItem(string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        Rect colorRect = GUILayoutUtility.GetRect(12, 12, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(colorRect, color);
        GUILayout.Label(label, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }
    
    // ========== NUEVA SECCIÓN: NAVEGACIÓN ==========
    private void DrawNavigationSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🧭 Navegación Rápida", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Botones de navegación en cruz
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("▲", GUILayout.Width(40), GUILayout.Height(30)))
        {
            startY += batchSize;
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("◄", GUILayout.Width(40), GUILayout.Height(30)))
        {
            startX -= batchSize;
        }
        if (GUILayout.Button("●", GUILayout.Width(40), GUILayout.Height(30)))
        {
            startX = 0;
            startY = 0;
        }
        if (GUILayout.Button("►", GUILayout.Width(40), GUILayout.Height(30)))
        {
            startX += batchSize;
        }
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("▼", GUILayout.Width(40), GUILayout.Height(30)))
        {
            startY -= batchSize;
        }
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Accesos rápidos
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ir a Origen (0,0)"))
        {
            startX = 0;
            startY = 0;
        }
        if (existingChunks.Count > 0)
        {
            if (GUILayout.Button("Ir a Límite Superior"))
            {
                startX = minX;
                startY = maxY + 1;
            }
            if (GUILayout.Button("Ir a Límite Derecho"))
            {
                startX = maxX + 1;
                startY = minY;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    // ========== ESCANEO DE CHUNKS EXISTENTES ==========
    private void ScanExistingChunks()
    {
        existingChunks.Clear();
        
        if (!Directory.Exists(terrainDataPath))
        {
            return;
        }
        
        string[] files = Directory.GetFiles(terrainDataPath, "*_Terrain.asset");
        
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            // Formato: "Chunk_X_Y_Terrain"
            string[] parts = fileName.Split('_');
            
            if (parts.Length >= 3 && parts[0] == "Chunk")
            {
                if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                {
                    existingChunks[new Vector2Int(x, y)] = true;
                }
            }
        }
        
        // Calcular rangos
        if (existingChunks.Count > 0)
        {
            minX = existingChunks.Keys.Min(k => k.x);
            maxX = existingChunks.Keys.Max(k => k.x);
            minY = existingChunks.Keys.Min(k => k.y);
            maxY = existingChunks.Keys.Max(k => k.y);
        }
        
        Repaint();
    }

    private bool ValidateInputs()
    {
        if (heightmap == null) {
            EditorUtility.DisplayDialog("Error", "¡Falta asignar el Heightmap!", "Ok");
            return false;
        }
        // Verificar si la textura es legible
        try { heightmap.GetPixel(0, 0); }
        catch {
            EditorUtility.DisplayDialog("Error de Textura", 
                "La textura no tiene permiso de lectura.\n\nVe al Inspector de la imagen > Advanced > Marca 'Read/Write Enabled' y dale a Apply.", "Ok");
            return false;
        }
        return true;
    }

    // 🛡️ SISTEMA DE SEGURIDAD
    private bool CheckForOverlaps()
    {
        int conflictCount = 0;
        for (int x = 0; x < batchSize; x++)
        {
            for (int y = 0; y < batchSize; y++)
            {
                int checkX = startX + x;
                int checkY = startY + y;
                
                // Revisamos si ya existe el archivo de terreno
                string path = $"{terrainDataPath}/Chunk_{checkX}_{checkY}_Terrain.asset";
                if (File.Exists(path)) conflictCount++;
            }
        }

        if (conflictCount > 0)
        {
            return EditorUtility.DisplayDialog(
                "⚠️ ¡ALERTA DE SUPERPOSICIÓN! ⚠️",
                $"Vas a generar un lote sobre coordenadas ya ocupadas.\n\n" +
                $"Se encontraron {conflictCount} chunks existentes.\n" +
                "Si continúas, la forma del terreno anterior se perderá y se aplicará la nueva.",
                "🔥 Sobreescribir", 
                "Cancelar"
            );
        }
        return true;
    }

    private void GenerateBatch()
    {
        // Asegurar carpetas
        if (!Directory.Exists(terrainDataPath)) Directory.CreateDirectory(terrainDataPath);
        if (!Directory.Exists(chunkDataPath)) Directory.CreateDirectory(chunkDataPath);

        GameObject worldParent = GameObject.Find("--- WORLD ENVIRONMENT ---");
        if (worldParent == null) worldParent = new GameObject("--- WORLD ENVIRONMENT ---");

        int resolution = 129; // Estándar de Unity

        // --- BUCLE PRINCIPAL ---
        for (int x = 0; x < batchSize; x++)
        {
            for (int y = 0; y < batchSize; y++)
            {
                int globalX = startX + x;
                int globalY = startY + y;
                string chunkName = $"Chunk_{globalX}_{globalY}";

                // 1. Crear/Cargar Data del Terreno
                TerrainData tData = GetOrCreateTerrainData(chunkName, resolution);

                // 2. Crear GameObject
                GameObject chunkGO = Terrain.CreateTerrainGameObject(tData);
                chunkGO.name = chunkName;
                chunkGO.transform.parent = worldParent.transform;
                chunkGO.transform.position = new Vector3(globalX * ChunkSize, 0, globalY * ChunkSize);

                // 3. Crear Lógica (ScriptableObject)
                CreateChunkLogic(globalX, globalY, chunkName);

                // 4. Aplicar Heightmap + Blending
                float[,] heights = GenerateHeightsAndBlend(x, y, resolution, globalX, globalY);
                tData.SetHeights(0, 0, heights);

                // 5. Conectar vecinos
                UpdateNeighbors(chunkGO, globalX, globalY);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Lote generado en ({startX},{startY}) con éxito.");
    }

    // --- LÓGICA DE MEZCLA (BLENDING) ---
    private float[,] GenerateHeightsAndBlend(int localX, int localY, int res, int globalX, int globalY)
    {
        float[,] heights = new float[res, res];

        // A. Base del Heightmap
        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                // Mapeo UV sobre todo el lote
                float u = (float)((localX * (res - 1)) + x) / (batchSize * (res - 1));
                float v = (float)((localY * (res - 1)) + y) / (batchSize * (res - 1));
                
                heights[y, x] = heightmap.GetPixelBilinear(u, v).grayscale;
            }
        }

        // B. Blending Izquierdo (Solo si es el borde izquierdo del lote)
        if (localX == 0)
        {
            Terrain leftNeighbor = GetNeighbor(globalX - 1, globalY);
            if (leftNeighbor != null)
            {
                float[,] neighborHeights = leftNeighbor.terrainData.GetHeights(res - 1, 0, 1, res);
                int blendWidth = (int)(res * blendStrength * 0.25f); // 25% de ancho para mezclar

                for (int y = 0; y < res; y++)
                {
                    float targetH = neighborHeights[y, 0];
                    for (int x = 0; x < blendWidth; x++)
                    {
                        float t = (float)x / blendWidth; // 0 a 1
                        // Curva suave (SmoothStep) es mejor que Lerp lineal
                        float smoothT = t * t * (3f - 2f * t); 
                        heights[y, x] = Mathf.Lerp(targetH, heights[y, x], smoothT);
                    }
                }
            }
        }

        // C. Blending Inferior (Solo si es el borde inferior del lote)
        if (localY == 0)
        {
            Terrain bottomNeighbor = GetNeighbor(globalX, globalY - 1);
            if (bottomNeighbor != null)
            {
                float[,] neighborHeights = bottomNeighbor.terrainData.GetHeights(0, res - 1, res, 1);
                int blendWidth = (int)(res * blendStrength * 0.25f);

                for (int x = 0; x < res; x++)
                {
                    float targetH = neighborHeights[0, x];
                    for (int y = 0; y < blendWidth; y++)
                    {
                        float t = (float)y / blendWidth;
                        float smoothT = t * t * (3f - 2f * t);
                        heights[y, x] = Mathf.Lerp(targetH, heights[y, x], smoothT);
                    }
                }
            }
        }

        return heights;
    }

    // --- FUNCIONES AUXILIARES ---

    private Terrain GetNeighbor(int x, int y)
    {
        GameObject obj = GameObject.Find($"Chunk_{x}_{y}");
        if (obj != null) return obj.GetComponent<Terrain>();
        return null;
    }

    private void UpdateNeighbors(GameObject current, int x, int y)
    {
        Terrain t = current.GetComponent<Terrain>();
        Terrain left = GetNeighbor(x - 1, y);
        Terrain right = GetNeighbor(x + 1, y);
        Terrain top = GetNeighbor(x, y + 1);
        Terrain bottom = GetNeighbor(x, y - 1);

        t.SetNeighbors(left, top, right, bottom);
        
        // Actualizar vecinos recíprocamente
        if (left) left.SetNeighbors(left.leftNeighbor, left.topNeighbor, t, left.bottomNeighbor);
        if (right) right.SetNeighbors(t, right.topNeighbor, right.rightNeighbor, right.bottomNeighbor);
        if (top) top.SetNeighbors(top.leftNeighbor, top.topNeighbor, top.rightNeighbor, t);
        if (bottom) bottom.SetNeighbors(bottom.leftNeighbor, t, bottom.rightNeighbor, bottom.bottomNeighbor);
    }

    private TerrainData GetOrCreateTerrainData(string name, int res)
    {
        string path = $"{terrainDataPath}/{name}_Terrain.asset";
        TerrainData tData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        
        if (tData == null)
        {
            tData = new TerrainData();
            tData.heightmapResolution = res;
            tData.size = new Vector3(ChunkSize, terrainHeight, ChunkSize);
            AssetDatabase.CreateAsset(tData, path);
        }
        else
        {
            // Actualizar settings si ya existe
            tData.size = new Vector3(ChunkSize, terrainHeight, ChunkSize);
        }
        return tData;
    }

    private void CreateChunkLogic(int x, int y, string name)
    {
        string path = $"{chunkDataPath}/{name}_Data.asset";
        ChunkDataAsset data;
        
        // Si el archivo ya existe, cargarlo para actualizar
        if (File.Exists(path))
        {
            data = AssetDatabase.LoadAssetAtPath<ChunkDataAsset>(path);
        }
        else
        {
            // Crear nuevo asset
            data = ScriptableObject.CreateInstance<ChunkDataAsset>();
            data.coordinates = new Vector2Int(x, y);
            AssetDatabase.CreateAsset(data, path);
        }
        
        // Aplicar plantilla de spawns si está asignada
        if (spawnTemplate != null)
        {
            data.enemySpawns = spawnTemplate.GenerateSpawnConfigs(new Vector2Int(x, y), ChunkSize);
            EditorUtility.SetDirty(data); // Marcar como modificado
        }
        else
        {
            // Sin plantilla, asegurar que la lista existe pero está vacía
            if (data.enemySpawns == null)
            {
                data.enemySpawns = new List<EnemySpawnConfig>();
            }
        }
    }
    
    // ========== FUNCIONES DE BORRADO ==========
    
    /// <summary>
    /// Borra los chunks del lote actual (según startX, startY y batchSize).
    /// </summary>
    private void DeleteCurrentBatch()
    {
        List<Vector2Int> toDelete = new List<Vector2Int>();
        
        // Recopilar coordenadas del lote actual
        for (int x = 0; x < batchSize; x++)
        {
            for (int y = 0; y < batchSize; y++)
            {
                int globalX = startX + x;
                int globalY = startY + y;
                Vector2Int coords = new Vector2Int(globalX, globalY);
                
                if (existingChunks.ContainsKey(coords))
                {
                    toDelete.Add(coords);
                }
            }
        }
        
        if (toDelete.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin Chunks", 
                $"No hay chunks existentes en el rango ({startX},{startY}) con tamaño {batchSize}x{batchSize}.", "Ok");
            return;
        }
        
        // Confirmar acción
        bool confirm = EditorUtility.DisplayDialog(
            "⚠️ Confirmar Borrado de Lote",
            $"Se eliminarán {toDelete.Count} chunks en el rango:\n" +
            $"Desde: ({startX}, {startY})\n" +
            $"Hasta: ({startX + batchSize - 1}, {startY + batchSize - 1})\n\n" +
            "Esto borrará permanentemente:\n" +
            "• Assets de TerrainData\n" +
            "• Assets de ChunkData\n" +
            "• GameObjects en la escena\n\n" +
            "¿Continuar?",
            "🗑️ Sí, Borrar",
            "Cancelar"
        );
        
        if (!confirm) return;
        
        DeleteChunksByCoordinates(toDelete);
    }
    
    /// <summary>
    /// Borra TODOS los chunks existentes.
    /// </summary>
    private void DeleteAllChunks()
    {
        if (existingChunks.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin Chunks", "No hay chunks para borrar.", "Ok");
            return;
        }
        
        // Confirmar acción con doble verificación
        bool confirm1 = EditorUtility.DisplayDialog(
            "🚨 ¡ADVERTENCIA CRÍTICA!",
            $"Estás a punto de ELIMINAR TODOS LOS CHUNKS ({existingChunks.Count} en total).\n\n" +
            "Esta acción es IRREVERSIBLE y borrará:\n" +
            "• Todos los TerrainData assets\n" +
            "• Todos los ChunkData assets\n" +
            "• Todo el contenido del mundo generado\n\n" +
            "¿Estás COMPLETAMENTE SEGURO?",
            "⚠️ Continuar",
            "Cancelar"
        );
        
        if (!confirm1) return;
        
        // Segunda confirmación
        bool confirm2 = EditorUtility.DisplayDialog(
            "💀 ÚLTIMA ADVERTENCIA",
            $"Esto borrará {existingChunks.Count} chunks permanentemente.\n" +
            "No hay forma de deshacer esta acción.\n\n" +
            "Para confirmar, haz clic en 'BORRAR TODO'.",
            "💥 BORRAR TODO",
            "Cancelar"
        );
        
        if (!confirm2) return;
        
        List<Vector2Int> allChunks = new List<Vector2Int>(existingChunks.Keys);
        DeleteChunksByCoordinates(allChunks);
    }
    
    /// <summary>
    /// Elimina chunks específicos por sus coordenadas.
    /// </summary>
    private void DeleteChunksByCoordinates(List<Vector2Int> coordinates)
    {
        int deletedTerrains = 0;
        int deletedChunks = 0;
        int deletedGameObjects = 0;
        
        EditorUtility.DisplayProgressBar("Borrando Chunks", "Preparando...", 0f);
        
        try
        {
            for (int i = 0; i < coordinates.Count; i++)
            {
                Vector2Int coord = coordinates[i];
                float progress = (float)i / coordinates.Count;
                EditorUtility.DisplayProgressBar("Borrando Chunks", 
                    $"Borrando chunk ({coord.x}, {coord.y})... ({i + 1}/{coordinates.Count})", progress);
                
                string chunkName = $"Chunk_{coord.x}_{coord.y}";
                
                // 1. Borrar TerrainData asset
                string terrainPath = $"{terrainDataPath}/{chunkName}_Terrain.asset";
                if (File.Exists(terrainPath))
                {
                    AssetDatabase.DeleteAsset(terrainPath);
                    deletedTerrains++;
                }
                
                // 2. Borrar ChunkDataAsset
                string chunkDataPathFile = $"{chunkDataPath}/{chunkName}_Data.asset";
                if (File.Exists(chunkDataPathFile))
                {
                    AssetDatabase.DeleteAsset(chunkDataPathFile);
                    deletedChunks++;
                }
                
                // 3. Borrar GameObject en la escena
                GameObject chunkGO = GameObject.Find(chunkName);
                if (chunkGO != null)
                {
                    DestroyImmediate(chunkGO);
                    deletedGameObjects++;
                }
            }
            
            // Refrescar y limpiar
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Resources.UnloadUnusedAssets();
            
            // Actualizar mapa
            ScanExistingChunks();
            
            EditorUtility.DisplayDialog(
                "✅ Borrado Completado",
                $"Se eliminaron exitosamente:\n\n" +
                $"• {deletedTerrains} TerrainData assets\n" +
                $"• {deletedChunks} ChunkData assets\n" +
                $"• {deletedGameObjects} GameObjects de escena",
                "Ok"
            );
            
            Debug.Log($"✅ Borrado completado: {deletedTerrains} terrains, {deletedChunks} chunks, {deletedGameObjects} GameObjects");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
    
    /// <summary>
    /// Limpia los GameObjects de chunks de la escena sin borrar los assets.
    /// </summary>
    private void CleanSceneObjects()
    {
        GameObject worldParent = GameObject.Find("--- WORLD ENVIRONMENT ---");
        
        if (worldParent == null)
        {
            EditorUtility.DisplayDialog("Sin GameObjects", 
                "No se encontró el GameObject '--- WORLD ENVIRONMENT ---' en la escena.", "Ok");
            return;
        }
        
        int childCount = worldParent.transform.childCount;
        
        if (childCount == 0)
        {
            EditorUtility.DisplayDialog("Sin GameObjects", 
                "El GameObject '--- WORLD ENVIRONMENT ---' no tiene chunks hijos.", "Ok");
            return;
        }
        
        bool confirm = EditorUtility.DisplayDialog(
            "🧹 Limpiar Escena",
            $"Se encontraron {childCount} chunks en la escena.\n\n" +
            "Esto eliminará SOLO los GameObjects de la escena.\n" +
            "Los assets (TerrainData y ChunkData) se mantendrán intactos.\n\n" +
            "¿Continuar?",
            "Limpiar",
            "Cancelar"
        );
        
        if (!confirm) return;
        
        DestroyImmediate(worldParent);
        
        EditorUtility.DisplayDialog(
            "✅ Limpieza Completada",
            $"Se eliminó '--- WORLD ENVIRONMENT ---' con {childCount} chunks.\n\n" +
            "Los assets permanecen intactos y puedes regenerar los chunks cuando quieras.",
            "Ok"
        );
        
        Debug.Log($"✅ Limpieza de escena completada: {childCount} GameObjects eliminados");
    }
}
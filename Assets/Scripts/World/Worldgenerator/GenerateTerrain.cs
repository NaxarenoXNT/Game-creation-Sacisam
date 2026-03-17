using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using World.ChunkSystem;
using World.BiomeSystem;


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
    // ⚠️ TerrainData DEBE estar dentro de Resources para poder cargarse en runtime
    // con Resources.Load<TerrainData>("World/TerrainData/Chunk_X_Y_Terrain")
    private string terrainDataPath = "Assets/Resources/World/TerrainData";
    private string chunkDataPath = "Assets/Resources/World/Chunks";
    
    // --- Sistema de Visualización ---
    private Vector2 scrollPosition;
    private Dictionary<Vector2Int, bool> existingChunks = new Dictionary<Vector2Int, bool>();
    private int minX, maxX, minY, maxY;
    private bool showGrid = true;
    private int gridViewRadius = 10;     // Cuántos chunks mostrar alrededor del punto actual
    
    // --- Sistema de Pintura de Biomas ---
    private bool biomePaintMode = false;
    private BiomeSettings selectedBiomeToPaint;
    private Dictionary<Vector2Int, BiomeSettings> chunkBiomeCache = new Dictionary<Vector2Int, BiomeSettings>();
    private BiomeSettings[] availableBiomes; // Todos los BiomeSettings del proyecto

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
        RefreshAvailableBiomes();
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
        
        // ========== SECCIÓN DE SPLATMAP POR BIOMAS ==========
        DrawSplatmapSection();
        
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(10);
        
        // ========== SECCIÓN DE BAKEO DE PROPS ==========
        DrawPropBakeSection();
        
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
        
        // ─── Pintura de Biomas ──────────────────────────────────────────────────
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        bool prevPaintMode = biomePaintMode;
        GUI.backgroundColor = biomePaintMode ? new Color(0.3f, 1f, 0.5f) : Color.white;
        if (GUILayout.Button(biomePaintMode ? "🎨 MODO PINTAR BIOMAS: ON" : "🎨 Modo Pintar Biomas: OFF", 
                             GUILayout.Height(28)))
        {
            biomePaintMode = !biomePaintMode;
            if (biomePaintMode && (availableBiomes == null || availableBiomes.Length == 0))
                RefreshAvailableBiomes();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        if (biomePaintMode)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "Clickeá un chunk existente (naranja/azul) en el grid para asignarle un bioma.\n" +
                "Cada chunk solo puede tener UN bioma. Al terminar, usá 'Sincronizar' para actualizar el WorldBiomeMap.",
                MessageType.None);
            
            // Selector de bioma
            selectedBiomeToPaint = (BiomeSettings)EditorGUILayout.ObjectField(
                "Bioma a Pintar", selectedBiomeToPaint, typeof(BiomeSettings), false);
            
            // Botones rápidos para biomas disponibles
            if (availableBiomes != null && availableBiomes.Length > 0)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Biomas disponibles:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                int count = 0;
                foreach (var biome in availableBiomes)
                {
                    if (biome == null) continue;
                    bool isActive = selectedBiomeToPaint == biome;
                    Color biomeColor = GetBiomeCellColor(biome.category);
                    GUI.backgroundColor = isActive ? Color.white : biomeColor;
                    
                    string label = isActive ? $"▶ {biome.biomeName}" : biome.biomeName;
                    if (GUILayout.Button(label, GUILayout.MinWidth(60), GUILayout.Height(22)))
                    {
                        selectedBiomeToPaint = biome;
                    }
                    
                    count++;
                    if (count % 4 == 0) // Wrap cada 4 botones
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("No se encontraron BiomeSettings en el proyecto.\n" +
                    "Creá uno desde: Create → World → Biome Settings", MessageType.Warning);
                if (GUILayout.Button("🔄 Buscar BiomeSettings"))
                    RefreshAvailableBiomes();
            }
            
            EditorGUILayout.Space(5);
            
            // Botón de sincronización
            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            if (GUILayout.Button("↻ Sincronizar Biomas → WorldBiomeMap", GUILayout.Height(30)))
            {
                SyncBiomesToWorldBiomeMap();
            }
            GUI.backgroundColor = Color.white;
            
            // Estadísticas de biomas
            int chunksConBioma = chunkBiomeCache.Count;
            int chunksSinBioma = existingChunks.Count - chunksConBioma;
            EditorGUILayout.LabelField($"📊 Con bioma: {chunksConBioma} | Sin bioma: {chunksSinBioma}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        // ─── Grid ───────────────────────────────────────────────────────────────
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
        
        // Detectar clicks para pintura de biomas
        Event evt = Event.current;
        bool isClick = evt.type == EventType.MouseDown && evt.button == 0;
        Vector2Int? clickedChunk = null;
        
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
                
                var coord = new Vector2Int(x, y);
                bool exists = existingChunks.ContainsKey(coord);
                bool isInNewBatch = (x >= startX && x < startX + batchSize && 
                                    y >= startY && y < startY + batchSize);
                bool hasBiome = chunkBiomeCache.TryGetValue(coord, out BiomeSettings chunkBiome);
                
                // ─── Colorear celdas ────────────────────────────────────────
                Color cellColor;
                
                if (exists && hasBiome)
                {
                    // Chunk con bioma asignado → color del bioma
                    cellColor = GetBiomeCellColor(chunkBiome.category);
                    
                    // Si además está en el nuevo batch, hacer borde especial
                    if (isInNewBatch)
                        cellColor = Color.Lerp(cellColor, new Color(1f, 0.5f, 0f), 0.3f);
                }
                else if (isInNewBatch && exists)
                    cellColor = new Color(1f, 0.5f, 0f, 0.8f); // Naranja: va a sobreescribir
                else if (isInNewBatch)
                    cellColor = new Color(0.5f, 1f, 0.5f, 0.6f); // Verde claro: nuevo
                else if (exists)
                    cellColor = new Color(0.3f, 0.6f, 1f, 0.8f); // Azul: existente sin bioma
                else
                    cellColor = new Color(0.2f, 0.2f, 0.2f, 0.3f); // Gris oscuro: vacío
                
                EditorGUI.DrawRect(cellRect, cellColor);
                
                // Dibujar borde
                Color borderColor = Color.black * 0.3f;
                if (biomePaintMode && exists && cellRect.Contains(evt.mousePosition))
                    borderColor = Color.yellow; // Highlight al hacer hover en modo pintar
                Handles.color = borderColor;
                Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, borderColor);
                
                // ─── Click para asignar bioma ───────────────────────────────
                if (isClick && biomePaintMode && exists && cellRect.Contains(evt.mousePosition))
                {
                    clickedChunk = coord;
                    evt.Use(); // Consumir el evento
                }
                
                // Tooltip
                if (cellRect.Contains(evt.mousePosition))
                {
                    string tooltip = $"({x}, {y})";
                    if (exists) tooltip += " ✓";
                    if (hasBiome) tooltip += $" [{chunkBiome.biomeName}]";
                    if (isInNewBatch) tooltip += " [LOTE]";
                    if (biomePaintMode && exists) tooltip += "\n🖱️ Click = asignar bioma";
                    GUI.Label(cellRect, new GUIContent("", tooltip));
                }
            }
        }
        
        // ─── Procesar click de bioma ────────────────────────────────────────
        if (clickedChunk.HasValue && biomePaintMode)
        {
            if (selectedBiomeToPaint != null)
            {
                // Asignar directamente el bioma seleccionado
                AssignBiomeToChunk(clickedChunk.Value, selectedBiomeToPaint);
                Debug.Log($"🎨 Bioma '{selectedBiomeToPaint.biomeName}' asignado a chunk {clickedChunk.Value}");
                Repaint();
            }
            else
            {
                // No hay bioma seleccionado → mostrar menú popup
                if (availableBiomes != null && availableBiomes.Length > 0)
                {
                    var menu = new GenericMenu();
                    var capturedCoord = clickedChunk.Value;
                    
                    foreach (var biome in availableBiomes)
                    {
                        if (biome == null) continue;
                        bool isCurrentBiome = chunkBiomeCache.TryGetValue(capturedCoord, out var current) && current == biome;
                        var capturedBiome = biome;
                        
                        menu.AddItem(
                            new GUIContent($"{biome.biomeName} ({biome.category})"),
                            isCurrentBiome,
                            () => {
                                AssignBiomeToChunk(capturedCoord, capturedBiome);
                                Debug.Log($"🎨 Bioma '{capturedBiome.biomeName}' asignado a chunk {capturedCoord}");
                                Repaint();
                            }
                        );
                    }
                    
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("❌ Quitar bioma"), false, () => {
                        AssignBiomeToChunk(capturedCoord, null);
                        Debug.Log($"🗑️ Bioma removido del chunk {capturedCoord}");
                        Repaint();
                    });
                    
                    menu.ShowAsContext();
                }
                else
                {
                    EditorUtility.DisplayDialog("Sin Biomas",
                        "No se encontraron BiomeSettings en el proyecto.\n" +
                        "Creá uno desde: Create → World → Biome Settings",
                        "Ok");
                }
            }
        }
        
        // ─── Leyenda ────────────────────────────────────────────────────────
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        DrawLegendItem("Vacío", new Color(0.2f, 0.2f, 0.2f, 0.3f));
        DrawLegendItem("Existente", new Color(0.3f, 0.6f, 1f, 0.8f));
        DrawLegendItem("Nuevo", new Color(0.5f, 1f, 0.5f, 0.6f));
        DrawLegendItem("⚠️ Sobreescribir", new Color(1f, 0.5f, 0f, 0.8f));
        EditorGUILayout.EndHorizontal();
        
        // Leyenda de biomas si hay alguno asignado
        if (chunkBiomeCache.Count > 0)
        {
            var uniqueBiomes = new HashSet<BiomeSettings>(chunkBiomeCache.Values);
            EditorGUILayout.BeginHorizontal();
            foreach (var biome in uniqueBiomes)
            {
                if (biome != null)
                    DrawLegendItem(biome.biomeName, GetBiomeCellColor(biome.category));
            }
            EditorGUILayout.EndHorizontal();
        }
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
        chunkBiomeCache.Clear();
        
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
                    var coord = new Vector2Int(x, y);
                    existingChunks[coord] = true;
                    
                    // Cargar bioma asignado desde el ChunkDataAsset
                    string chunkAssetPath = $"{chunkDataPath}/Chunk_{x}_{y}_Data.asset";
                    var chunkAsset = AssetDatabase.LoadAssetAtPath<ChunkDataAsset>(chunkAssetPath);
                    if (chunkAsset != null && chunkAsset.primaryBiome != null)
                    {
                        chunkBiomeCache[coord] = chunkAsset.primaryBiome;
                    }
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
    
    /// <summary>
    /// Recarga la lista de BiomeSettings disponibles en el proyecto.
    /// </summary>
    private void RefreshAvailableBiomes()
    {
        // Buscar todos los BiomeSettings en el proyecto
        string[] guids = AssetDatabase.FindAssets("t:BiomeSettings");
        var list = new List<BiomeSettings>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var biome = AssetDatabase.LoadAssetAtPath<BiomeSettings>(path);
            if (biome != null) list.Add(biome);
        }
        availableBiomes = list.ToArray();
    }
    
    /// <summary>
    /// Asigna un BiomeSettings a un chunk existente (modifica su ChunkDataAsset).
    /// Después sincroniza automáticamente el WorldBiomeMap y aplica el splatmap
    /// para que los cambios de color sean visibles de inmediato.
    /// </summary>
    private void AssignBiomeToChunk(Vector2Int coord, BiomeSettings biome)
    {
        string chunkAssetPath = $"{chunkDataPath}/Chunk_{coord.x}_{coord.y}_Data.asset";
        var chunkAsset = AssetDatabase.LoadAssetAtPath<ChunkDataAsset>(chunkAssetPath);
        
        if (chunkAsset == null)
        {
            Debug.LogWarning($"⚠️ No se encontró ChunkDataAsset en {chunkAssetPath}");
            return;
        }
        
        Undo.RecordObject(chunkAsset, "Asignar Bioma a Chunk");
        chunkAsset.primaryBiome = biome;
        EditorUtility.SetDirty(chunkAsset);
        AssetDatabase.SaveAssetIfDirty(chunkAsset);
        
        // Actualizar cache local
        if (biome != null)
            chunkBiomeCache[coord] = biome;
        else
            chunkBiomeCache.Remove(coord);
        
        // Auto-sincronizar el WorldBiomeMap y aplicar splatmap para feedback visual inmediato
        AutoSyncAndApplySplatmap(coord);
    }
    
    /// <summary>
    /// Sincroniza el punto de control del bioma en WorldBiomeMap y aplica el splatmap
    /// al chunk modificado y sus vecinos (para transiciones correctas).
    /// Se ejecuta automáticamente al pintar un bioma — no requiere acción manual.
    /// </summary>
    private void AutoSyncAndApplySplatmap(Vector2Int modifiedCoord)
    {
        var biomeMap = Object.FindFirstObjectByType<WorldBiomeMap>();
        if (biomeMap == null) return;
        
        float chkSize = ChunkSize;
        var so = new SerializedObject(biomeMap);
        var pointsProp = so.FindProperty("controlPoints");
        
        // Actualizar/eliminar solo el punto de este chunk específico
        string targetId = $"chunk_{modifiedCoord.x}_{modifiedCoord.y}";
        bool found = false;
        
        for (int i = pointsProp.arraySize - 1; i >= 0; i--)
        {
            string pointId = pointsProp.GetArrayElementAtIndex(i)
                .FindPropertyRelative("pointId").stringValue;
            if (pointId == targetId)
            {
                // Si hay bioma asignado, actualizar; si no, eliminar
                if (chunkBiomeCache.TryGetValue(modifiedCoord, out var biome) && biome != null)
                {
                    var point = pointsProp.GetArrayElementAtIndex(i);
                    point.FindPropertyRelative("dominantBiome").objectReferenceValue = biome;
                    point.FindPropertyRelative("worldPosition").vector3Value = new Vector3(
                        (modifiedCoord.x + 0.5f) * chkSize, 0f, (modifiedCoord.y + 0.5f) * chkSize);
                }
                else
                {
                    pointsProp.DeleteArrayElementAtIndex(i);
                }
                found = true;
                break;
            }
        }
        
        // Si no existía el punto y hay bioma, crearlo
        if (!found && chunkBiomeCache.TryGetValue(modifiedCoord, out var newBiome) && newBiome != null)
        {
            int newIdx = pointsProp.arraySize;
            pointsProp.InsertArrayElementAtIndex(newIdx);
            var newPoint = pointsProp.GetArrayElementAtIndex(newIdx);
            newPoint.FindPropertyRelative("pointId").stringValue = targetId;
            newPoint.FindPropertyRelative("worldPosition").vector3Value = new Vector3(
                (modifiedCoord.x + 0.5f) * chkSize, 0f, (modifiedCoord.y + 0.5f) * chkSize);
            newPoint.FindPropertyRelative("dominantBiome").objectReferenceValue = newBiome;
            newPoint.FindPropertyRelative("influence").floatValue = 1f;
        }
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(biomeMap);
        
        // Aplicar splatmap al chunk modificado + vecinos dentro del radio de blend.
        // El radio de vecinos se calcula dinámicamente: ceil(blendRadius / chunkSize)
        // para garantizar que la transición se pinte en todos los chunks afectados.
        int neighborRadius = Mathf.CeilToInt(biomeMap.BlendRadius / chkSize);
        neighborRadius = Mathf.Max(neighborRadius, 1); // mínimo 1
        
        var coordsToUpdate = new List<Vector2Int>();
        for (int dx = -neighborRadius; dx <= neighborRadius; dx++)
            for (int dy = -neighborRadius; dy <= neighborRadius; dy++)
            {
                var neighbor = modifiedCoord + new Vector2Int(dx, dy);
                if (existingChunks.ContainsKey(neighbor))
                    coordsToUpdate.Add(neighbor);
            }
        
        ApplySplatmapToCoords(coordsToUpdate, biomeMap);
    }
    
    /// <summary>
    /// Sincroniza los biomas asignados en ChunkDataAssets → BiomeControlPoints del WorldBiomeMap.
    /// Cada chunk con primaryBiome genera un control point en su centro.
    /// </summary>
    private void SyncBiomesToWorldBiomeMap()
    {
        var biomeMap = FindBiomeMapInScene();
        if (biomeMap == null) return;
        
        // Resolver tamaño del chunk
        float chkSize = ChunkSize;
        
        // Obtener SerializedObject para editar
        var so = new SerializedObject(biomeMap);
        var pointsProp = so.FindProperty("controlPoints");
        
        // Eliminar todos los puntos auto-generados (prefijo "chunk_")
        int removedCount = 0;
        for (int i = pointsProp.arraySize - 1; i >= 0; i--)
        {
            string pointId = pointsProp.GetArrayElementAtIndex(i)
                .FindPropertyRelative("pointId").stringValue;
            if (pointId.StartsWith("chunk_"))
            {
                pointsProp.DeleteArrayElementAtIndex(i);
                removedCount++;
            }
        }
        
        // Agregar un control point por cada chunk con bioma asignado
        int addedCount = 0;
        foreach (var kvp in chunkBiomeCache)
        {
            var coord = kvp.Key;
            var biome = kvp.Value;
            if (biome == null) continue;
            
            Vector3 center = new Vector3(
                (coord.x + 0.5f) * chkSize,
                0f,
                (coord.y + 0.5f) * chkSize
            );
            
            int newIdx = pointsProp.arraySize;
            pointsProp.InsertArrayElementAtIndex(newIdx);
            var newPoint = pointsProp.GetArrayElementAtIndex(newIdx);
            newPoint.FindPropertyRelative("pointId").stringValue = $"chunk_{coord.x}_{coord.y}";
            newPoint.FindPropertyRelative("worldPosition").vector3Value = center;
            newPoint.FindPropertyRelative("dominantBiome").objectReferenceValue = biome;
            newPoint.FindPropertyRelative("influence").floatValue = 1f;
            addedCount++;
        }
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(biomeMap);
        
        Debug.Log($"✅ Sincronización completada: {removedCount} puntos eliminados, {addedCount} generados.");
        
        // Ofrecer aplicar splatmap inmediatamente para que se vean los colores del terreno
        bool applySplatmap = EditorUtility.DisplayDialog("✅ Sincronización Completada",
            $"Se eliminaron {removedCount} puntos auto-generados.\n" +
            $"Se generaron {addedCount} BiomeControlPoints nuevos.\n\n" +
            "Los puntos manuales (sin prefijo 'chunk_') se conservaron.\n\n" +
            "¿Desearís aplicar el Splatmap ahora a todos los chunks?\n" +
            "(Necesario para ver los colores del terreno en cada bioma)",
            "🎨 Sincronizar + Aplicar Splatmap",
            "Solo Sincronizar");
        
        if (applySplatmap)
        {
            ApplySplatmapToAll();
        }
    }
    
    /// <summary>
    /// Devuelve el color representativo de un BiomeCategory para el grid.
    /// </summary>
    private static Color GetBiomeCellColor(BiomeCategory category)
    {
        return category switch
        {
            BiomeCategory.Forest      => new Color(0.15f, 0.55f, 0.15f, 0.85f),
            BiomeCategory.Plains      => new Color(0.55f, 0.75f, 0.20f, 0.85f),
            BiomeCategory.Mountain    => new Color(0.50f, 0.42f, 0.35f, 0.85f),
            BiomeCategory.Arid        => new Color(0.80f, 0.70f, 0.25f, 0.85f),
            BiomeCategory.Coastal     => new Color(0.25f, 0.60f, 0.80f, 0.85f),
            BiomeCategory.Dark        => new Color(0.40f, 0.12f, 0.50f, 0.85f),
            BiomeCategory.Urban       => new Color(0.55f, 0.55f, 0.55f, 0.85f),
            BiomeCategory.Underground => new Color(0.30f, 0.20f, 0.12f, 0.85f),
            _                         => new Color(0.50f, 0.50f, 0.50f, 0.85f)
        };
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
            data.enemySpawnConfigs = spawnTemplate.GenerateSpawnConfigs(new Vector2Int(x, y), ChunkSize);
            EditorUtility.SetDirty(data); // Marcar como modificado
        }
        else
        {
            // Sin plantilla, asegurar que la lista existe pero está vacía
            if (data.enemySpawnConfigs == null)
            {
                data.enemySpawnConfigs = new List<EnemySpawnConfig>();
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
    
    // ========== SPLATMAP POR BIOMAS ==========
    
    [Header("Splatmap Config")]
    private float praderaNoiseScale = 0.02f;
    private float praderaNoiseIntensity = 0.25f;
    
    /// <summary>
    /// Dibuja la sección de UI para la herramienta de Splatmap.
    /// </summary>
    private void DrawSplatmapSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🎨 SPLATMAP POR BIOMAS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Pinta las texturas del terreno según el WorldBiomeMap.\n" +
            "• Requiere WorldBiomeMap en la escena con control points configurados.\n" +
            "• Cada BiomeSettings debe tener un TerrainLayer asignado.\n" +
            "• Funciona sobre chunks ya generados (no modifica alturas).\n" +
            "• Podés esculpir el terreno y luego re-aplicar sin problemas.", 
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        // Config de ruido para pradera
        GUILayout.Label("Variación Pradera (Perlin Noise)", EditorStyles.miniLabel);
        praderaNoiseScale = EditorGUILayout.Slider("Escala Ruido", praderaNoiseScale, 0.001f, 0.1f);
        praderaNoiseIntensity = EditorGUILayout.Slider("Intensidad Ruido", praderaNoiseIntensity, 0f, 0.5f);
        
        EditorGUILayout.Space(5);
        
        GUI.backgroundColor = new Color(0.5f, 0.85f, 1f);
        if (GUILayout.Button("🎨 Aplicar Splatmap al Lote Actual", GUILayout.Height(35)))
        {
            ApplySplatmapToBatch();
        }
        GUI.backgroundColor = Color.white;
        
        GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
        if (GUILayout.Button("🌍 Aplicar Splatmap a TODOS los Chunks", GUILayout.Height(30)))
        {
            ApplySplatmapToAll();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Aplica splatmap al lote actual según startX, startY, batchSize.
    /// </summary>
    private void ApplySplatmapToBatch()
    {
        var biomeMap = FindBiomeMapInScene();
        if (biomeMap == null) return;
        
        List<Vector2Int> coords = new List<Vector2Int>();
        for (int x = 0; x < batchSize; x++)
            for (int y = 0; y < batchSize; y++)
                coords.Add(new Vector2Int(startX + x, startY + y));
        
        ApplySplatmapToCoords(coords, biomeMap);
    }
    
    /// <summary>
    /// Aplica splatmap a todos los chunks existentes.
    /// </summary>
    private void ApplySplatmapToAll()
    {
        var biomeMap = FindBiomeMapInScene();
        if (biomeMap == null) return;
        
        if (existingChunks.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin Chunks", "No hay chunks existentes.", "Ok");
            return;
        }
        
        ApplySplatmapToCoords(new List<Vector2Int>(existingChunks.Keys), biomeMap);
    }
    
    /// <summary>
    /// Busca WorldBiomeMap en la escena. Muestra error si no existe.
    /// </summary>
    private WorldBiomeMap FindBiomeMapInScene()
    {
        var biomeMap = Object.FindFirstObjectByType<WorldBiomeMap>();
        if (biomeMap == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró WorldBiomeMap en la escena.\n\n" +
                "Agregá un GameObject con el componente WorldBiomeMap " +
                "y configurá al menos un BiomeControlPoint antes de usar esta herramienta.",
                "Ok");
        }
        return biomeMap;
    }
    
    /// <summary>
    /// Aplica splatmap a un set de coordenadas de chunk.
    /// Recopila todos los TerrainLayers únicos de los BiomeSettings presentes,
    /// los asigna al TerrainData, y pinta el alphamap según los pesos del BiomeMap.
    /// 
    /// Para el bioma category=Plains agrega una perturbación de PerlinNoise
    /// que genera "parches" irregulares revelando variación en la textura.
    /// </summary>
    private void ApplySplatmapToCoords(List<Vector2Int> coords, WorldBiomeMap biomeMap)
    {
        // 1. Recopilar todos los TerrainLayers únicos de los biomas con control points
        var allLayers = CollectTerrainLayers(biomeMap);
        if (allLayers.Count == 0)
        {
            EditorUtility.DisplayDialog("Sin TerrainLayers",
                "Ningún BiomeSettings tiene un TerrainLayer asignado.\n" +
                "Asigná un TerrainLayer en cada BiomeSettings que quieras ver en el terreno.",
                "Ok");
            return;
        }
        
        // Crear mapa de layer → index para lookups rápidos
        var layerToIndex = new Dictionary<TerrainLayer, int>();
        for (int i = 0; i < allLayers.Count; i++)
            layerToIndex[allLayers[i]] = i;
        
        TerrainLayer[] layerArray = allLayers.ToArray();
        
        // Resolver índice del layer del defaultBiome para usarlo como fallback
        // en píxeles donde ningún bioma tiene influencia.
        int defaultBiomeLayerIdx = -1;
        var defaultBiomeSO = GetDefaultBiomeFromMap(biomeMap);
        if (defaultBiomeSO != null && defaultBiomeSO.terrainLayer != null)
        {
            if (layerToIndex.TryGetValue(defaultBiomeSO.terrainLayer, out int idx))
                defaultBiomeLayerIdx = idx;
        }
        
        int processed = 0;
        EditorUtility.DisplayProgressBar("Aplicando Splatmap", "Preparando...", 0f);
        
        try
        {
            foreach (var coord in coords)
            {
                float progress = (float)processed / coords.Count;
                EditorUtility.DisplayProgressBar("Aplicando Splatmap",
                    $"Chunk ({coord.x}, {coord.y})... ({processed + 1}/{coords.Count})", progress);
                
                // Buscar el Terrain en la escena o cargar su TerrainData
                string chunkName = $"Chunk_{coord.x}_{coord.y}";
                Terrain terrain = GetNeighbor(coord.x, coord.y);
                
                TerrainData tData = null;
                
                if (terrain != null)
                {
                    tData = terrain.terrainData;
                }
                else
                {
                    // Intentar cargar desde asset
                    string path = $"{terrainDataPath}/{chunkName}_Terrain.asset";
                    tData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                }
                
                if (tData == null)
                {
                    processed++;
                    continue;
                }
                
                // Asignar layers al terrain
                tData.terrainLayers = layerArray;
                
                int alphaRes = tData.alphamapResolution;
                float[,,] alphamap = new float[alphaRes, alphaRes, allLayers.Count];
                
                // Posición del chunk en el mundo
                float worldOriginX = coord.x * ChunkSize;
                float worldOriginZ = coord.y * ChunkSize;
                
                // Pintar cada píxel del alphamap
                for (int ay = 0; ay < alphaRes; ay++)
                {
                    for (int ax = 0; ax < alphaRes; ax++)
                    {
                        // Mapear coordenada de alphamap → posición mundo
                        float normalizedX = (float)ax / (alphaRes - 1);
                        float normalizedZ = (float)ay / (alphaRes - 1);
                        float worldX = worldOriginX + normalizedX * ChunkSize;
                        float worldZ = worldOriginZ + normalizedZ * ChunkSize;
                        
                        // Samplear bioma en esta posición
                        BiomeSample sample = biomeMap.GetBiomeAt(new Vector3(worldX, 0, worldZ));
                        
                        // Distribuir pesos en los layers
                        float totalAssigned = 0f;
                        foreach (var (biome, weight) in sample.Influences)
                        {
                            if (biome.terrainLayer == null) continue;
                            if (!layerToIndex.TryGetValue(biome.terrainLayer, out int layerIdx)) continue;
                            
                            float finalWeight = weight;
                            
                            // BONUS: Variación con Perlin Noise para praderas
                            if (biome.category == BiomeCategory.Plains && praderaNoiseIntensity > 0f)
                            {
                                float noise = Mathf.PerlinNoise(
                                    worldX * praderaNoiseScale + 1000f,
                                    worldZ * praderaNoiseScale + 1000f
                                );
                                // Remap noise de [0,1] → [-intensity, +intensity] y sumar al peso
                                float perturbation = (noise - 0.5f) * 2f * praderaNoiseIntensity;
                                finalWeight = Mathf.Clamp01(finalWeight + perturbation);
                            }
                            
                            alphamap[ay, ax, layerIdx] += finalWeight;
                            totalAssigned += finalWeight;
                        }
                        
                        // Normalizar para que la suma sea 1
                        if (totalAssigned > 0f && Mathf.Abs(totalAssigned - 1f) > 0.001f)
                        {
                            for (int l = 0; l < allLayers.Count; l++)
                                alphamap[ay, ax, l] /= totalAssigned;
                        }
                        else if (totalAssigned <= 0f)
                        {
                            // Fallback: usar el terrainLayer del defaultBiome si existe,
                            // sino primer layer. Esto evita cortes duros en chunks sin bioma.
                            int fallbackIdx = defaultBiomeLayerIdx >= 0 ? defaultBiomeLayerIdx : 0;
                            alphamap[ay, ax, fallbackIdx] = 1f;
                        }
                    }
                }
                
                tData.SetAlphamaps(0, 0, alphamap);
                EditorUtility.SetDirty(tData);
                processed++;
            }
            
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("✅ Splatmap Aplicado",
                $"Se pintaron {processed} chunks con {allLayers.Count} capas de textura.\n\n" +
                "Las alturas no fueron modificadas.",
                "Ok");
            
            Debug.Log($"✅ Splatmap aplicado a {processed} chunks con {allLayers.Count} TerrainLayers.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
    
    /// <summary>
    /// Recopila TerrainLayers únicos de todos los BiomeSettings referenciados por el BiomeMap.
    /// Incluye el defaultBiome para que los chunks sin bioma asignado tengan una textura de transición.
    /// </summary>
    private List<TerrainLayer> CollectTerrainLayers(WorldBiomeMap biomeMap)
    {
        var layers = new List<TerrainLayer>();
        var seen = new HashSet<TerrainLayer>();
        
        // Primero incluir el defaultBiome para que sea layer[0] (fallback natural)
        var defaultBiome = GetDefaultBiomeFromMap(biomeMap);
        if (defaultBiome != null && defaultBiome.terrainLayer != null)
        {
            if (seen.Add(defaultBiome.terrainLayer))
                layers.Add(defaultBiome.terrainLayer);
        }
        
        foreach (var point in biomeMap.ControlPoints)
        {
            if (point.dominantBiome == null) continue;
            if (point.dominantBiome.terrainLayer == null) continue;
            
            if (seen.Add(point.dominantBiome.terrainLayer))
                layers.Add(point.dominantBiome.terrainLayer);
        }
        
        // También buscar en todos los BiomeSettings del proyecto por si hay biomas
        // usados como defaultBiome que no tienen control point
        var allBiomes = Resources.FindObjectsOfTypeAll<BiomeSettings>();
        foreach (var biome in allBiomes)
        {
            if (biome.terrainLayer != null && seen.Add(biome.terrainLayer))
                layers.Add(biome.terrainLayer);
        }
        
        return layers;
    }
    
    /// <summary>
    /// Lee el campo 'defaultBiome' del WorldBiomeMap usando SerializedObject.
    /// </summary>
    private BiomeSettings GetDefaultBiomeFromMap(WorldBiomeMap biomeMap)
    {
        var so = new SerializedObject(biomeMap);
        var defaultProp = so.FindProperty("defaultBiome");
        return defaultProp?.objectReferenceValue as BiomeSettings;
    }
    
    // ========== BAKEO DE PROPS ==========
    
    /// <summary>
    /// Dibuja la sección de UI para bakear props de la escena en los ChunkDataAssets.
    /// </summary>
    private void DrawPropBakeSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("🏠 BAKEO DE PROPS (Escena → ChunkData)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Colocá prefabs en la escena con el componente PropMarker, " +
            "posicionalos visualmente y luego presioná 'Bakear' para guardar " +
            "sus posiciones en los ChunkDataAssets correspondientes.\n\n" +
            "Flujo: Arrastrar prefab → Agregar PropMarker → Asignar PropData → Bakear.",
            MessageType.Info);
        
        EditorGUILayout.Space(5);
        
        // Contar markers en escena
        var allMarkers = Object.FindObjectsByType<World.ChunkSystem.PropMarker>(FindObjectsSortMode.None);
        int totalMarkers = allMarkers.Length;
        int unbaked = 0;
        int baked = 0;
        int withoutData = 0;
        
        foreach (var m in allMarkers)
        {
            if (m.propData == null) withoutData++;
            else if (m.isBaked) baked++;
            else unbaked++;
        }
        
        EditorGUILayout.LabelField($"📊 PropMarkers en escena: {totalMarkers}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"   Pendientes: {unbaked} | Ya bakeados: {baked} | Sin PropData: {withoutData}", EditorStyles.miniLabel);
        
        if (withoutData > 0)
        {
            EditorGUILayout.HelpBox(
                $"⚠️ Hay {withoutData} PropMarker(s) sin PropData asignado. " +
                "Serán ignorados al bakear.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(5);
        
        // Botón principal de bakeo
        GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
        if (GUILayout.Button("🏠 Bakear Props → ChunkData", GUILayout.Height(35)))
        {
            BakePropsToChunkData();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(3);
        
        // Botón de limpieza de props bakeados (NO borra assets, solo GOs)
        GUI.backgroundColor = new Color(0.9f, 0.75f, 0.6f);
        if (GUILayout.Button("🧹 Limpiar Props Bakeados de la Escena", GUILayout.Height(28)))
        {
            CleanBakedPropsFromScene();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(3);
        
        // Botón de selección rápida
        if (GUILayout.Button("🔍 Seleccionar todos los PropMarkers"))
        {
            var markers = Object.FindObjectsByType<World.ChunkSystem.PropMarker>(FindObjectsSortMode.None);
            Selection.objects = markers.Select(m => m.gameObject).ToArray();
            Debug.Log($"🔍 Seleccionados {markers.Length} PropMarkers en la escena.");
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// Encuentra todos los PropMarker en la escena, calcula a qué chunk pertenecen
    /// y guarda su información en el ChunkDataAsset correspondiente.
    /// </summary>
    private void BakePropsToChunkData()
    {
        var allMarkers = Object.FindObjectsByType<World.ChunkSystem.PropMarker>(FindObjectsSortMode.None);
        
        if (allMarkers.Length == 0)
        {
            EditorUtility.DisplayDialog("Sin Props",
                "No se encontraron GameObjects con PropMarker en la escena.\n\n" +
                "Agregá el componente PropMarker a los objetos que quieras bakear.",
                "Ok");
            return;
        }
        
        // Filtrar markers válidos (con PropData asignado y no bakeados)
        var validMarkers = allMarkers
            .Where(m => m.propData != null && !m.isBaked)
            .ToList();
        
        if (validMarkers.Count == 0)
        {
            int bakedCount = allMarkers.Count(m => m.isBaked);
            int noDataCount = allMarkers.Count(m => m.propData == null);
            EditorUtility.DisplayDialog("Nada que bakear",
                $"Todos los PropMarkers están bakeados ({bakedCount}) " +
                $"o no tienen PropData ({noDataCount}).\n\n" +
                "Para re-bakear un prop, desmarcá 'isBaked' en su PropMarker.",
                "Ok");
            return;
        }
        
        // Agrupar por chunk
        var chunkGroups = new Dictionary<Vector2Int, List<World.ChunkSystem.PropMarker>>();
        int chunkSz = ChunkSize;
        
        foreach (var marker in validMarkers)
        {
            int chunkX = Mathf.FloorToInt(marker.transform.position.x / chunkSz);
            int chunkY = Mathf.FloorToInt(marker.transform.position.z / chunkSz);
            var coord = new Vector2Int(chunkX, chunkY);
            
            if (!chunkGroups.ContainsKey(coord))
                chunkGroups[coord] = new List<World.ChunkSystem.PropMarker>();
            
            chunkGroups[coord].Add(marker);
        }
        
        // Confirmar
        string summary = $"Se bakearán {validMarkers.Count} props en {chunkGroups.Count} chunk(s):\n";
        foreach (var kvp in chunkGroups)
        {
            summary += $"  • Chunk ({kvp.Key.x}, {kvp.Key.y}): {kvp.Value.Count} props\n";
        }
        
        bool confirm = EditorUtility.DisplayDialog(
            "🏠 Confirmar Bakeo de Props",
            summary + "\n¿Continuar?",
            "Bakear",
            "Cancelar");
        
        if (!confirm) return;
        
        // Procesar cada chunk
        int totalBaked = 0;
        int chunksCreated = 0;
        int chunksUpdated = 0;
        int skippedNoChunk = 0;
        
        EditorUtility.DisplayProgressBar("Bakeando Props", "Preparando...", 0f);
        
        try
        {
            int processed = 0;
            
            foreach (var kvp in chunkGroups)
            {
                Vector2Int coord = kvp.Key;
                var markers = kvp.Value;
                float progress = (float)processed / chunkGroups.Count;
                EditorUtility.DisplayProgressBar("Bakeando Props",
                    $"Chunk ({coord.x}, {coord.y})... ({processed + 1}/{chunkGroups.Count})", progress);
                
                // Cargar o crear el ChunkDataAsset
                string assetPath = $"{chunkDataPath}/Chunk_{coord.x}_{coord.y}_Data.asset";
                var chunkAsset = AssetDatabase.LoadAssetAtPath<World.ChunkSystem.ChunkDataAsset>(assetPath);
                
                if (chunkAsset == null)
                {
                    // Verificar si existe TerrainData para este chunk
                    string terrainPath = $"{terrainDataPath}/Chunk_{coord.x}_{coord.y}_Terrain.asset";
                    bool terrainExists = File.Exists(terrainPath);
                    
                    if (!terrainExists)
                    {
                        Debug.LogWarning(
                            $"⚠️ No existe terreno ni ChunkData para chunk ({coord.x}, {coord.y}). " +
                            $"{markers.Count} props en esas coordenadas serán ignorados. " +
                            "Generá primero el terreno con 'GENERAR LOTE'.");
                        skippedNoChunk += markers.Count;
                        processed++;
                        continue;
                    }
                    
                    // Crear ChunkDataAsset nuevo
                    if (!Directory.Exists(chunkDataPath))
                        Directory.CreateDirectory(chunkDataPath);
                    
                    chunkAsset = ScriptableObject.CreateInstance<World.ChunkSystem.ChunkDataAsset>();
                    chunkAsset.coordinates = coord;
                    AssetDatabase.CreateAsset(chunkAsset, assetPath);
                    chunksCreated++;
                }
                else
                {
                    chunksUpdated++;
                }
                
                Undo.RecordObject(chunkAsset, "Bakear Props");
                
                // Determinar el índice inicial para IDs únicos
                int startIndex = chunkAsset.propSpawnConfigs.Count;
                
                // Agregar cada prop
                foreach (var marker in markers)
                {
                    var config = marker.ToSpawnConfig(coord, startIndex);
                    chunkAsset.propSpawnConfigs.Add(config);
                    
                    // Marcar como bakeado
                    Undo.RecordObject(marker, "Marcar como bakeado");
                    marker.isBaked = true;
                    EditorUtility.SetDirty(marker);
                    
                    startIndex++;
                    totalBaked++;
                }
                
                EditorUtility.SetDirty(chunkAsset);
                processed++;
            }
            
            AssetDatabase.SaveAssets();
            
            string result = $"✅ Bakeo completado:\n\n" +
                $"• {totalBaked} props bakeados\n" +
                $"• {chunksUpdated} chunks actualizados\n" +
                $"• {chunksCreated} chunks creados\n";
            
            if (skippedNoChunk > 0)
                result += $"• ⚠️ {skippedNoChunk} props ignorados (sin chunk existente)\n";
            
            result += "\n¿Querés limpiar los Props bakeados de la escena?";
            
            bool cleanup = EditorUtility.DisplayDialog("✅ Bakeo Completado",
                result, "🧹 Limpiar de escena", "Mantener en escena");
            
            if (cleanup)
            {
                CleanBakedPropsFromScene();
            }
            
            Debug.Log($"✅ Bakeo: {totalBaked} props → {chunkGroups.Count} chunks ({chunksCreated} nuevos, {chunksUpdated} actualizados)");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
    
    /// <summary>
    /// Elimina de la escena solo los GameObjects con PropMarker que ya fueron bakeados.
    /// NO borra assets ni terrenos — solo limpia los objetos visuales de diseño.
    /// </summary>
    private void CleanBakedPropsFromScene()
    {
        var allMarkers = Object.FindObjectsByType<World.ChunkSystem.PropMarker>(FindObjectsSortMode.None);
        var bakedMarkers = allMarkers.Where(m => m.isBaked).ToArray();
        
        if (bakedMarkers.Length == 0)
        {
            EditorUtility.DisplayDialog("Sin Props Bakeados",
                "No hay PropMarkers marcados como bakeados en la escena.",
                "Ok");
            return;
        }
        
        bool confirm = EditorUtility.DisplayDialog(
            "🧹 Limpiar Props Bakeados",
            $"Se eliminarán {bakedMarkers.Length} GameObject(s) con PropMarker bakeado.\n\n" +
            "Los datos ya están guardados en los ChunkDataAssets.\n" +
            "Los assets NO se borran, solo los GameObjects de la escena.\n\n" +
            "¿Continuar?",
            "Limpiar",
            "Cancelar");
        
        if (!confirm) return;
        
        int destroyed = 0;
        foreach (var marker in bakedMarkers)
        {
            if (marker != null && marker.gameObject != null)
            {
                Undo.DestroyObjectImmediate(marker.gameObject);
                destroyed++;
            }
        }
        
        Debug.Log($"🧹 Limpieza: {destroyed} props bakeados eliminados de la escena.");
    }
}

//TODO: arreglar la transicion de colores de chunks y corregir este bug generado de que se pintan baches de chunks en lugar de solamentre le seleccionado o dejar ese sistema pero arreglar el transition de colores para que no se note tanto, quizas agregando un tercer color de "transicion" entre el color del biome y el color gris neutro, o algo asi
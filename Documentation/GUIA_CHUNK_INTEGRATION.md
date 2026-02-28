# 🚀 Guía de Integración: Sistema de Chunks

> **📖 Ver también:**  
> - Sistema básico: [24_ChunkSystem.md](24_ChunkSystem.md)  
> - Integración completa: [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md)  
> - Setup de pooling: [GUIA_SETUP_POOLING.md](GUIA_SETUP_POOLING.md)

---

## Setup Rápido (5 minutos)

### Paso 1: Crear WorldChunkManager en la Escena

```
1. Hierarchy: Click derecho > Create Empty
2. Renombrar: "WorldChunkManager"
3. Add Component > WorldChunkManager
4. Configurar en Inspector:
   ✅ Player Transform: Arrastra tu jugador
   ✅ Chunk Size: 256 (⚠️ IMPORTANTE: Este es el valor maestro para todo el sistema)
   ✅ Load Radius: 2
   ✅ Update Interval: 1
   ✅ Show Debug Gizmos: ✅
```

### ⚙️ Sincronización del Tamaño de Chunk

**IMPORTANTE:** El tamaño de chunk se define UNA SOLA VEZ en `WorldChunkManager.ChunkSize`.

Todos los sistemas leen automáticamente de ahí:
- ✅ `ChunkDataAssetEditor` (editor visual)
- ✅ `GenerateTerrain` (generador de mundo)
- ✅ `ChunkSpawnTemplate` (plantillas de spawn)
- ✅ Gizmos y visualizaciones

**NO cambies manualmente el tamaño en diferentes lugares.** Si necesitas cambiar el tamaño:
1. Abre el `WorldChunkManager` en el Inspector
2. Cambia el valor de `Chunk Size`
3. Todos los sistemas se sincronizarán automáticamente

**Valor recomendado:** 256 (balance entre detalle y performance)

---

### Paso 2: Verificar DynamicEnemyPoolManager

```
Ya debería estar en tu escena. Si no:

1. Hierarchy: Create Empty > "DynamicEnemyPoolManager"
2. Add Component > DynamicEnemyPoolManager
3. Configurar:
   ✅ Enemy Controller Prefab: Tu prefab de EnemyController
```

### Paso 3: Crear tu Primer Chunk

```
1. Project: Click derecho > Create > World > Chunk Data
2. Nombre: "Chunk_Test_00"
3. Inspector:
   - Coordinates: (0, 0)
   - Gizmo Color: Verde
   
4. Click "+" en Enemy Spawns
5. Configurar primer spawn:
   ✅ Enemy Data: Selecciona tu EnemigoData (ej: GoblinWarrior)
   ✅ Spawn Position: (10, 0, 10)
   ✅ Initial AI State: Idle
```

### Paso 4: Cargar el Chunk

**Opción A - Modo Fácil (Automático):**
```
1. Hierarchy: Create Empty > "ChunkLoader"
2. Add Component > ChunkLoader
3. En Inspector:
   - Chunk Assets: Arrastra "Chunk_Test_00"
   - Load On Start: ✅
```

**Opción B - Modo Manual (Código):**
```csharp
// En algún script de inicio
void Start()
{
    ChunkDataAsset chunk = Resources.Load<ChunkDataAsset>("Chunks/Chunk_Test_00");
    WorldChunkManager.Instance.RegisterChunk(chunk.ToRuntimeData());
}
```

### Paso 5: Play y Testear

```
1. Hit Play
2. Mueve al jugador cerca de (10, 0, 10)
3. Deberías ver un enemigo spawnearse
4. Aléjate del chunk → el enemigo vuelve al pool
5. Acércate de nuevo → el enemigo respawnea
```

---

## 🎨 Configuración Visual (Editor Avanzado)

### Agregar Waypoints a un Enemigo

```
1. Selecciona tu ChunkDataAsset
2. En Inspector, en "Enemy Spawns", selecciona un spawn
3. En "Editar Spawn" elige el spawn de la lista
4. En Scene View verás handles amarillos
5. Click "Agregar Waypoint en Posición Actual"
6. Arrastra los handles para mover waypoints
```

### Crear Spawns desde GameObjects

```
1. Scene View: Coloca GameObjects vacíos donde quieras spawns
2. Selecciona el GameObject
3. Selecciona tu ChunkDataAsset
4. Click "Crear Spawn desde GameObject Seleccionado"
5. Asigna el EnemigoData al nuevo spawn
```

---

## 📦 Ejemplo Completo: Campamento Goblin

```csharp
// ChunkSetupExample.cs
using UnityEngine;
using World.ChunkSystem;

public class ChunkSetupExample : MonoBehaviour
{
    [SerializeField] private EnemigoData goblinData;
    [SerializeField] private EnemigoData hobgoblinData;
    
    void Start()
    {
        CrearCampamentoGoblin();
    }
    
    void CrearCampamentoGoblin()
    {
        var chunk = new ChunkData
        {
            coordinates = new Vector2Int(1, 1),
            chunkId = "goblin_camp",
            enemySpawnConfigs = new System.Collections.Generic.List<EnemySpawnConfig>
            {
                // Guardia 1 - Patrulla Norte
                new EnemySpawnConfig
                {
                    spawnId = "guard_north",
                    enemyData = goblinData,
                    spawnPosition = new Vector3(150, 0, 190),
                    initialAIState = EnemyAIState.Patrolling,
                    patrolBehavior = PatrolBehavior.PingPong,
                    patrolWaypoints = new System.Collections.Generic.List<Vector3>
                    {
                        new Vector3(140, 0, 190),
                        new Vector3(160, 0, 190)
                    }
                },
                
                // Guardia 2 - Patrulla Sur
                new EnemySpawnConfig
                {
                    spawnId = "guard_south",
                    enemyData = goblinData,
                    spawnPosition = new Vector3(150, 0, 110),
                    initialAIState = EnemyAIState.Patrolling,
                    patrolBehavior = PatrolBehavior.PingPong,
                    patrolWaypoints = new System.Collections.Generic.List<Vector3>
                    {
                        new Vector3(140, 0, 110),
                        new Vector3(160, 0, 110)
                    }
                },
                
                // Jefe Hobgoblin - En el centro
                new EnemySpawnConfig
                {
                    spawnId = "hobgoblin_chief",
                    enemyData = hobgoblinData,
                    spawnPosition = new Vector3(150, 0, 150),
                    initialAIState = EnemyAIState.Resting,
                    isUnique = true,
                    uniqueId = "hobgoblin_chief_camp_1",
                    detectionRadius = 25f
                }
            }
        };
        
        WorldChunkManager.Instance.RegisterChunk(chunk);
        Debug.Log("✅ Campamento Goblin creado");
    }
}
```

---

## 🔍 Debugging

### Ver Estado del Sistema

```
1. Hierarchy: Selecciona "WorldChunkManager"
2. Inspector: Tres puntitos (•••) > Debug: Mostrar Estado
3. Console mostrará:
   - Total chunks registrados
   - Chunks cargados actualmente
   - Enemigos activos por chunk
```

### Gizmos en Scene View

```
Scene View con Show Debug Gizmos ✅:
- 🟩 Verde: Chunks cargados
- 🟨 Amarillo: Chunk del jugador (más alto)
- 🔵 Cyan: Bounds del chunk individual
- 🟡 Amarillo (handles): Waypoints editables
```

### Consola Esperada

```
Al entrar a un chunk:
🗺️ Jugador cambió de chunk: (0, -1) → (0, 0)
📦 Cargando chunk (0, 0) (3 spawns)
✅ Chunk chunk_0_0: 3/3 enemigos spawneados

Al salir:
📤 Descargando chunk (0, 0) (3 enemigos activos)
♻️ EnemyController devuelto al pool: Goblin
```

---

## ⚠️ Troubleshooting

### "WorldChunkManager no encontrado"
```
✅ Asegúrate que WorldChunkManager esté en la escena
✅ Debe estar activo (GameObject.SetActive(true))
✅ El script debe inicializarse antes de registrar chunks (usa Invoke)
```

### "DynamicEnemyPoolManager no está asignado"
```
✅ Crea el GameObject con DynamicEnemyPoolManager
✅ Asigna el Enemy Controller Prefab
✅ En WorldChunkManager, referencia opcional (auto-detecta si es Singleton)
```

### "Enemigos no aparecen"
```
✅ Verifica que Spawn Position esté en el chunk correcto
   Ejemplo: Chunk (0,0) → Posiciones entre (0-100, 0-100)
✅ Asigna EnemigoData al spawn config
✅ Verifica que ChunkLoader esté cargando los chunks
✅ Mueve al jugador al chunk (mira gizmos amarillos)
```

### "Enemigos aparecen duplicados"
```
✅ NO llames a RegisterChunk() múltiples veces para el mismo chunk
✅ Usa un ChunkLoader O código manual, no ambos
✅ Llama RegisterChunk() solo una vez al inicio
``` ### "Waypoints no se muestran en Scene View"
```
✅ Selecciona el ChunkDataAsset (no el GameObject)
✅ En Inspector "Editar Spawn" selecciona un spawn
✅ Scene View debe estar en modo de edición (no Play)
```

---

## 🎯 Workflows Comunes

### A. Diseño Visual en Editor

```
1. Crea ChunkDataAsset
2. Usa "Auto-Posicionar Spawns en Grid"
3. Ajusta manualmente en Scene View
4. Agrega waypoints con botón o handles
5. Asigna EnemigoData a cada spawn
6. Carga con ChunkLoader
```

### B. Generación Procedural

```csharp
void GenerarChunkProcedural(Vector2Int coords)
{
    var chunk = new ChunkData { coordinates = coords };
    int enemyCount = Random.Range(3, 8);
    
    for (int i = 0; i < enemyCount; i++)
    {
        Vector3 randomPos = GetRandomPosInChunk(coords);
        
        chunk.enemySpawnConfigs.Add(new EnemySpawnConfig
        {
            enemyData = GetRandomEnemyData(),
            spawnPosition = randomPos,
            initialAIState = EnemyAIState.Idle
        });
    }
    
    WorldChunkManager.Instance.RegisterChunk(chunk);
}
```

### C. Migración desde Sistema Actual

```csharp
// Si ya tienes enemigos spawneados manualmente:

// ANTES:
Instantiate(enemyPrefab, position, rotation);

// DESPUÉS:
// 1. Crea EnemySpawnConfig por cada spawn manual
// 2. Agrúpalos en ChunkData por zona
// 3. Registra chunks
// 4. Elimina spawns manuales
```

---

## 📊 Optimización Recomendada

### Configuración por Tipo de Juego

**Mundo Pequeño (< 10 chunks):**
```
Chunk Size: 50
Load Radius: 3
Update Interval: 1.5f
Max Spawns Per Frame: 10
```

**Mundo Mediano (10-50 chunks):**
```
Chunk Size: 100
Load Radius: 2
Update Interval: 1f
Max Spawns Per Frame: 5
```

**Mundo Grande (50+ chunks):**
```
Chunk Size: 150
Load Radius: 1
Update Interval: 0.5f
Max Spawns Per Frame: 3
Min Reload Time: 10f
```

---

## ✅ Checklist de Integración

```
CONFIGURACIÓN INICIAL:
☐ WorldChunkManager en escena
☐ DynamicEnemyPoolManager en escena
☐ EnemyController prefab asignado
☐ Player Transform referenciado

CREAR CHUNKS:
☐ ChunkDataAssets creados
☐ Spawns configurados con EnemigoData
☐ Waypoints definidos (opcional)
☐ Chunks registrados (ChunkLoader o código)

TESTING:
☐ Play mode funciona
☐ Enemigos spawnean al acercarse
☐ Enemigos despawnean al alejarse
☐ Pool manager reutiliza controllers
☐ No hay errores en consola

OPTIMIZACIÓN:
☐ Ajustar Load Radius según performance
☐ Configurar Max Spawns Per Frame
☐ Validar con WorldChunkManager > Debug
```

---

## 🎓 Próximos Pasos

1. **Implementa IA básica**: Crea `AIController` que use los waypoints
2. **Integra con SaveSystem**: Guarda estado de enemigos únicos
3. **Eventos de Chunk**: Publica eventos cuando chunks cargan/descargan
4. **Biomas**: Agrega lógica de bioma por chunk para variar spawns
5. **Streaming**: Para mundos muy grandes, carga chunks desde archivos

---

**¡Listo!** Tu sistema de chunks está funcionando. 🎉

Para más detalles ver [24_ChunkSystem.md](24_ChunkSystem.md)

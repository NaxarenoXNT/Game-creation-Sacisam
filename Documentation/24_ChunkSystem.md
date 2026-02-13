# Sistema de Chunks del Mundo

> **💡 Para la integración completa con enemigos y pooling, ver [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md)**

## 📋 Descripción

Sistema de chunks para optimizar el rendimiento dividiendo el mundo en secciones. Carga y descarga dinámicamente contenido según la posición del jugador.

---

## 🎯 Características Clave

✅ **Chunks dinámicos** - Carga/descarga automática según posición del jugador  
✅ **Configuración estática** - Define spawns, waypoints, comportamientos  
✅ **Integración con pooling** - Usa `DynamicEnemyPoolManager` (ver [Doc 14](14_ObjectPool_Refactor.md))  
✅ **Enemigos únicos** - Soporte para bosses que no respawnean  
✅ **Editor visual** - Diseña chunks y waypoints en el editor  
✅ **Escalable** - Spawn escalonado para evitar lag  

---

## 🏗️ Arquitectura Simplificada

```
WorldChunkManager (Singleton)
├── Dictionary<Vector2Int, ChunkData> chunks
├── Detecta posición del jugador
└── Carga/descarga chunks dinámicamente

ChunkData (por chunk)
├── Vector2Int coordinates
├── List<EnemySpawnConfig> enemySpawnConfigs  ← Config estática
└── List<EnemyController> activeEnemies        ← Referencias runtime

EnemySpawnConfig (por enemigo)
├── EnemigoData enemyData               ← ScriptableObject
├── Vector3 spawnPosition               ← Dónde aparece
├── List<Vector3> patrolWaypoints       ← Ruta de patrulla
├── EnemyAIState initialAIState         ← Estado inicial
└── bool isDefeatedThisSession          ← Tracking de muerte
```

**Ver arquitectura completa en:** [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md#-flujo-de-spawning)

---

## 📦 Setup Rápido

### 1. Crear WorldChunkManager

```
GameObject → Create Empty → "WorldChunkManager"
Add Component → WorldChunkManager
Configurar:
  - Player Transform: Tu jugador
  - Chunk Size: 100
  - Load Radius: 2
```

### 2. Crear ChunkDataAsset

```
Project → Create → World → Chunk Data
Configurar:
  - Coordinates: (0, 0)
  - Enemy Spawns: [Agregar configs]
```

### 3. Cargar el Chunk

```csharp
// Opción A: Automático con ChunkLoader component
// Opción B: Manual
WorldChunkManager.Instance.RegisterChunk(chunkAsset.ToRuntimeData());
```

**📖 Setup detallado:** Ver [GUIA_CHUNK_INTEGRATION.md](GUIA_CHUNK_INTEGRATION.md)

---

## 🔧 Componentes Principales

### WorldChunkManager
- **Singleton** que gestiona todos los chunks
- Detecta cambios de posición del jugador
- Carga/descarga chunks automáticamente
- Integra con `DynamicEnemyPoolManager`

```csharp
// API principal
WorldChunkManager.Instance.RegisterChunk(chunkData);
WorldChunkManager.Instance.GetChunk(Vector2Int coords);
WorldChunkManager.Instance.ReloadAllChunks();
WorldChunkManager.Instance.ResetAllSessionState(); // Resetea muertes de enemigos
```

### ChunkData
- Representa un chunk en runtime
- Contiene lista de `EnemySpawnConfig`
- Trackea enemigos activos

### EnemySpawnConfig
- Configuración estática por enemigo
- Define posición, waypoints, comportamiento
- Trackea si fue derrotado esta sesión

**Ver API completa:** [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md#-estadísticas)

---

## ⚙️ Configuración de EnemySpawnConfig

### Estados de IA Disponibles
```csharp
EnemyAIState.Idle       // Parado en su posición
EnemyAIState.Patrolling // Patrullando waypoints
EnemyAIState.Resting    // Descansando (animación)
EnemyAIState.Alerted    // En alerta
```

### Comportamientos de Patrulla
```csharp
PatrolBehavior.Loop      // 1→2→3→1
PatrolBehavior.PingPong  // 1→2→3→2→1
PatrolBehavior.Random    // Aleatorio
PatrolBehavior.Once      // Una vez y para
```

### Ejemplo Completo
```csharp
new EnemySpawnConfig
{
    spawnId = "goblin_patrol_1",
    enemyData = miGoblinData,              // EnemigoData (SO)
    spawnPosition = new Vector3(10, 0, 5),
    initialAIState = EnemyAIState.Patrolling,
    patrolWaypoints = new List<Vector3> { ... },
    patrolBehavior = PatrolBehavior.Loop,
    isUnique = false,                      // Normal enemy
    isDefeatedThisSession = false          // Vivo
};
```

**Ver configuración avanzada:** [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md#-ejemplo-de-uso-en-el-editor)

---

## 📊 Optimización

### Parámetros de Performance

```csharp
[SerializeField] private int loadRadius = 2;           // Chunks vecinos a cargar
[SerializeField] private float updateInterval = 1f;    // Frecuencia de check
[SerializeField] private int maxSpawnsPerFrame = 5;    // Spawns por frame
[SerializeField] private float minReloadTime = 5f;     // Cooldown de recarga
```

### Benchmark

```
Escenario: 25 chunks (5x5 grid), 10 enemigos por chunk = 250 total

Con Load Radius = 1:
- Chunks cargados: 1 (chunk actual)
- Enemigos activos: 10
- CPU overhead: ~0.1ms/frame

Con Load Radius = 2:
- Chunks cargados: 9 (3x3 grid)
- Enemigos activos: 90
- CPU overhead: ~0.5ms/frame

Con Load Radius = 3:
- Chunks cargados: 25 (5x5 grid)
- Enemigos activos: 250
- CPU overhead: ~1.2ms/frame
```

**Recomendación**: Load Radius = 2 para balance perfecto

---

## � Debug y Estadísticas

### Ver Estado en Runtime
```csharp
// Context Menu en WorldChunkManager
[ContextMenu("Debug: Mostrar Estado")]

// Output:
=== WORLD CHUNK MANAGER ===
Chunks totales: 5
Chunks cargados: 3
Enemigos activos: 12
```

### Gizmos en Scene View
```
Show Debug Gizmos: ✅
- Verde: Chunks cargados
- Amarillo: Chunk del jugador
```

---

## 🔗 Referencias

| Documento | Contenido |
|-----------|-----------|
| [14_ObjectPool_Refactor.md](14_ObjectPool_Refactor.md) | Sistema de pooling genérico |
| **[25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md)** | **Integración completa con enemigos** ⭐ |
| [GUIA_SETUP_POOLING.md](GUIA_SETUP_POOLING.md) | Guía práctica de pooling |
| [GUIA_CHUNK_INTEGRATION.md](GUIA_CHUNK_INTEGRATION.md) | Guía práctica de chunks |

---

## 📝 Notas Importantes 

⚠️ **Estado Dinámico**: HP, buffs, posición actual se resetean al recargar  
✅ **Estado Estático**: Waypoints, configuración de IA persisten  
🔄 **Tracking de Sesión**: Enemigos muertos no respawnean hasta reiniciar  
🎯 **Enemigos Únicos**: Se marcan como derrotados (ver Doc 25 para detalles)  

**Ver flujo completo y ejemplos:** [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md)

---

## 🚀 Próximos Pasos

Ahora que conoces el sistema básico de chunks:
1. 📖 Lee [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md) para la integración completa
2. 🛠️ Usa [GUIA_CHUNK_INTEGRATION.md](GUIA_CHUNK_INTEGRATION.md) para setup paso a paso
3. 🎮 Revisa [ChunkSystemIntegrationExample.cs](../Assets/Scripts/Examples/ChunkSystemIntegrationExample.cs) para código de ejemplo


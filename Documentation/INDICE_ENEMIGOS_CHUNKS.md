# 📚 Índice Especializado - Sistema de Enemigos y Chunks

> **🏠 Volver al índice general:** [INDICE.md](INDICE.md)

Este índice cubre específicamente el sistema de enemigos, chunks y pooling del proyecto.

---

## 🎯 Documentación Principal

| Documento | Descripción | Cuándo Leer |
|-----------|-------------|-------------|
| **[14_ObjectPool_Refactor.md](14_ObjectPool_Refactor.md)** | Sistema de pooling genérico<br/>Arquitectura, implementación, API | Para entender el pooling en profundidad<br/>⚙️ **Técnica** |
| **[24_ChunkSystem.md](24_ChunkSystem.md)** | Sistema de chunks básico<br/>Componentes, configuración, API | Para entender cómo funcionan los chunks<br/>📦 **Básica** |
| **[25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md)** | **Integración completa**<br/>EnemigoData → Enemigos → Controller → Chunks → Pool | **Documento principal del sistema**<br/>⭐ **EMPEZAR AQUÍ** |

---

## 📖 Guías Prácticas

| Guía | Descripción | Cuándo Usar |
|------|-------------|-------------|
| **[GUIA_SETUP_POOLING.md](GUIA_SETUP_POOLING.md)** | Setup paso a paso del pooling<br/>5 pasos, ejemplos de código | Al configurar el pooling por primera vez |
| **[GUIA_CHUNK_INTEGRATION.md](GUIA_CHUNK_INTEGRATION.md)** | Setup paso a paso de chunks<br/>Configuración visual, testing | Al crear tu primer chunk |

---

## 🗺️ Mapa Mental del Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                    SISTEMA COMPLETO                         │
│                                                             │
│  EnemigoData (SO) ──┐                                      │
│                     │                                       │
│  Factory Method ────┼──> Enemigos (Lógica)                │
│                     │     - Goblin                          │
│                     │     - Orco                            │
│                     │     - Dragon                          │
│                     │                                       │
│                     └──> EnemyController (Unity)           │
│                           ↕                                 │
│                     DynamicEnemyPoolManager                │
│                           ↕                                 │
│                     WorldChunkManager                      │
│                           ↕                                 │
│                     ChunkData + EnemySpawnConfig           │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚦 Guía de Lectura Recomendada

### Para empezar desde cero:

```
1. GUIA_SETUP_POOLING.md
   ↓ (Configuras DynamicEnemyPoolManager)
   
2. GUIA_CHUNK_INTEGRATION.md
   ↓ (Creas tu primer chunk)
   
3. 25_Sistema_Chunks_Enemigos.md
   ↓ (Entiendes cómo todo se integra)
   
4. Testea en Unity ✅
```

### Para entender el sistema completo:

```
1. 25_Sistema_Chunks_Enemigos.md ⭐
   ↓ (Visión general de la integración)
   
2. 24_ChunkSystem.md
   ↓ (Detalles de chunks)
   
3. 14_ObjectPool_Refactor.md
   ↓ (Detalles técnicos del pool)
```

### Para implementar funcionalidad nueva:

```
1. ChunkSystemIntegrationExample.cs
   ↓ (Ver ejemplos de código)
   
2. 25_Sistema_Chunks_Enemigos.md
   ↓ (API y métodos disponibles)
   
3. Implementar tu feature ✅
```

---

## 📂 Archivos de Código Relevantes

### Scripts Principales
```
Assets/Scripts/
├── World/ChunkSystem/
│   ├── WorldChunkManager.cs        → Orquestador principal (delega a sub-módulos)
│   ├── ChunkData.cs                → Datos de chunk runtime
│   ├── ChunkDataAsset.cs           → Asset del editor (ScriptableObject)
│   ├── EnemySpawnConfig.cs         → Config de spawn de enemigos
│   ├── ChunkTerrainLoader.cs       → Carga dinámica de TerrainData
│   ├── ChunkEnemySpawner.cs        → Spawning/despawning de enemigos
│   ├── ChunkProceduralDecorator.cs → Decoración procedural por bioma
│   ├── ChunkPropsManager.cs        → Props con identidad (edificios, cofres, NPCs)
│   ├── ChunkLoader.cs              → Helper para chunks adicionales fuera de auto-carga
│   ├── ChunkSpawnTemplate.cs       → Plantillas reutilizables de spawn
│   ├── ChunkSystemExample.cs       → Ejemplos de uso programático
│   └── PropMarker.cs               → Marcador para bakear props desde la escena
│
├── Managers/
│   ├── DynamicEnemyPoolManager.cs  → Pool de enemigos
│   └── ObjectPool<T>.cs            → Pool genérico
│
├── Controllers/EnemigosCont/
│   └── EnemyController.cs          → Controller de enemigo
│
├── SO/
│   └── EnemigoData.cs              → ScriptableObject base
│
└── Subclases/Enemigos/
    ├── Goblin.cs                   → Lógica de Goblin
    ├── Orco.cs                     → Lógica de Orco
    └── Dragon.cs                   → Lógica de Dragon
```

### Ejemplos
```
Assets/Scripts/Examples/
├── ChunkSystemIntegrationExample.cs    → Ejemplo completo
└── EnemySpawnerExample.cs              → Ejemplo de pooling
```

---

## 🔍 Búsqueda Rápida

### Quiero saber cómo...

| Tarea | Ver Documento | Sección |
|-------|---------------|---------|
| Crear un enemigo | Doc 25 | "Ejemplo de Uso en el Editor" |
| Spawnar enemigos manualmente | Doc 25 | "Flujo de Spawning" |
| Resetear enemigos muertos | Doc 25 | "Persistencia por Sesión" |
| Configurar waypoints | Doc 24 | "Configuración de EnemySpawnConfig" |
| Debuggear chunks | Doc 24 | "Debug y Estadísticas" |
| Entender el pooling | Doc 14 | "Arquitectura Nueva" |
| Setup inicial | GUIA_CHUNK_INTEGRATION | Todo el documento |

---

## ✅ Checklist de Implementación

Usa esto para verificar que todo está configurado:

### Pooling
- [ ] DynamicEnemyPoolManager en la escena
- [ ] Enemy Controller Prefab asignado
- [ ] EnemigoData (SO) creados
- [ ] Pool funciona (test con spawning manual)

### Chunks
- [ ] WorldChunkManager en la escena
- [ ] Player Transform asignado
- [ ] ChunkDataAssets creados
- [ ] EnemySpawnConfigs configurados
- [ ] Chunks registrados en el manager

### Integración
- [ ] Enemigos spawnean al acercarse
- [ ] Enemigos desaparecen al alejarse
- [ ] Enemigos muertos no respawnean
- [ ] Logs de debug funcionan
- [ ] Gizmos visibles en Scene View

---

## 📝 Notas de Mantenimiento

### Al Actualizar el Sistema

Si modificas algún componente del sistema:

1. ✅ Actualiza el documento principal ([25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md))
2. ✅ Verifica que las guías sigan siendo válidas
3. ✅ Actualiza los ejemplos de código si es necesario
4. ✅ Testea en Unity para verificar compatibilidad

### Al Agregar Nueva Funcionalidad

1. 📝 Documenta en [25_Sistema_Chunks_Enemigos.md](25_Sistema_Chunks_Enemigos.md) sección "Próximos Pasos"
2. 📝 Agrega ejemplo en `ChunkSystemIntegrationExample.cs`
3. 📝 Actualiza este índice si creas nueva documentación

---

## 🎯 TL;DR - Resumen Ultra Rápido

**Sistema de Enemigos:**
- EnemigoData (SO) define stats → Enemigos (clases) tiene lógica → EnemyController (Unity) visualiza
- DynamicEnemyPoolManager reutiliza controllers para performance
- WorldChunkManager spawnea/descarga según posición del jugador
- Enemigos muertos no respawnean hasta reiniciar el juego

**Documentos importantes:**
- **25_Sistema_Chunks_Enemigos.md** ← Empieza aquí ⭐
- GUIA_CHUNK_INTEGRATION.md ← Setup práctico
- 24_ChunkSystem.md ← Referencia de chunks
- 14_ObjectPool_Refactor.md ← Referencia de pooling

**Todo funciona correctamente y es compatible entre sí** ✅

# Índice de Documentación - Saclisam

> **📚 Índice Especializado:** Para documentación completa del sistema de enemigos, chunks y pooling, ver [INDICE_ENEMIGOS_CHUNKS.md](INDICE_ENEMIGOS_CHUNKS.md)

## Guías Generales
| # | Documento | Descripción |
|---|-----------|-------------|
| 00 | [GUIA_UNITY](00_GUIA_UNITY.md) | Guía de configuración de Unity |
| -- | [TODO](TODO.md) | Estado y tareas pendientes |
| **31** | [**Recuperacion_Escena**](31_Recuperacion_Escena.md) | **Setup desde cero tras pérdida de datos: managers, prefab del player, cámara** ⭐ |

---

## Arquitectura y Sistemas Core

| # | Documento | Descripción |
|---|-----------|-------------|
| 01 | [Arquitectura](01_Arquitectura.md) | Visión general de la arquitectura |
| 09 | [EventBus](09_EventBus.md) | Sistema de eventos desacoplado |
| 12 | [Guardado](12_Guardado.md) | Sistema de persistencia |
| **14** | [**ObjectPool_Refactor**](14_ObjectPool_Refactor.md) | **Sistema de pooling refactorizado** ⭐ |
| 15 | [SceneReference](15_SceneReference.md) | Referencias a escenas |
| **24** | [**ChunkSystem**](24_ChunkSystem.md) | **Sistema de chunks y optimización de mundo** ⭐ |
| **25** | [**Sistema_Chunks_Enemigos**](25_Sistema_Chunks_Enemigos.md) | **Integración completa: Enemigos + Chunks + Pool** ⭐ |

> 💡 **Para docs 14, 24 y 25:** Ver índice especializado → [INDICE_ENEMIGOS_CHUNKS.md](INDICE_ENEMIGOS_CHUNKS.md)

---

## Entidades y Clases

| # | Documento | Descripción |
|---|-----------|-------------|
| 02 | [Entidades](02_Entidades.md) | Sistema base de entidades |
| 03 | [Clases_Jugador](03_Clases_Jugador.md) | Clases jugables |
| 04 | [Enemigos](04_Enemigos.md) | Sistema de enemigos |
| 10 | [IA](10_IA.md) | Inteligencia artificial de enemigos |

---

## Combat System

| # | Documento | Descripción |
|---|-----------|-------------|
| **17** | [**Sistema_Combate**](17_Sistema_Combate.md) | **Sistema de combate dinámico** ⭐ |
| **18** | [**Sistema_Party**](18_Sistema_Party.md) | **Gestión de party y refuerzos** ⭐ |
| 05 | [Habilidades](05_Habilidades.md) | Sistema de habilidades |
| 06 | [Efectos](06_Efectos.md) | Efectos de habilidades |
| 07 | [Estados](07_Estados.md) | Estados alterados |
| 08 | [Elementos](08_Elementos.md) | Sistema elemental |
| 13 | [Cooldowns](13_Cooldowns.md) | Sistema de cooldowns |

---

## Progresión

| # | Documento | Descripción |
|---|-----------|-------------|
| 16 | [Evoluciones_Traits_Chains](16_Evoluciones_Traits_Chains.md) | Sistema de evoluciones |
| 19 | [Evoluciones](19_Evoluciones.md) | Sistema de evoluciones data-driven |
| 22 | [Misiones](22_Misiones.md) | Sistema de misiones y mundo vivo |

---

## UI

| # | Documento | Descripción |
|---|-----------|-------------|
| 11 | [UI_Reactiva](11_UI_Reactiva.md) | Sistema de UI reactiva |

---

## Documentación Técnica

| Documento | Descripción |
|-----------|-------------|
| [correccionesgenerales](correccionesgenerales.md) | Correcciones generales |
| [correccionesParaCombateManager](correccionesParaCombateManager.md) | Correcciones del CombateManager |

---
Actualizado)

```
┌─────────────────────────────────────────────────────────────────┐
│                         SACLISAM                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  MUNDO                EXPLORACIÓN          COMBATE              │
│  ─────                ────────────         ───────              │
│  WorldChunkManager    PlayerInterest      CombateManager       │
│  DynamicEnemyPool     PlayerParty         TurnManager          │
│        │                   │               CombatEncounter      │
│        │                   │                    │               │
│        └───────────────────┼────────────────────┘               │
│                            │                                    │
│                       EventBus                                  │
│                      (Comunicación)                             │
│                            │                                    │
│  ┌─────────────────────────┼────────────────────┐               │
│  │                    PROGRESIÓN                │               │
│  │  Evoluciones | XP/Niveles | Habilidades      │               │
│  └──────────────────────────────────────────────┘               │
│                      (Comunicación)                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```
Sistema de Chunks
```csharp
// Registrar chunk con enemigos
WorldChunkManager.Instance.RegisterChunk(chunkData);

// Obtener chunk actual del jugador
var coords = WorldChunkManager.Instance.WorldToChunkCoords(playerPos);
var chunk = WorldChunkManager.Instance.GetChunk(coords);
```

### Pooling de Enemigos
```csharp
// Obtener enemigo del pool
var enemy = DynamicEnemyPoolManager.Instance.ObtenerController(enemigoData);

// Devolver al pool
DynamicEnemyPoolManager.Instance.DevolverController(enemy, enemigoData);
```

### Iniciar Combate Dinámico
```csharp
// Automático: PlayerInterestZone detecta enemigos
// Manual:
CombateManager.Instance.IniciarCombateConEntidades(party, enemigos);
```

### Cambiar Main Character
```csharp
PlayerPartyManager.Instance.SetMainCharacter(nuevoMain);
```

### Solicitar Refuerzos
```csharp
ReinforcementSystem.Instance.RequestReinforcements(combatPosition);
```

### Suscribirse a Eventos
```csharp
EventBus.Suscribir<EventoCombateIniciado>(OnCombate);
EventBus.Suscribir<EventoMainCambiado>(OnMainChanged);
EventBus.Suscribir<EventoEnemigoDerrotado>(OnEnemyKill

### Suscribirse a Eventos
```csharp
EventBus.Suscribir<EventoCombateIniciado>(OnCombate);
EventBus.Suscribir<EventoMainCambiado>(OnMainChanged);
```

# Índice de Documentación - Saclisam

> **📚 Índice Especializado:** Para documentación completa del sistema de enemigos, chunks y pooling, ver [INDICE_ENEMIGOS_CHUNKS.md](INDICE_ENEMIGOS_CHUNKS.md)

## Guías Generales
| # | Documento | Descripción |
|---|-----------|-------------|
| 00 | [GUIA_UNITY](00_GUIA_UNITY.md) | Guía de configuración de Unity |
| -- | [TODO](TODO.md) | Estado y tareas pendientes |
| **31** | [**Recuperacion_Escena**](31_Recuperacion_Escena.md) | **Setup desde cero tras pérdida de datos** ⭐ |

---

## Arquitectura y Sistemas Core

| # | Documento | Descripción |
|---|-----------|-------------|
| 01 | [Arquitectura](01_Arquitectura.md) | Visión general de la arquitectura |
| 09 | [EventBus](09_EventBus.md) | Sistema de eventos desacoplado (EventBus estático) |
| 12 | [Guardado](12_Guardado.md) | Sistema de persistencia (per-personaje + global) |
| **14** | [**ObjectPool_Refactor**](14_ObjectPool_Refactor.md) | **Sistema de pooling refactorizado** ⭐ |
| 15 | [SceneReference](15_SceneReference.md) | Referencias a escenas |
| **24** | [**ChunkSystem**](24_ChunkSystem.md) | **Sistema de chunks y optimización de mundo** ⭐ |
| **25** | [**Sistema_Chunks_Enemigos**](25_Sistema_Chunks_Enemigos.md) | **Integración: Enemigos + Chunks + Pool** ⭐ |

> 💡 **Para docs 14, 24 y 25:** Ver índice especializado → [INDICE_ENEMIGOS_CHUNKS.md](INDICE_ENEMIGOS_CHUNKS.md)

---

## Entidades y Clases

| # | Documento | Descripción |
|---|-----------|-------------|
| 02 | [Entidades](02_Entidades.md) | Sistema base de entidades |
| 03 | [Clases_Jugador](03_Clases_Jugador.md) | Clases jugables y módulos |
| 04 | [Enemigos](04_Enemigos.md) | Sistema de enemigos |
| 10 | [IA](10_IA.md) | Inteligencia artificial de enemigos |

---

## Combate y Party

| # | Documento | Descripción |
|---|-----------|-------------|
| **17** | [**Sistema_Combate**](17_Sistema_Combate.md) | **Sistema de combate dinámico** ⭐ |
| **18** | [**Sistema_Party**](18_Sistema_Party.md) | **Gestión de party: Main/Active/Stationed** ⭐ |
| 05 | [Habilidades](05_Habilidades.md) | Sistema de habilidades |
| 06 | [Efectos](06_Efectos.md) | Efectos de habilidades |
| 07 | [Estados](07_Estados.md) | Estados alterados |
| 08 | [Elementos](08_Elementos.md) | Sistema elemental |
| 13 | [Cooldowns](13_Cooldowns.md) | Sistema de cooldowns |
| 21 | [Sistema_Dano](21_Sistema_Dano.md) | Pipeline de daño |

---

## Progresión (Multi-Personaje)

| # | Documento | Descripción |
|---|-----------|-------------|
| **16** | [**Evoluciones_Traits_Chains**](16_Evoluciones_Traits_Chains.md) | **Traits, cadenas y condiciones como SOs** ⭐ |
| **19** | [**Evoluciones**](19_Evoluciones.md) | **Sistema data-driven: EvolutionState per-personaje, GlobalPlayerState** ⭐ |
| **22** | [**Misiones**](22_Misiones.md) | **Misiones: Global/Personal/Exclusive, MissionManager** ⭐ |

---

## UI

| # | Documento | Descripción |
|---|-----------|-------------|
| 11 | [UI_Reactiva](11_UI_Reactiva.md) | Sistema de UI reactiva |
| 20 | [UI](20_UI.md) | Sistemas de UI general |

### Selección de Personaje (UI Toolkit)
| Archivo | Ubicación | Descripción |
|---------|-----------|-------------|
| `CharacterSelectionConfig.cs` | Scripts/CharacterSelection/ | SO con clases, prefab, límites |
| `CharacterSelectionManager.cs` | Scripts/CharacterSelection/ | Lógica de creación y transición |
| `CharacterSelectionUI.cs` | Scripts/CharacterSelection/ | Controlador UI Toolkit |
| `CharacterSelectionBootstrap.cs` | Scripts/CharacterSelection/ | Setup de escena |
| `CharacterSelection.uxml` | UI_Toolkit/ | Layout UXML |
| `CharacterSelection.uss` | UI_Toolkit/ | Estilos USS |

---

## Mundo y Generación

| # | Documento | Descripción |
|---|-----------|-------------|
| 26 | [Sistema_Plantillas_Spawn](26_Sistema_Plantillas_Spawn.md) | Plantillas de spawn de enemigos |
| 27 | [Editor_Visual_Chunks](27_Editor_Visual_Chunks.md) | Editor visual de chunks |
| 28 | [Sistema_Camara](28_Sistema_Camara.md) | Cámara isométrica |
| 29 | [Stack_FlowController](29_Stack_FlowController.md) | Game flow controller |
| 30 | [Sistema_De_Biomas](30_Sistema_De_Biomas.md) | Sistema de biomas |

---

## Documentación Técnica

| Documento | Descripción |
|-----------|-------------|
| [CORRECCIONES_APLICADAS](CORRECCIONES_APLICADAS.md) | Correcciones de pooling (Feb 2026) |
| [GUIA_CHUNK_INTEGRATION](GUIA_CHUNK_INTEGRATION.md) | Guía de integración de chunks |
| [GUIA_SETUP_POOLING](GUIA_SETUP_POOLING.md) | Setup de pooling |
| [GUIA_SETUP_TESTING](GUIA_SETUP_TESTING.md) | Setup de testing |

---

## Arquitectura Multi-Personaje (Resumen)

```
┌──────────────────────────────────────────────────────────────┐
│                     ESTADO POR PERSONAJE                      │
│                                                              │
│   EntityController ──► EvolutionState (per-character)        │
│        │                   ├── kills, karma, traits, etc.    │
│        │                   └── misionesCompletadas           │
│        ▼                                                     │
│   PlayerPartyManager ◄──► MissionManager                     │
│   (Main/Party/Station)    (Global/Personal/Exclusive)        │
│                                                              │
│              GlobalPlayerState (compartido)                   │
│              ├── traitsGlobalmenteBloqueados                  │
│              ├── misionesExclusivasAsignadas                  │
│              └── misionesGlobalesCompletadas                  │
│                                                              │
│              EvolutionController (evaluación per-character)   │
│              └── Conditions evalúan EvolutionState individual │
└──────────────────────────────────────────────────────────────┘
```

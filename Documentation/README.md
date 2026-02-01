# Índice de Documentación - Saclisam

## Guías Generales
| # | Documento | Descripción |
|---|-----------|-------------|
| 00 | [GUIA_UNITY](00_GUIA_UNITY.md) | Guía de configuración de Unity |
| -- | [Guia](Guia.md) | Guía general del proyecto |
| -- | [TODO](TODO.md) | Estado y tareas pendientes |

---

## Arquitectura y Sistemas Core

| # | Documento | Descripción |
|---|-----------|-------------|
| 01 | [Arquitectura](01_Arquitectura.md) | Visión general de la arquitectura |
| 09 | [EventBus](09_EventBus.md) | Sistema de eventos desacoplado |
| 12 | [Guardado](12_Guardado.md) | Sistema de persistencia |
| 14 | [ObjectPool](14_ObjectPool.md) | Pool de objetos reutilizables |
| 15 | [SceneReference](15_SceneReference.md) | Referencias a escenas |

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
| -- | [Evoluciones](Evoluciones.md) | Documentación adicional de evoluciones |

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

## Mapa de Sistemas (Nuevo)

```
┌─────────────────────────────────────────────────────────────────┐
│                         SACLISAM                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  EXPLORACIÓN          COMBATE              PROGRESIÓN           │
│  ────────────         ───────              ──────────           │
│  PlayerInterestZone   CombateManager       Evoluciones          │
│  PlayerPartyManager   TurnManager          XP/Niveles           │
│        │              CombatEncounter      Habilidades          │
│        │                   │                    │               │
│        └───────────────────┼────────────────────┘               │
│                            │                                    │
│                       EventBus                                  │
│                      (Comunicación)                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Quick Reference

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
```

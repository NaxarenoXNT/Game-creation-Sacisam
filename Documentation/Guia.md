# Documentación del Proyecto Saclisam

## Índice de Documentación

Este proyecto es un sistema de combate RPG por turnos desarrollado en Unity. La documentación está organizada en los siguientes archivos:

| Archivo | Descripción |
|---------|-------------|
| [00_GUIA_UNITY.md](00_GUIA_UNITY.md) | **⭐ LEER PRIMERO** - Cambios a realizar en el Editor de Unity |
| [01_Arquitectura.md](01_Arquitectura.md) | Visión general de la arquitectura del proyecto |
| [02_Entidades.md](02_Entidades.md) | Sistema de entidades (Entidad, Jugador, Enemigos) |
| [03_Clases_Jugador.md](03_Clases_Jugador.md) | Clases jugables (Guerrero, etc.) |
| [04_Enemigos.md](04_Enemigos.md) | Tipos de enemigos (Goblin, Orcos, Dragon) |
| [05_Habilidades.md](05_Habilidades.md) | Sistema de habilidades y HabilidadData |
| [06_Efectos.md](06_Efectos.md) | Efectos de habilidades (Daño, Curación, Estados) |
| [07_Estados.md](07_Estados.md) | Sistema de estados alterados |
| [08_Elementos.md](08_Elementos.md) | Sistema elemental y EntityStats |
| [09_EventBus.md](09_EventBus.md) | Sistema de eventos global |
| [10_IA.md](10_IA.md) | Sistema de IA modular |
| [11_UI_Reactiva.md](11_UI_Reactiva.md) | Sistema de UI con bindings |
| [12_Guardado.md](12_Guardado.md) | Sistema de guardado y carga |
| [13_Cooldowns.md](13_Cooldowns.md) | Sistema de cooldowns |
| [14_ObjectPool.md](14_ObjectPool.md) | Pool de objetos reutilizables |
| [15_SceneReference.md](15_SceneReference.md) | Referencias type-safe a escenas |

## Estructura de Carpetas

```
Assets/Scripts/
├── Controllers/           # Controladores de Unity (MonoBehaviours)
│   ├── EntityController.cs
│   └── EnemigosCont/
│       └── EnemyController.cs
├── Estados/               # Sistema de estados alterados
│   ├── EstadoActivo.cs
│   └── GestorEstados.cs
├── Flags/                 # Enums y flags del sistema
│   └── Tipo.cs
├── Habilidades/           # Sistema de cooldowns
│   └── GestorCooldowns.cs
├── IA/                    # Sistema de IA modular
│   └── SistemaIA.cs
├── Interfaces/            # Interfaces del sistema
│   └── IEntidadCombate.cs
├── Managers/              # Managers y sistemas globales
│   ├── CombateManager.cs
│   ├── EventBus.cs
│   ├── GameConfig.cs
│   ├── ObjectPool.cs
│   ├── SaveSystem.cs
│   └── SceneReference.cs
├── Padres/                # Clases base abstractas
│   ├── Entidad.cs
│   ├── Jugador.cs
│   └── Enemigos.cs
├── SO/                    # ScriptableObjects
│   ├── ClaseData.cs
│   ├── EnemigoData.cs
│   ├── HabilidadData.cs
│   ├── ElementDefinition.cs
│   └── GameConfig.cs
├── Subclases/             # Implementaciones concretas
│   ├── Guerrero.cs
│   ├── Goblin.cs
│   ├── Orcos.cs
│   └── Dragon.cs
├── Todohabilidades/       # Efectos de habilidades
│   ├── DamageEffect.cs
│   ├── HealEffect.cs
│   └── StatusEffect.cs
└── UI/                    # Sistema de UI reactiva
    └── UIReactiva.cs
```

## Namespaces Principales

- `Padres` - Clases base (Entidad, Jugador, Enemigos)
- `Flags` - Enumeraciones (TipoEntidades, ElementAttribute, StatusFlag, CombatStyle)
- `Interfaces` - Interfaces del sistema (IEntidadCombate, IDamageable, IHealable, etc.)
- `Habilidades` - Sistema de cooldowns y comandos
- `Managers` - Sistemas globales (EventBus, SaveSystem, ObjectPool)
- `IA` - Sistema de IA modular
- `UI` - Sistema de UI reactiva

## Inicio Rápido

1. **Leer** [00_GUIA_UNITY.md](00_GUIA_UNITY.md) para adaptar el proyecto en el Editor
2. **Crear** ScriptableObjects necesarios (ClaseData, EnemigoData, HabilidadData)
3. **Configurar** GameConfig con los ElementDefinition
4. **Añadir** EntityController a los jugadores y EnemyController a los enemigos
5. **Configurar** CombateManager en la escena de combate

## Convenciones de Código

- **Nombres de métodos**: Sin caracteres especiales (ñ, á, etc.)
  - `RecibirDano` en lugar de `RecibirDaño`
  - `CalcularDanoContra` en lugar de `CalcularDañoContra`
- **Eventos**: Prefijo `On` + nombre en PascalCase
  - `OnVidaCambiada`, `OnMuerte`, `OnNivelSubido`
- **Interfaces**: Prefijo `I` + nombre descriptivo
  - `IEntidadCombate`, `IDamageable`, `IHealable`

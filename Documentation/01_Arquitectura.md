# Arquitectura del Proyecto

## Visión General

El proyecto sigue una arquitectura de separación entre **lógica pura** y **componentes de Unity**:

```
┌─────────────────────────────────────────────────────────────┐
│                    UNITY (MonoBehaviours)                    │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ EntityController │  │ EnemyController │                   │
│  │   + EntityStats  │  │   + EntityStats │                   │
│  └────────┬────────┘  └────────┬────────┘                   │
│           │                    │                             │
│           ▼                    ▼                             │
│  ┌─────────────────────────────────────────┐                │
│  │         LÓGICA PURA (C# Classes)         │                │
│  │  ┌─────────┐  ┌─────────┐  ┌──────────┐ │                │
│  │  │ Jugador │  │Enemigos │  │ Entidad  │ │                │
│  │  └────┬────┘  └────┬────┘  └────▲─────┘ │                │
│  │       └────────────┴────────────┘       │                │
│  └─────────────────────────────────────────┘                │
│                                                              │
│  ┌─────────────────────────────────────────┐                │
│  │       DATOS (ScriptableObjects)          │                │
│  │  ClaseData │ EnemigoData │ HabilidadData │                │
│  └─────────────────────────────────────────┘                │
└─────────────────────────────────────────────────────────────┘
```

## Capas del Sistema

### 1. Capa de Datos (ScriptableObjects)
- **ClaseData**: Define estadísticas base y tipo de clase jugable
- **EnemigoData**: Define estadísticas y comportamiento de enemigos
- **HabilidadData**: Define habilidades con efectos encadenados
- **ElementDefinition**: Define bonificaciones elementales
- **GameConfig**: Configuración global del juego

### 2. Capa de Lógica (Clases C# Puras)
- **Entidad**: Clase base abstracta con vida, ataque, defensa
- **Jugador**: Extiende Entidad con mana y progresión
- **Enemigos**: Extiende Entidad con IA y drops

### 3. Capa de Unity (MonoBehaviours)
- **EntityController**: Conecta Jugador con Unity
- **EnemyController**: Conecta Enemigos con Unity
- **EntityStats**: Gestiona elementos y bonificaciones
- **CombateManager**: Orquesta el combate por turnos

### 4. Capa de Sistemas Globales
- **EventBus**: Comunicación desacoplada
- **SaveSystem**: Persistencia de datos
- **ObjectPool**: Reutilización de GameObjects

## Patrón de Comunicación

```
┌────────────┐     Eventos     ┌────────────┐
│  Sistema A │ ──────────────► │  Sistema B │
└────────────┘                 └────────────┘
      │                              │
      │    EventBus.Publicar()       │
      └──────────────────────────────┘
```

### Ejemplo de flujo de daño:
1. `HabilidadData.Ejecutar()` crea DamageEffect
2. `DamageEffect.Aplicar()` llama a `objetivo.RecibirDano()`
3. `Entidad.RecibirDano()` calcula mitigación y aplica daño
4. `Entidad` dispara evento `OnDañoRecibido`
5. `EntityController` escucha y actualiza visuales
6. `EventBus` notifica a sistemas interesados

## Interfaces Principales

```csharp
// Interfaz completa de combate
IEntidadCombate : IDamageable, IHealable, IStatusReceiver, IIdentificable

// Interfaces granulares
IDamageable     → RecibirDano()
IHealable       → Curar()
IStatusReceiver → AplicarEstado(), TieneEstado(), RemoverEstado()
IIdentificable  → Nombre_Entidad, Nivel_Entidad, TipoEntidad

// Interfaces de comportamiento
IEntidadActuable   → ObtenerAccionElegida()
IGestorHabilidades → PuedeUsarHabilidad(), IniciarCooldown()
```

## Diagrama de Dependencias

```
                    ┌─────────────┐
                    │   Flags     │
                    │ (Enums)     │
                    └──────┬──────┘
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ Interfaces  │    │   Padres    │    │     SO      │
│             │◄───│  (Entidad)  │◄───│ (ClaseData) │
└─────────────┘    └─────────────┘    └─────────────┘
         │                 │
         │    ┌────────────┴────────────┐
         │    ▼                         ▼
         │  ┌─────────────┐    ┌─────────────┐
         │  │  Subclases  │    │  Estados    │
         │  │  (Goblin)   │    │(GestorEst.) │
         │  └─────────────┘    └─────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│            Controllers                   │
│  (EntityController, EnemyController)     │
└─────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────┐
│              Managers                    │
│  (CombateManager, EventBus, SaveSystem)  │
└─────────────────────────────────────────┘
```

## Principios de Diseño

1. **Separación de Responsabilidades**: Lógica separada de Unity
2. **Composición sobre Herencia**: Efectos componibles en habilidades
3. **Inversión de Dependencias**: Interfaces para desacoplamiento
4. **Patrón Command**: HabilidadData implementa IHabilidadesCommand
5. **Patrón Observer**: Eventos para comunicación reactiva
6. **Singleton Controlado**: GameConfig con validación

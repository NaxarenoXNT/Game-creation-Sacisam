# Sistema de Entidades

> Documentación de la capa base de entidades: clase abstracta `Entidad`, interfaces de combate y progresión.
> Para detalles de clases de jugador ver [03_Clases_Jugador.md](03_Clases_Jugador.md).
> Para detalles de enemigos ver [04_Enemigos.md](04_Enemigos.md).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Jerarquía de Clases](#jerarquía-de-clases)
- [Interfaces del Sistema](#interfaces-del-sistema)
- [Entidad (Clase Base)](#entidad-clase-base)
- [Jugador (Abstracta)](#jugador-abstracta)
- [Enemigos (Abstracta)](#enemigos-abstracta)
- [Diagramas de Flujo](#diagramas-de-flujo)
- [⚠ TODOs en código](#-todos-en-código)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Padres/Entidad.cs](../Assets/Scripts/Padres/Entidad.cs) | Clase base abstracta para todas las entidades |
| [Assets/Scripts/Padres/Jugador.cs](../Assets/Scripts/Padres/Jugador.cs) | Clase abstracta de jugadores — ver [03_Clases_Jugador.md](03_Clases_Jugador.md) |
| [Assets/Scripts/Padres/Enemigos.cs](../Assets/Scripts/Padres/Enemigos.cs) | Clase abstracta de enemigos — ver [04_Enemigos.md](04_Enemigos.md) |
| [Assets/Scripts/Interfaces/IEntidadCombate.cs](../Assets/Scripts/Interfaces/IEntidadCombate.cs) | Interfaz principal de combate (IDamageable, IHealable, etc.) |
| [Assets/Scripts/Interfaces/IJugadorProgresion.cs](../Assets/Scripts/Interfaces/IJugadorProgresion.cs) | Interfaz de progresión de jugador |
| [Assets/Scripts/Interfaces/IRecursoProvider.cs](../Assets/Scripts/Interfaces/IRecursoProvider.cs) | Interfaz genérica de recursos (Mana, Fe, etc.) |
| [Assets/Scripts/Subclases/JugadorFactory.cs](../Assets/Scripts/Subclases/JugadorFactory.cs) | Factory estática de instancias de Jugador |
| [Assets/Scripts/SO/ClaseData.cs](../Assets/Scripts/SO/ClaseData.cs) | ScriptableObject de configuración de clase jugador |
| [Assets/Scripts/SO/EnemigoData.cs](../Assets/Scripts/SO/EnemigoData.cs) | ScriptableObject de configuración de enemigo |
| [Assets/Scripts/Controllers/EntityController.cs](../Assets/Scripts/Controllers/EntityController.cs) | Componente Unity que envuelve una entidad lógica |

---

## Jerarquía de Clases

```
Entidad (abstracta) : IEntidadCombate
    ├── Jugador (abstracta) : IJugadorProgresion, IRecursoProvider
    │       ├── Guerrero
    │       ├── Arquero
    │       └── Mago
    │
    └── Enemigos (abstracta) : IEntidadActuable
            ├── Goblin
            ├── Orcos
            └── Dragon
```

---

## Interfaces del Sistema

**Archivo**: `Assets/Scripts/Interfaces/IEntidadCombate.cs`

`IEntidadCombate` es la interfaz principal que implementa `Entidad`. Está compuesta por cuatro interfaces granulares:

| Interfaz | Responsabilidad |
|----------|----------------|
| `IDamageable` | `RecibirDano(int, ElementAttribute)` |
| `IHealable` | `Curar(int)` |
| `IStatusReceiver` | `AplicarEstado`, `TieneEstado`, `RemoverEstado` |
| `IIdentificable` | `Nombre_Entidad`, `Nivel_Entidad`, `TipoEntidad`, `EsTipoEntidad` |

Además expone directamente: `CombatStats`, stats de combate, `EsDerrotado`, `EstaMuerto`, `EstaVivo()`, `PuedeActuar()`, `UsaEstiloDeCombate()`, `CalcularDanoContra()`, `AplicarDanoDesdeContexto()`.

**Archivo**: `Assets/Scripts/Interfaces/IJugadorProgresion.cs`  
Expone: `Nivel_Entidad`, `Experiencia_Actual`, `Experiencia_Progreso`, `Mana_jugador`, `ManaActual_jugador`, `RecibirXP()`, eventos de nivel y mana.

**Archivo**: `Assets/Scripts/Interfaces/IRecursoProvider.cs`  
Permite al sistema de habilidades verificar y consumir recursos de forma genérica, independientemente del tipo (Mana, Energía, Sangre, etc.). Métodos: `ObtenerRecursoActual`, `ObtenerRecursoMaximo`, `TieneRecursoSuficiente`, `ConsumirRecurso`, `RestaurarRecurso`, `PoseeRecurso`. Evento: `OnRecursoCambiado`.

---

## Entidad (Clase Base)

**Archivo**: `Assets/Scripts/Padres/Entidad.cs`  
**Namespace**: `Padres`

### Propiedades Principales

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Vida_Entidad` | int | Vida máxima |
| `VidaActual_Entidad` | int | Vida actual |
| `PuntosDeAtaque_Entidad` | int | Ataque base |
| `PuntosDeDefensa_Entidad` | float | Defensa (mitigación) |
| `Velocidad` | int | Determina orden de turnos |
| `Nivel_Entidad` | int | Nivel actual |
| `Nombre_Entidad` | string | Nombre para mostrar |
| `EsDerrotado` | bool | Si fue derrotado en combate |
| `EstaMuerto` | bool | Si vida llegó a 0 |
| `GestorEstados` | `GestorEstados` | Maneja estados alterados |
| `GestorPasivas` | `GestorPasivas` | Maneja habilidades pasivas |
| `CombatStats` | `CombatStats` | Estadísticas de crítico, elemental y resistencias |
| `EntityDamageModifiers` | `List<IDamageModifier>` | Modificadores de daño activos (pasivas, traits) |
| `EffectHandler` | `EffectHandler` | Handler de efectos activos (lazy init) |
| `Experiencia_Actual` | float | XP acumulada en el nivel actual |
| `Experiencia_Progreso` | float | XP necesaria para subir de nivel |

### Propiedades Abstractas

```csharp
public abstract TipoEntidades TipoEntidad { get; }
public abstract ElementAttribute AtributosEntidad { get; }
```

### Métodos Principales

```csharp
// Verificación de estado
bool EstaVivo()      // VidaActual > 0 && !EstaMuerto
bool PuedeActuar()   // EstaVivo && !EsDerrotado && !GestorEstados.EstaIncapacitado

// Identidad
void Renombrar(string nuevoNombre)  // Actualiza Nombre_Entidad en runtime

// Combate — vía pipeline estándar
void AplicarDanoDesdeContexto(DamageContext context)   // método preferido
void RecibirDano(int danoBruto, ElementAttribute tipo) // pipeline simplificado
void RecibirDanoPuro(int danoBruto, ElementAttribute tipo) // ignora defensa
int  Curar(int cantidad)
int  CalcularDanoContra(IEntidadCombate objetivo)
DamageResult CalcularDanoContraConResultado(IEntidadCombate objetivo)

// Estados
void  AplicarEstado(StatusFlag status, int duracion, int danoPorTurno, float modificador)
bool  TieneEstado(StatusFlag status)
void  RemoverEstado(StatusFlag status)
bool  ProcesarEstadosInicioTurno()   // true = puede actuar; aplica daño de estados y notifica

// Modificadores de stats (para pasivas y buffs)
void  ModificarVidaMaxima(int cantidad)
void  ModificarAtaque(int cantidad)
virtual void ModificarDefensa(float cantidad)
void  ModificarVelocidad(int cantidad)

// Abstractos (implementar en subclases)
abstract bool EsTipoEntidad(TipoEntidades tipo)
abstract bool UsaEstiloDeCombate(CombatStyle estilo)
```

### Eventos

```csharp
event Action<int, int> OnVidaCambiada;  // (vidaActual, vidaMaxima)
event Action<int> OnDañoRecibido;       // (cantidad ya mitigada)
event Action OnMuerte;
```

Al morir también se publica `EventoMuerte` en el `EventBus` con referencia a la entidad y al asesino.

### Fórmula de Daño Completa

`CalcularDanoContraConResultado` usa el `DamageCalculator` central:

```
BASE_OFFENSE  = (ATK + ELEM_ATK) * RACE_ATK
OFFENSE       = BASE_OFFENSE * (isCrit ? CRIT_MULT : 1)
DEF_MULT      = 1 / (1 + ln(1 + DEF * RACE_DEF) / K)   ← K configurable en CombatConfig
PHYS_DAMAGE   = OFFENSE * DEF_MULT
ELEM_DAMAGE   = ELEM_ATK * clamp(1 - RES_e, 0.1, 1.5)
FINAL_DAMAGE  = PHYS_DAMAGE + ELEM_DAMAGE
```

`RecibirDano` ejecuta estos pasos internamente:
1. `AplicarMitigacionPorFaccion` (virtual, override en subclases como NoMuerto/Elemental)
2. `DamageCalculator.CalculateDefenseMultiplier` (fórmula logarítmica con K de `CombatConfig`)
3. Resistencia elemental de `CombatStats.resistencias`
4. Aplica daño, lanza eventos, llama `Morir()` si `VidaActual <= 0`

---

## Jugador (Abstracta)

**Archivo**: `Assets/Scripts/Padres/Jugador.cs` — **Namespace**: `Padres`

`Jugador` extiende `Entidad` e implementa `IJugadorProgresion` e `IRecursoProvider`. Añade Mana, sistema de progresión con XP, escalado por nivel configurable (`EscaladoJugador`), `GestorHabilidades`, hooks de clase (B3–B9) y el sistema de módulos de evolución.

> Para la documentación completa de `Jugador` y todas sus subclases (Guerrero, Mago, Arquero) ver **[03_Clases_Jugador.md](03_Clases_Jugador.md)**.

---

## Enemigos (Abstracta)

**Archivo**: `Assets/Scripts/Padres/Enemigos.cs` — **Namespace**: `Padres`

`Enemigos` extiende `Entidad` e implementa `IEntidadActuable`. Añade `XPOtorgada`, `GestorHabilidades`, `HabilidadPorDefecto`, `CerebroIA` y `ObtenerAccionElegida`. Las subclases definen su escalado por nivel con `EscaladoEnemigo` (`static readonly`).

> Para la documentación completa de `Enemigos` y todas sus subclases (Goblin, Orcos, Dragon) ver **[04_Enemigos.md](04_Enemigos.md)**.

---

## Diagramas de Flujo

### Recibir Daño (`AplicarDanoDesdeContexto` — pipeline preferido)

```
AplicarDanoDesdeContexto(context)
        │
        ▼
┌───────────────────────────┐
│  VidaActual -= FinalDamage│
│  (ya calculado en pipeline)│
└───────────┬───────────────┘
            │
            ▼
┌───────────────────────────┐
│ OnDañoRecibido.Invoke     │
│ OnVidaCambiada.Invoke     │
└───────────┬───────────────┘
            │
            ▼
   ¿IsCritical && Attacker?
   ─── Sí → EffectHandler.NotifyCriticalHit
            │
            ▼
    ┌───────┴───────┐
    │ VidaActual<=0 │
    └───────┬───────┘
            │ Sí
            ▼
┌───────────────────────────┐
│ Morir(context.Attacker)   │
│ EstaMuerto = true         │
│ OnMuerte.Invoke           │
│ EventBus.Publicar(EventoMuerte)│
└───────────┬───────────────┘
            │
            ▼
    ¿Attacker es Jugador?
    ─── Sí → jugador.AlEliminar(this)
            │
            ▼
    jugadorAtacante.PostAtaqueConContexto(ctx, objetivoMurio)
```

### Recibir Daño (`RecibirDano` — pipeline simplificado)

```
RecibirDano(danoBruto, tipo)
        │
        ▼
AplicarMitigacionPorFaccion (virtual)
        │
        ▼
CalculateDefenseMultiplier(DEF, K)
   DEF_MULT = 1 / (1 + ln(1 + DEF) / K)
        │
        ▼
Resistencia elemental de CombatStats
   elemMult = clamp(1 - resistencia, 0.1, 1.5)
        │
        ▼
VidaActual -= danoMitigado (mín 1)
        │
        ▼
OnDañoRecibido / OnVidaCambiada
        │
        ▼
¿VidaActual <= 0? → Morir(_ultimoAtacante)
```

---

## ⚠ TODOs encontrados en código

> Extraídos de archivos de entidad base y controladores.

- **`EntityController.cs:352`** — `TODO: Integrar con sistema de UI para seleccion manual` — `ObtenerAccionElegida` siempre usa la primera habilidad disponible y el primer enemigo vivo; no hay selección interactiva del jugador todavía.
- **`EntityController.cs:392`** — `TODO: UI para seleccion` — El caso `EnemigoUnico` y `EnemigoTodos` en la selección de objetivo usa `Find(e => e.EstaVivo())` como placeholder.
- **`EnemyController.cs:358`** — `TODO: Pasar atacante si está disponible` — Al publicar `EventoEnemigoDerrotado`, el campo `Asesino` se fija siempre en `null`; se necesita propagar el atacante desde el pipeline de daño.

> Los TODOs del módulo `HeraldoCaidoModulo` y los de `EvolutionController` están documentados en [03_Clases_Jugador.md](03_Clases_Jugador.md).
> Los TODOs específicos de enemigos están documentados en [04_Enemigos.md](04_Enemigos.md).


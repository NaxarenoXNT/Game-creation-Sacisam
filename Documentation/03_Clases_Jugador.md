# Clases del Jugador

> Documentación de la clase abstracta `Jugador` y todas sus subclases concretas (Guerrero, Mago, Arquero), el sistema de módulos de evolución, y la configuración via `ClaseData`.

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Visión General](#visión-general)
- [Jugador (Clase Abstracta)](#jugador-clase-abstracta)
  - [Propiedades Adicionales](#propiedades-adicionales)
  - [Inicialización](#inicialización)
  - [Sistema de Progresión](#sistema-de-progresión)
  - [Sistema de Recursos](#sistema-de-recursos-irecursoprovider)
  - [Hooks de Comportamiento de Clase](#hooks-de-comportamiento-de-clase)
  - [Sistema de Módulos (B9)](#sistema-de-módulos-b9)
- [Guerrero](#guerrero)
- [Mago](#mago)
- [Arquero](#arquero)
- [Sistema de Módulos de Evolución](#sistema-de-módulos-de-evolución)
- [ClaseData (ScriptableObject)](#clasedata-scriptableobject)
- [JugadorFactory](#jugadorfactory)
- [Crear una Nueva Clase de Jugador](#crear-una-nueva-clase-de-jugador)
- [Tabla Comparativa](#tabla-comparativa)
- [⚠ TODOs en código](#-todos-en-código)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Padres/Jugador.cs](../Assets/Scripts/Padres/Jugador.cs) | Clase abstracta base para todos los jugadores |
| [Assets/Scripts/Subclases/ClasesDelJugador/Guerrero.cs](../Assets/Scripts/Subclases/ClasesDelJugador/Guerrero.cs) | Clase Guerrero |
| [Assets/Scripts/Subclases/ClasesDelJugador/Mago.cs](../Assets/Scripts/Subclases/ClasesDelJugador/Mago.cs) | Clase Mago |
| [Assets/Scripts/Subclases/ClasesDelJugador/Arquero.cs](../Assets/Scripts/Subclases/ClasesDelJugador/Arquero.cs) | Clase Arquero |
| [Assets/Scripts/Subclases/Modulos/IComportamientoDeClase.cs](../Assets/Scripts/Subclases/Modulos/IComportamientoDeClase.cs) | Interfaz para módulos de evolución |
| [Assets/Scripts/Subclases/Modulos/PaladinModulo.cs](../Assets/Scripts/Subclases/Modulos/PaladinModulo.cs) | Módulo Paladín (evolución de Guerrero) |
| [Assets/Scripts/Subclases/Modulos/HeraldoCaidoModulo.cs](../Assets/Scripts/Subclases/Modulos/HeraldoCaidoModulo.cs) | Módulo Heraldo Caído (stub, pendiente) |
| [Assets/Scripts/Subclases/Modulos/ModuloClaseSO.cs](../Assets/Scripts/Subclases/Modulos/ModuloClaseSO.cs) | SO base abstracto para módulos serializables en Unity |
| [Assets/Scripts/Subclases/Modulos/PaladinModuloSO.cs](../Assets/Scripts/Subclases/Modulos/PaladinModuloSO.cs) | SO concreto para el módulo Paladín |
| [Assets/Scripts/Subclases/Modulos/HeraldoCaidoModuloSO.cs](../Assets/Scripts/Subclases/Modulos/HeraldoCaidoModuloSO.cs) | SO concreto para el módulo Heraldo Caído |
| [Assets/Scripts/SO/ClaseData.cs](../Assets/Scripts/SO/ClaseData.cs) | ScriptableObject de configuración de clase |
| [Assets/Scripts/Subclases/JugadorFactory.cs](../Assets/Scripts/Subclases/JugadorFactory.cs) | Factory estática de instancias de Jugador |
| [Assets/Scripts/Evolution/EvolutionController.cs](../Assets/Scripts/Evolution/EvolutionController.cs) | Controlador de evoluciones de clase |
| [Assets/Scripts/Evolution/EvolutionApplier.cs](../Assets/Scripts/Evolution/EvolutionApplier.cs) | Aplicador de efectos de evolución |

---

## Visión General

Las clases del jugador heredan de `Jugador` (abstracta) y añaden mecánicas únicas de clase.
Además, cada clase puede recibir **módulos de evolución** (`IComportamientoDeClase`) en runtime
sin necesidad de cambiar su tipo de objeto.

```
Jugador (abstracta) : Entidad, IJugadorProgresion, IRecursoProvider
├── Guerrero      ← mecánica: +15% a toda ganancia de defensa
├── Mago          ← mecánica: distribución XP 60/40 (más XP a elementos)
└── Arquero       ← mecánica: crítico garantizado mientras está en sigilo
       +
   List<IComportamientoDeClase>   ← módulos de evolución (Paladín, HeraldoCaído...)
```

---

## Jugador (Clase Abstracta)

**Archivo**: `Assets/Scripts/Padres/Jugador.cs` — **Namespace**: `Padres`

### Propiedades Adicionales

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Mana_jugador` | `int` | Mana máximo |
| `ManaActual_jugador` | `int` | Mana actual |
| `GestorHabilidades` | `GestorHabilidades` | Gestiona habilidades activas |
| `Modulos` | `IReadOnlyList<IComportamientoDeClase>` | Módulos de evolución activos |
| `entityStats` | `EntityStats` | Referencia al componente Unity (vinculado post-construcción) |

### Eventos Adicionales

```csharp
event Action<int> OnNivelSubido;                           // (nuevoNivel)
event Action<float, float> OnXPGanada;                     // (xpActual, xpNecesaria)
event Action<int, int> OnManaCambiado;                     // (manaActual, manaMaximo)
event Action<TipoRecurso, float, float> OnRecursoCambiado; // (tipo, actual, maximo)
```

### Inicialización

```csharp
// Llamar después de construir la instancia (lo hace ClaseData.CrearInstancia)
void InicializarDesdeClaseData(ClaseData datos)
    // Configura GestorHabilidades con límite de slots
    // Agrega pasivas iniciales al GestorPasivas
    // Llama InicializarComportamientoDeClase()

// Vinculación con el componente Unity (post-construcción)
void VincularEntityStats(EntityStats stats)
```

### Sistema de Progresión

```csharp
void RecibirXP(float xp)
// La XP se divide entre el jugador y sus elementos activos:
//   XP jugador   = xp * PropXPJugador   (default: 0.80)
//   XP elementos = xp * PropXPElementos (default: 0.20)
// Subclases sobrescriben las proporciones con propiedades virtuales (ej: Mago 0.60/0.40)

static int EscaladoExperiencia(int nivel)
// Fórmula exponencial: 100 * (1.10)^(nivel-2)
// Cap de crecimiento en nivel 60 -> post-60 usa tasa 2.5%
// Ejemplos: Nivel 2 = 100 XP | Nivel 10 aprox 195 XP | Nivel 60 aprox 11.739 XP
```

#### EscaladoJugador

Cada clase define su propio `EscaladoJugador` en el campo `escalado` del SO (`ClaseData`). Valores por defecto:

```csharp
public class EscaladoJugador
{
    public int vidaPorNivel      = 50;
    public int ataquePorNivel    = 5;
    public float defensaPorNivel = 2f;
    public int manaPorNivel      = 10;
    public int velocidadPorNivel = 1;
}
```

Al subir de nivel se restauran completamente la vida y el mana, y se llama a `entityStats.ActualizarBaseYRecalcular()`.

#### Flujo de Subida de Nivel

```
SubirNivel()
    ├── Nivel_Entidad++
    ├── AplicarEscaladoNivel()   <- subclases pueden override
    │       vida/atk/def/mana/vel += escalado.*PorNivel
    │       VidaActual = Vida, ManaActual = Mana
    ├── entityStats.ActualizarBaseYRecalcular()
    ├── OnNivelSubido.Invoke(Nivel)
    └── OnNivelSubidoClase(Nivel)  <- hook de clase (virtual)
```

### Sistema de Recursos (`IRecursoProvider`)

El recurso principal es `Mana` por defecto. Los módulos pueden hacer override via `OverridearRecursoPrincipal()` (sustitutivo, ej: Paladín → Fe).

```csharp
float ObtenerRecursoActual(TipoRecurso tipo)
float ObtenerRecursoMaximo(TipoRecurso tipo)
bool  TieneRecursoSuficiente(TipoRecurso tipo, float cantidad)
bool  ConsumirRecurso(TipoRecurso tipo, float cantidad)
void  RestaurarRecurso(TipoRecurso tipo, float cantidad)
bool  PoseeRecurso(TipoRecurso tipo)

// Ajustes internos (acceso interno/módulos)
internal void AjustarMana(int delta)
internal void AjustarManaPercent(float multiplicador)
```

Hooks virtuales para subclases:

```csharp
// Recurso principal consultado por todos los métodos IRecursoProvider.
// Módulos via OverridearRecursoPrincipal() pueden cambiarlo (sustitutivo, ej: Paladín → Fe).
protected virtual TipoRecurso RecursoPrincipal  // default: TipoRecurso.Mana

// Consumo del recurso principal. Override para comportamiento especial
// (ej: Hematómante consume HP si no tiene Sangre).
protected virtual bool ConsumoRecursoPrincipal(float cantidad)

// Regeneración por turno. Se invoca automáticamente al procesar estados de inicio de turno.
protected virtual void RegenerarRecursoPorTurno()

// Callback cuando un recurso se agota. Override para efectos especiales
// (ej: Berserker entra en furia cuando se queda sin recursos).
protected virtual void OnRecursoAgotado(TipoRecurso tipo, float deficit)
```

### Hooks de Comportamiento de Clase

Métodos virtuales que las subclases o módulos pueden extender:

| Región | Método | Propósito |
|--------|--------|-----------|
| B3 | `AlMorir(IEntidadCombate asesino)` | Pre-muerte (antes de marcar `EstaMuerto`) |
| B3 | `AlEliminar(Entidad victima)` | Post-kill del jugador |
| B4 | `TieneEfectoAmbiental` | Aura persistente (no implementado) |
| B5 | `AntesDeCastear(...)` | Cancela casteo si retorna `false` |
| B5 | `DespuesDeCastear(...)` | Post-cast |
| B6 | `InicializarComportamientoDeClase()` | Setup al crear la instancia |
| B6 | `LimpiarComportamientoDeClase()` | Cleanup al destruir/reciclar |
| B7 | `OnNivelSubidoClase(int nivel)` | Post-level-up específico de clase |
| B8 | `ForzarCritico()` | Garantiza crítico en próximo ataque (default: `false`) |
| B8 | `PostAtaqueConContexto(ctx, objetivoMurio)` | Post-ataque en pipeline |

### Sistema de Módulos (B9)

```csharp
void AgregarModulo(IComportamientoDeClase modulo)   // evita duplicados por ModuloId
void RemoverModulo(string moduloId)

// Hooks de módulos consultados automáticamente:
ElementAttribute ModificarElementoAtaqueDeModulos(ElementAttribute atribBase)
int ModificarCuracionOtorgada(int cantidadBase, IEntidadCombate objetivo)
// ModificarCuracionRecibida se consulta automáticamente al curar al jugador
```

---

## Guerrero

**Namespace**: `Subclases`
**Ruta**: `Assets/Scripts/Subclases/ClasesDelJugador/Guerrero.cs`

### Mecánica Única — Maestría Defensiva

Toda ganancia de defensa (por nivel **o** por fuente externa: traits, pasivas, evoluciones)
recibe un bonus adicional del **+15%**.

- **Subida de nivel**: `AplicarEscaladoNivel()` aplica el escalado base y luego suma `defensaPorNivel * 0.15f` encima.
- **Cualquier otra fuente**: `ModificarDefensa(float cantidad)` está override; si `cantidad > 0` la multiplica por `1.15f`. Las pérdidas de defensa no se ven afectadas.

```csharp
// Ejemplo: si defensaPorNivel = 2.0 y el jugador sube de nivel:
// Base: +2.0 DEF
// Bonus Guerrero: +0.3 DEF  (2.0 * 0.15)
// Total: +2.3 DEF

// Ejemplo: si un trait otorga +10 DEF:
// Base: +10.0 DEF
// Bonus Guerrero: +1.5 DEF  (10 * 0.15)
// Total: +11.5 DEF
```

**Constante**: `private const float BonusDefensaPorcentaje = 0.15f`

### Evolución disponible: Paladín

Se aplica via `PaladinModulo` (ver [Sistema de Módulos](#sistema-de-módulos-de-evolución)). Ver también [19_Evoluciones.md](19_Evoluciones.md).

---

## Mago

**Namespace**: `Subclases`
**Ruta**: `Assets/Scripts/Subclases/ClasesDelJugador/Mago.cs`

### Mecánica Única — Sinergia Elemental

La distribución de XP cambia de la estándar (80/20) a **60% jugador / 40% elementos**.
El Mago sube de nivel más lento pero sus elementos progresan significativamente más rápido.

| | Estándar | Mago |
|---|---|---|
| XP al jugador | 80% | 60% |
| XP a elementos | 20% | 40% |

Implementado sobreescribiendo las propiedades virtuales de `Jugador`:

```csharp
protected override float PropXPJugador   => 0.6f;
protected override float PropXPElementos => 0.4f;
```

---

## Arquero

**Namespace**: `Subclases`
**Ruta**: `Assets/Scripts/Subclases/ClasesDelJugador/Arquero.cs`

### Mecánica Única — Sigilo

Mientras el Arquero está en estado de sigilo, **cada ataque es un crítico garantizado**.

- Al atacar: si el objetivo **muere** del golpe, **permanece en sigilo**.
- Si el objetivo **sobrevive**, sale automáticamente del sigilo.

#### API pública

```csharp
Arquero arquero = ...;

// Activar sigilo (llamar desde habilidad, item, evento de mundo...)
arquero.EntrarEnSigilo();

// Consultar estado
bool estaOculto = arquero.EstaInvisible;

// Salir manualmente (normalmente ocurre automático al atacar sin matar)
arquero.SalirDeSigilo();
```

#### Flujo técnico

1. `ForzarCritico()` retorna `EstaInvisible` -> `Entidad.CalcularDanoContraConResultado()`
   lee este valor y fuerza `critChance = 1f` antes de ejecutar el pipeline.
2. `PostAtaqueConContexto(ctx, objetivoMurio)` (override de `Jugador`) llama
   `SalirDeSigilo()` si el objetivo sobrevivió.

---

## Sistema de Módulos de Evolución

Las clases **no cambian de tipo** al evolucionar; se les inyectan **módulos de comportamiento**
(`IComportamientoDeClase`) que modifican mecánicas en runtime.

**Archivo interfaz**: `Assets/Scripts/Subclases/Modulos/IComportamientoDeClase.cs`

### Reglas de consulta

| Tipo de hook | Iteración | Comportamiento |
|---|---|---|
| **Aditivo** (curación ±%) | 0 -> Count | Todos los módulos contribuyen en cadena |
| **Sustitutivo** (elemento de ataque, recurso) | Count-1 -> 0 | El módulo más reciente que responda gana |

### Hooks disponibles en `IComportamientoDeClase`

| Hook | Tipo | Descripción |
|---|---|---|
| `ModificarCuracionOtorgada` | Aditivo | Curación que el jugador da a otros |
| `ModificarCuracionRecibida` | Aditivo | Curación que el jugador recibe |
| `ModificarElementoAtaque` | Sustitutivo | Override del elemento de ataque |
| `OverridearRecursoPrincipal` | Sustitutivo | Cambiar Mana por Fe u otro recurso |

### API de módulos en `Jugador`

```csharp
// Agregar un módulo (evita duplicados por ModuloId)
jugador.AgregarModulo(modulo);

// Remover un módulo por ID
jugador.RemoverModulo("paladin");

// Leer la lista (read-only)
IReadOnlyList<IComportamientoDeClase> modulos = jugador.Modulos;
```

### Módulos implementados

| Módulo | ID | Evolución de | Archivo |
|---|---|---|---|
| `PaladinModulo` | `"paladin"` | Guerrero | `Assets/Scripts/Subclases/Modulos/PaladinModulo.cs` |
| `HeraldoCaidoModulo` | `"heraldo_caido"` | Paladín | `Assets/Scripts/Subclases/Modulos/HeraldoCaidoModulo.cs` *(stub)* |

### Ejemplo: Guerrero -> Paladín -> Heraldo Caído

```
Estado del jugador (Guerrero):    [  ]
Después de evolucionar a Paladín: [ PaladinModulo ]
Después de Heraldo Caído:         [ PaladinModulo, HeraldoCaidoModulo ]

Consulta de elemento (sustitutivo, iteración reversa):
  HeraldoCaidoModulo.ModificarElementoAtaque(None) -> Dark  <- gana
  PaladinModulo no se consulta.

Consulta de curación (aditivo, iteración normal):
  PaladinModulo.ModificarCuracionRecibida(100) -> 120   (+20%)
  HeraldoCaidoModulo.ModificarCuracionRecibida(120) -> 120  (sin cambio)
  Resultado: 120
```

### Crear un NPC ya evolucionado

En el `ClaseData` del NPC, agregar los módulos en el campo `modulosIniciales`:

1. Crear SO del módulo: *Assets -> Create -> Clases/Modulos/Paladin*
2. Arrastrarlo a `ClaseData.modulosIniciales`
3. `ClaseData.CrearInstancia()` los aplicará automáticamente al instanciar

---

## ClaseData (ScriptableObject)

**Archivo**: `Assets/Scripts/SO/ClaseData.cs`
`[CreateAssetMenu(menuName = "Combate/Clase Jugador")]`

| Header Unity | Campos |
|---|---|
| Info General | `nombreClase`, `iconoClase`, `descripcionClase` |
| Stats Base | `vidaBase`, `ataqueBase`, `defensaBase`, `manaBase`, `velocidadBase` |
| Escalado por Nivel | `escalado` (`EscaladoJugador`) |
| Atributos | `atributos`, `tipoEntidad`, `estiloCombate` |
| Visual y Animación | `animatorOverride`, `prefabProyectil` |
| Habilidades Iniciales | `habilidadesIniciales` (`List<HabilidadData>`), `pasivasIniciales` (`List<PasivaData>`) |
| Límites | `limiteHabilidadesActivas` (default 8), `limitePasivas` (default 4) |
| Módulos Iniciales | `modulosIniciales` — para NPCs ya evolucionados |

`CrearInstancia()` delega a `JugadorFactory.Crear(this)` y luego aplica los `modulosIniciales`.

> El campo `ubicacionInicialId` está en el SO pero el sistema de spawn por ID **no está implementado** todavía.

---

## JugadorFactory

**Archivo**: `Assets/Scripts/Subclases/JugadorFactory.cs`

Factory estática que resuelve la instancia correcta por `datos.nombreClase`:

```csharp
public static Jugador Crear(ClaseData datos)
//   "Guerrero" -> new Guerrero(datos)
//   "Mago"     -> new Mago(datos)
//   "Arquero"  -> new Arquero(datos)
//   otro       -> ArgumentException — agregar nuevo case
```

---

## Crear una Nueva Clase de Jugador

### Paso 1: Crear la subclase

```csharp
// Assets/Scripts/Subclases/ClasesDelJugador/NuevaClase.cs
using Padres;

namespace Subclases
{
    public class NuevaClase : Jugador
    {
        public NuevaClase(ClaseData datos) : base(
            datos.nombreClase, datos.vidaBase, datos.ataqueBase,
            datos.defensaBase, 1, datos.manaBase, datos.velocidadBase,
            datos.atributos, datos.tipoEntidad, datos.estiloCombate, datos.escalado)
        {
            InicializarDesdeClaseData(datos);
        }

        // Hooks opcionales a sobrescribir según la mecánica deseada:

        // Override de stats por nivel:
        protected override void AplicarEscaladoNivel() { base.AplicarEscaladoNivel(); /* extra */ }

        // Override de distribución de XP:
        protected override float PropXPJugador   => 0.7f;
        protected override float PropXPElementos => 0.3f;

        // Crítico garantizado bajo alguna condición:
        public override bool ForzarCritico() => false; /* condición propia */

        // Curación recibida modificada (si no usa módulos):
        protected override int ModificarCuracionRecibida(int cantidad) => cantidad;
    }
}
```

### Paso 2: Registrar en `JugadorFactory`

```csharp
// Assets/Scripts/Subclases/JugadorFactory.cs — agregar al switch:
"NuevaClase" => new NuevaClase(datos),
```

### Paso 3: Crear el SO de ClaseData en Unity

*Assets → Create → Combate → Clase Jugador* → configurar `nombreClase = "NuevaClase"` y el resto de stats.

---

## Tabla Comparativa

| Clase | Mecánica Única | XP al jugador | Implementación |
|---|---|---|---|
| Guerrero | +15% a toda ganancia de DEF | 80% | `Guerrero.cs` |
| Mago | XP 60/40 (más a elementos) | 60% | `Mago.cs` |
| Arquero | Crítico garantizado en sigilo | 80% | `Arquero.cs` |

> **Paladín** y **Heraldo Caído** son **módulos de evolución** (`IComportamientoDeClase`), no clases independientes. No aparecen como subclases de `Jugador`.

---

## ⚠ TODOs en código

> Extraídos de archivos de módulos y sistema de evolución.

- **`HeraldoCaidoModulo.cs:30`** — `TODO: agregar habilidades específicas del Heraldo Caído` — El módulo no registra ninguna habilidad nueva al adjuntarse; pendiente implementar el árbol de habilidades completo.
- **`HeraldoCaidoModulo.cs:36`** — `TODO: limpiar habilidades específicas del Heraldo Caído` — Al remover el módulo no se desregistra nada; debe limpiarse cuando se implemente la adición.
- **`EvolutionController.cs:86`** — `TODO: Agregar más suscripciones según necesites` — El método `SuscribirEventos()` tiene un placeholder para eventos adicionales como `EventoMisionCompletada`.
- **`EvolutionController.cs:264`** — `TODO: Llamar a tu sistema de cambio de clase` — Cuando `claseDestino != null`, el cambio de clase no se aplica todavía (`jugador.CambiarClase(...)` está comentado).
- **`EvolutionController.cs:373`** — `TODO: Sincronizar más datos iniciales si es necesario` — Al inicializar desde datos persistentes puede quedar desincronizado.
- **`EvolutionApplier.cs:36`** — `TODO: Combinar atributo elemental del jugador con efecto.elemento` — El atributo del jugador no se combina con el del efecto al aplicar evoluciones elementales.
- **`EvolutionApplier.cs:39`** — `TODO: Aplicar status pasivo persistente` — Al aplicar una evolución con efecto `AddStatusPassive`, no se aplica el estado pasivo.
- **`EvolutionApplier.cs:42`** — `TODO: Ajustar karma en EvolutionState` — Los efectos de tipo `KarmaDelta` no modifican el karma.
- **`EvolutionApplier.cs:45`** — `TODO: Ajustar reputación de facción` — Los efectos de tipo `ReputationDelta` no actualizan la reputación.
- **`EvolutionApplier.cs:48`** — `TODO: Marcar regla de mundo` — Los efectos de tipo `WorldRuleToggle` no marcan la regla de mundo.
- **`EvolutionApplier.cs:51`** — `TODO: Ajustar bias de IA global` — Los efectos de tipo `AITargetBias` no modifican el bias global de IA.
- **`EvolutionApplier.cs:54`** — `TODO: Ajustar peso en tablas de drop` — Los efectos de tipo `LootTableBias` no actualizan los pesos.
- **`EvolutionApplier.cs:60`** — `TODO: Ajustar cooldowns activos/base` — Los efectos de tipo `ModifyCooldowns` no modifican ningún cooldown.
- **`ClaseData` — `ubicacionInicialId`** — Campo presente en el SO pero el sistema de spawn por ID no está implementado todavía.


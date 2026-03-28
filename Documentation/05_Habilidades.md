# Sistema de Habilidades

> Documentación del sistema de habilidades activas y pasivas.
> Para detalles de efectos activos (daño, curación, estados) ver [06_Efectos.md](06_Efectos.md), [07_Estados.md](07_Estados.md) y [08_Elementos.md](08_Elementos.md).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Arquitectura del Sistema](#arquitectura-del-sistema)
- [HabilidadData](#habilidaddata)
  - [Propiedades](#propiedades)
  - [CostoRecurso](#costorecurso)
  - [Enums Clave](#enums-clave)
  - [Métodos Principales](#métodos-principales)
- [Efectos Activos (IHabilidadEffect)](#efectos-activos-ihabilidadeffect)
  - [DamageEffect](#damageeffect)
  - [HealEffect](#healeffect)
  - [StatusEffect](#statuseffect)
- [PasivaData](#pasivadata)
  - [Propiedades](#propiedades-1)
  - [Ciclo de Vida](#ciclo-de-vida)
- [Efectos Pasivos (IPasivaEffect)](#efectos-pasivos-ipasivaeffect)
  - [ModificadorStatEffect](#modificadorstateffect)
  - [RegeneracionEffect](#regeneracioneffect)
  - [ResistenciaElementalEffect](#resistenciaelementaleffect)
  - [TriggerCombateEffect](#triggercombateeffect)
- [GestorHabilidades](#gestorhabilidades)
  - [Eventos](#eventos)
  - [Agregar / Remover / Consultar](#agregar--remover--consultar)
  - [Uso de Habilidades](#uso-de-habilidades)
  - [Serialización](#serialización)
- [GestorPasivas](#gestorpasivas)
- [GestorCooldowns](#gestorcooldowns)
- [RegistroHabilidades](#registrohabilidades)
- [Interfaces](#interfaces)
- [Crear Habilidades en Unity](#crear-habilidades-en-unity)
- [⚠ TODOs en código](#-todos-en-código)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/SO/HabilidadData.cs](../Assets/Scripts/SO/HabilidadData.cs) | ScriptableObject de habilidad activa |
| [Assets/Scripts/SO/PasivaData.cs](../Assets/Scripts/SO/PasivaData.cs) | ScriptableObject de habilidad pasiva |
| [Assets/Scripts/Habilidades/GestorHabilidades.cs](../Assets/Scripts/Habilidades/GestorHabilidades.cs) | Gestor runtime de habilidades activas por entidad |
| [Assets/Scripts/Habilidades/GestorPasivas.cs](../Assets/Scripts/Habilidades/GestorPasivas.cs) | Gestor runtime de pasivas por entidad |
| [Assets/Scripts/Habilidades/GestorCooldowns.cs](../Assets/Scripts/Habilidades/GestorCooldowns.cs) | Gestión de cooldowns internos por entidad |
| [Assets/Scripts/Habilidades/CostoRecurso.cs](../Assets/Scripts/Habilidades/CostoRecurso.cs) | Modelo de costo de recurso para habilidades |
| [Assets/Scripts/Habilidades/RegistroHabilidades.cs](../Assets/Scripts/Habilidades/RegistroHabilidades.cs) | Singleton SO: catálogo global para save/load |
| [Assets/Scripts/Interfaces/Habilidades/IHabilidadesCommand.cs](../Assets/Scripts/Interfaces/Habilidades/IHabilidadesCommand.cs) | Interfaz de ejecución/validación de habilidad activa |
| [Assets/Scripts/Interfaces/Habilidades/IHabilidadEffect.cs](../Assets/Scripts/Interfaces/Habilidades/IHabilidadEffect.cs) | Interfaz base de efecto activo |
| [Assets/Scripts/Interfaces/Habilidades/IPasivaEffect.cs](../Assets/Scripts/Interfaces/Habilidades/IPasivaEffect.cs) | Interfaz base de efecto pasivo |
| [Assets/Scripts/Todohabilidades/DamageEffect.cs](../Assets/Scripts/Todohabilidades/DamageEffect.cs) | Efecto activo: aplica daño |
| [Assets/Scripts/Todohabilidades/HealEffect.cs](../Assets/Scripts/Todohabilidades/HealEffect.cs) | Efecto activo: restaura vida |
| [Assets/Scripts/Todohabilidades/StatusEffect.cs](../Assets/Scripts/Todohabilidades/StatusEffect.cs) | Efecto activo: aplica estado alterado |
| [Assets/Scripts/Todohabilidades/Pasivas/ModificadorStatEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/ModificadorStatEffect.cs) | Efecto pasivo: modifica estadística |
| [Assets/Scripts/Todohabilidades/Pasivas/RegeneracionEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/RegeneracionEffect.cs) | Efecto pasivo: regeneración por turno |
| [Assets/Scripts/Todohabilidades/Pasivas/ResistenciaElementalEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/ResistenciaElementalEffect.cs) | Efecto pasivo: resistencia/vulnerabilidad elemental |
| [Assets/Scripts/Todohabilidades/Pasivas/TriggerCombateEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/TriggerCombateEffect.cs) | Efecto pasivo: trigger al golpear/ser golpeado/matar/curar |
| [Assets/Scripts/Flags/TipoRecurso.cs](../Assets/Scripts/Flags/TipoRecurso.cs) | Enums: `TipoRecurso`, `CategoriaHabilidad` |

---

## Arquitectura del Sistema

El sistema de habilidades se divide en dos subsistemas paralelos:

```
─── ACTIVAS ─────────────────────────────────────────────────────────────
HabilidadData (ScriptableObject, IHabilidadesCommand)
    ├── costosRecursos[]  ──► CostoRecurso (TipoRecurso + cantidad)
    ├── tipoObjetivo      ──► TargetType
    ├── categoria         ──► CategoriaHabilidad
    ├── faccionesProhibidas[]
    └── efectos[]         ──► IHabilidadEffect
                                  ├── DamageEffect
                                  ├── HealEffect
                                  └── StatusEffect

GestorHabilidades  (por entidad, en runtime)
    └── GestorCooldowns  (interno)

─── PASIVAS ─────────────────────────────────────────────────────────────
PasivaData (ScriptableObject)
    ├── siempreActiva / condicion (CondicionPasiva)
    ├── faccionesProhibidas[]
    └── efectos[]         ──► IPasivaEffect
                                  ├── ModificadorStatEffect
                                  ├── RegeneracionEffect
                                  ├── ResistenciaElementalEffect
                                  └── TriggerCombateEffect

GestorPasivas  (por entidad, en runtime)

─── CATÁLOGO ────────────────────────────────────────────────────────────
RegistroHabilidades (ScriptableObject Singleton en Resources/)
    ├── todasLasHabilidades[]
    └── todasLasPasivas[]
```

La inicialización de ambos gestores ocurre en las subclases de `Jugador` y `Enemigos` a través de `InicializarDesdeClaseData(datos)` / `InicializarDesdeEnemigoData(datos)`. Ver [03_Clases_Jugador.md](03_Clases_Jugador.md) y [04_Enemigos.md](04_Enemigos.md).

---

## HabilidadData

**Archivo**: `Assets/Scripts/SO/HabilidadData.cs`
**Menú Unity**: `Create > Combate > Habilidad Data`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `nombreHabilidad` | `string` | Nombre para mostrar en UI |
| `icono` | `Sprite` | Icono de la habilidad |
| `descripcion` | `string` | Texto descriptivo |
| `categoria` | `CategoriaHabilidad` | Clasificación funcional (Ataque, Curación, Buff…) |
| `costosRecursos` | `List<CostoRecurso>` | Recursos consumidos al usar (puede ser vacío) |
| `cooldownTurnos` | `int` | Turnos de espera después de usar (0 = sin cooldown) |
| `faccionesProhibidas` | `List<TipoEntidades>` | Facciones que **no** pueden usar esta habilidad |
| `tipoObjetivo` | `TargetType` | A quién afecta la habilidad |
| `efectos` | `List<IHabilidadEffect>` | Efectos aplicados al ejecutar (`[SerializeReference]`) |

### CostoRecurso

**Archivo**: `Assets/Scripts/Habilidades/CostoRecurso.cs`

Representa un costo de recurso individual. Una habilidad puede tener múltiples costos (ej. 10 Mana + 5 Fe).

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `tipo` | `TipoRecurso` | Recurso a consumir |
| `cantidad` | `float` | Cantidad (o porcentaje si `usaPorcentaje = true`) |
| `usaPorcentaje` | `bool` | Si es `true`, `cantidad` es % del máximo del recurso |

```csharp
// Verificar viabilidad → consumir recursos (flujo estándar)
if (habilidad.VerificarCostosRecursos(invocador))
    habilidad.ConsumirRecursos(invocador);
```

### Enums Clave

**Archivo**: `Assets/Scripts/Flags/TipoRecurso.cs`

```csharp
public enum TipoRecurso
{
    Ninguno,        // Habilidades sin costo
    Mana,           // Recurso mágico clásico
    Energia,        // Recurso físico (guerreros, ladrones)
    Sangre,         // Recurso de sacrificio (habilidades oscuras)
    Fe,             // Recurso divino (paladines, clérigos)
    Furia,          // Se acumula con combate (berserkers)
    Concentracion,  // Se gasta al recibir daño
    Cargas          // Usos limitados que se recargan
}

public enum CategoriaHabilidad
{
    Ataque,     // Habilidades ofensivas
    Curacion,   // Restaurar vida/recursos
    Buff,       // Mejoras a aliados
    Debuff,     // Penalizaciones a enemigos
    Control,    // Stun, root, silence…
    Utilidad    // Movimiento, invocación…
}
```

**Tipos de Objetivo (TargetType)**

```csharp
public enum TargetType
{
    EnemigoUnico,   // Un enemigo específico
    EnemigoTodos,   // Todos los enemigos
    AliadoUnico,    // Un aliado específico
    AliadoTodos,    // Todos los aliados
    Self            // Solo el usuario
}
```

### Métodos Principales

#### `EsViable()`

Verifica si la habilidad puede usarse. Realiza las siguientes comprobaciones en orden:
1. La facción del invocador no está en `faccionesProhibidas`.
2. El invocador tiene recursos suficientes (`VerificarCostosRecursos`).
3. El invocador está vivo.
4. Hay un objetivo válido (si `tipoObjetivo != Self`).

Utiliza `IRecursoProvider` si está disponible; si no, hace fallback a `IJugadorProgresion.ManaActual_jugador` para compatibilidad.

#### `Ejecutar()`

```csharp
public void Ejecutar(
    IEntidadCombate invocador,
    IEntidadCombate objetivo,
    List<IEntidadCombate> aliados,
    List<IEntidadCombate> enemigos
)
```

Itera sobre `efectos` y llama `efecto.Aplicar()` para cada uno. Los efectos operan sobre `Entidad` (clase base), por lo que hace cast desde `IEntidadCombate`.

#### `ConsumirRecursos()` / `VerificarCostosRecursos()`

Gestionan el sistema de costos multi-recurso. `GestorHabilidades.UsarHabilidad()` se encarga de llamarlos en el orden correcto — no llamar manualmente.

---

## Efectos Activos (IHabilidadEffect)

**Archivo**: `Assets/Scripts/Interfaces/Habilidades/IHabilidadEffect.cs`
**Carpeta de implementaciones**: `Assets/Scripts/Todohabilidades/`

Todos los efectos activos son clases `[Serializable]` que implementan `IHabilidadEffect`:

```csharp
public interface IHabilidadEffect
{
    void Aplicar(Entidad invocador, Entidad objetivo,
                 List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos);
}
```

### DamageEffect

**Archivo**: `Assets/Scripts/Todohabilidades/DamageEffect.cs`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `baseDamage` | `int` | Daño base antes de stats |
| `tipoDano` | `ElementAttribute` | Elemento del daño (ver [08_Elementos.md](08_Elementos.md)) |
| `escaladoATK` | `float` | Multiplicador del ATK del invocador (`1 = +100% ATK`) |
| `ignoraDefensa` | `bool` | Si `true`, llama `RecibirDanoPuro()` en vez de `RecibirDano()` |
| `usaPorcentajeVidaObjetivo` | `bool` | Si `true`, `baseDamage` es % de la vida actual del objetivo |

Fórmula de daño:
```
danoFinal = baseDamage + (invocador.ATK × escaladoATK)
```
Si `usaPorcentajeVidaObjetivo = true`:
```
danoFinal = objetivo.VidaActual × (baseDamage / 100)
```

### HealEffect

**Archivo**: `Assets/Scripts/Todohabilidades/HealEffect.cs`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `curacionBase` | `int` | Cantidad fija a curar (o % si `usaPorcentajeVidaMax = true`) |
| `usaPorcentajeVidaMax` | `bool` | Si `true`, `curacionBase` es % de la vida máxima del objetivo |
| `escaladoConStat` | `float` | Multiplicador del ATK del invocador sumado a la curación |

### StatusEffect

**Archivo**: `Assets/Scripts/Todohabilidades/StatusEffect.cs`

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `statusAplicar` | `StatusFlag` | Estado a aplicar (ver [07_Estados.md](07_Estados.md)) |
| `duracionTurnos` | `int` | Duración en turnos |
| `danoPorTurno` | `int` | Daño periódico (para veneno, quemado, etc.) |
| `modificadorStats` | `float` | Modificador porcentual de stats (`0.2 = -20%`) |

Llama a `objetivo.AplicarEstado()`. El objetivo es determinado por `HabilidadData.tipoObjetivo`, no por este efecto.

---

## PasivaData

**Archivo**: `Assets/Scripts/SO/PasivaData.cs`
**Menú Unity**: `Create > Combate > Pasiva Data`

Las pasivas no requieren activación manual — están activas mientras la entidad las posea (o mientras se cumpla su `condicion`).

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `nombrePasiva` | `string` | Nombre para mostrar |
| `icono` | `Sprite` | Icono |
| `descripcion` | `string` | Texto descriptivo |
| `categoria` | `CategoriaPasiva` | Clasificación: `Estadisticas`, `Resistencias`, `Regeneracion`, `Triggers`, `Supervivencia`, `Ofensiva` |
| `siempreActiva` | `bool` | Si `false`, depende de `condicion` |
| `condicion` | `CondicionPasiva` | Cuándo se activa (sólo relevante si `siempreActiva = false`) |
| `valorCondicion` | `float` | Umbral numérico para la condición (ej. 50 para "HP < 50%") |
| `faccionesProhibidas` | `List<TipoEntidades>` | Facciones que no pueden tener esta pasiva |
| `efectos` | `List<IPasivaEffect>` | Efectos (`[SerializeReference]`) |

**CondicionPasiva disponibles:**

| Valor | Activación |
|-------|-----------|
| `Ninguna` | Siempre activa |
| `VidaMenorQue` | HP actual < `valorCondicion`% |
| `VidaMayorQue` | HP actual > `valorCondicion`% |
| `VidaIgualA` | HP actual ≈ `valorCondicion`% |
| `VidaLlena` | HP = 100% |
| `VidaCritica` | HP ≤ 25% |

### Ciclo de Vida

El SO delega el estado de activación al `GestorPasivas` — el SO mismo es stateless:

```
GestorPasivas.AgregarPasiva(pasiva)
    → pasiva.PuedeActivarse()    // chequea faccion + condicion
    → pasiva.Activar(portador)   // aplica todos los IPasivaEffect
    → EventBus: EventoPasivaDesbloqueada

GestorPasivas.ProcesarInicioTurno()   // llamar cada turno del portador
    → re-evalúa condiciones de pasivas condicionales
    → pasiva.ProcesarTurno(portador, estaActiva)

GestorPasivas.RemoverPasiva(pasiva)
    → pasiva.Desactivar(portador)    // revierte todos los IPasivaEffect
    → EventBus: EventoPasivaRemovida
```

---

## Efectos Pasivos (IPasivaEffect)

**Archivo**: `Assets/Scripts/Interfaces/Habilidades/IPasivaEffect.cs`
**Carpeta de implementaciones**: `Assets/Scripts/Todohabilidades/Pasivas/`

A diferencia de `IHabilidadEffect`, los efectos pasivos se **aplican** al obtener la pasiva y se **remueven** al perderla. También tienen un hook de `ProcesarTurno` para efectos periódicos.

```csharp
public interface IPasivaEffect
{
    void Aplicar(Entidad portador);        // Al activar la pasiva
    void Remover(Entidad portador);        // Al desactivar la pasiva
    void ProcesarTurno(Entidad portador);  // Cada turno (si está activa)
    string ObtenerDescripcion();
}
```

### ModificadorStatEffect

Modifica directamente un stat del portador. El valor original se guarda para revertirlo en `Remover()`.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `stat` | `TipoStat` | `Vida`, `Ataque`, `Defensa`, `Velocidad` |
| `tipo` | `TipoModificador` | `Plano` (+50 ATK) o `Porcentaje` (+20% ATK) |
| `valor` | `float` | Positivo = buff, negativo = debuff |

### RegeneracionEffect

Regenera HP o un recurso al inicio de cada turno del portador.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `tipo` | `TipoRegeneracion` | `Vida`, `Mana`, `Energia` |
| `cantidad` | `float` | Cantidad a regenerar por turno |
| `usaPorcentaje` | `bool` | Si `true`, `cantidad` es % del máximo |

Usa `IRecursoProvider` para Mana/Energía. El `Aplicar()` no hace nada visual.

### ResistenciaElementalEffect

Agrega resistencia o vulnerabilidad a un elemento. Su implementación real está pendiente de que `Entidad` tenga un sistema de resistencias indexado.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `elemento` | `ElementAttribute` | Elemento afectado (ver [08_Elementos.md](08_Elementos.md)) |
| `porcentajeResistencia` | `float` | Positivo = resistencia, negativo = vulnerabilidad |

### TriggerCombateEffect

Se activa cuando ocurre un evento de combate en el portador (golpear, ser golpeado, matar, curar). Su conexión a los eventos de `Entidad` está pendiente de implementación.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `trigger` | `TipoTrigger` | `AlGolpear`, `AlSerGolpeado`, `AlMatar`, `AlCurar` |
| `probabilidad` | `float` | Probabilidad de activación (0–100%) |
| `efectoTrigger` | `TipoEfectoTrigger` | `CurarVida`, `DanoAdicional`, `AplicarEstado`, `ReducirCooldown` |
| `valorEfecto` | `float` | Valor del efecto |
| `usaPorcentajeDelDano` | `bool` | Si el valor es % del daño/curación realizada |

---

## GestorHabilidades

**Archivo**: `Assets/Scripts/Habilidades/GestorHabilidades.cs`
**Namespace**: `Habilidades`

Gestiona las habilidades activas de una entidad en runtime. Cada instancia de `Jugador` o `Enemigos` tiene su propio `GestorHabilidades`. Encapsula internamente un `GestorCooldowns`.

### Constructores

```csharp
// Crear vacío con portador
new GestorHabilidades(portador, limite: 0);

// Crear con habilidades iniciales (desde ClaseData / EnemigoData)
new GestorHabilidades(portador, habilidadesIniciales, limite: 0);
```

`limite = 0` equivale a sin límite.

### Eventos

| Evento | Tipo | Descripción |
|--------|------|-------------|
| `OnHabilidadAgregada` | `Action<HabilidadData>` | Al aprender una habilidad |
| `OnHabilidadRemovida` | `Action<HabilidadData>` | Al perder una habilidad |
| `OnHabilidadesCambiadas` | `Action` | Cualquier cambio en la lista |
| `OnHabilidadUsada` | `Action<HabilidadData>` | Al ejecutar una habilidad |

`AgregarHabilidad()` y `RemoverHabilidad()` también publican al `EventBus` (`EventoHabilidadDesbloqueada` / `EventoHabilidadRemovida`). `UsarHabilidad()` publica `EventoHabilidadUsada`.

### Agregar / Remover / Consultar

```csharp
bool AgregarHabilidad(HabilidadData hab, bool notificar = true)
bool RemoverHabilidad(HabilidadData hab)
bool RemoverHabilidad(string nombre)
bool ReemplazarHabilidad(HabilidadData vieja, HabilidadData nueva)
void LimpiarHabilidades()

IReadOnlyList<HabilidadData> ObtenerTodas()
List<HabilidadData> ObtenerDisponibles(objetivo, aliados, enemigos)  // sin cooldown + viable
List<HabilidadData> ObtenerPorCategoria(CategoriaHabilidad cat)
bool TieneHabilidad(HabilidadData hab)
bool TieneHabilidad(string nombre)
HabilidadData ObtenerPorNombre(string nombre)
HabilidadData ObtenerPorIndice(int i)

int Cantidad            // total de habilidades
int Limite              // límite configurado (0 = sin límite)
int EspaciosDisponibles // Limite - Cantidad (int.MaxValue si sin límite)
```

### Uso de Habilidades

```csharp
// Flujo estándar de un turno:
bool PuedeUsar(hab, objetivo, aliados, enemigos)  // cooldown + EsViable
bool UsarHabilidad(hab, objetivo, aliados, enemigos)
    // 1. ConsumirRecursos
    // 2. IniciarCooldown
    // 3. hab.Ejecutar(...)
    // 4. OnHabilidadUsada + EventBus
```

```csharp
int ObtenerCooldown(HabilidadData hab)
bool EstaEnCooldown(HabilidadData hab)
void ProcesarInicioTurno()   // reducir todos los cooldowns en 1
void ResetearCooldowns()
GestorCooldowns Cooldowns    // acceso directo para UI
```

### Serialización

```csharp
List<string> ObtenerNombresParaGuardar()
void CargarDesdeNombres(List<string> nombres, Func<string, HabilidadData> buscarHabilidad)
// buscarHabilidad → use RegistroHabilidades.Instancia.BuscarHabilidad
```

---

## GestorPasivas

**Archivo**: `Assets/Scripts/Habilidades/GestorPasivas.cs`
**Namespace**: `Habilidades`

Gestiona el ciclo de vida de las pasivas de una entidad. Mantiene un `HashSet<PasivaData>` interno de pasivas actualmente activas para resolver el estado mutable sin modificar el SO.

| Método | Descripción |
|--------|-------------|
| `AgregarPasiva(pasiva)` | Agrega, activa y notifica. Verifica `PuedeActivarse()` antes |
| `RemoverPasiva(pasiva)` | Desactiva, remueve y notifica |
| `ProcesarInicioTurno()` | Re-evalúa condiciones de pasivas condicionales; llama `ProcesarTurno()` en todas |
| `ActualizarEstados()` | Re-evalúa condiciones sin procesar turno (llamar al cambiar HP, etc.) |
| `ActivarTodas()` | Activar al inicio del combate |
| `DesactivarTodas()` | Desactivar al fin del combate si es necesario |
| `EstaPasivaActiva(pasiva)` | Consulta si una pasiva está activa en este portador |
| `TienePasiva(pasiva)` | Consulta si el portador posee la pasiva (activa o no) |

Eventos: `OnPasivaAgregada`, `OnPasivaRemovida`. Al agregar/remover también publica al `EventBus`.

---

## GestorCooldowns

**Archivo**: `Assets/Scripts/Habilidades/GestorCooldowns.cs`
**Namespace**: `Habilidades`

Vive **dentro** de `GestorHabilidades` (campo privado). No reside en `EntityController`.

Internamente usa un `Dictionary<string, int>` keyed por `nombreHabilidad`.

| Método | Descripción |
|--------|-------------|
| `EstaDisponible(hab)` / `EstaDisponible(nombre)` | `true` si cooldown = 0 |
| `ObtenerCooldown(hab)` | Turnos restantes |
| `IniciarCooldown(hab)` | Registra `cooldownTurnos` al ser usada |
| `ProcesarInicioTurno()` | Decrementa todos en 1; dispara `OnHabilidadDisponible` en los que llegan a 0 |
| `ResetearCooldown(nombre)` | Fuerza cooldown a 0 |
| `ResetearTodos()` | Resetea todos los cooldowns |
| `ReducirCooldown(nombre, cantidad)` | Reduce en `cantidad` (mínimo 0) |
| `ObtenerTodosLosCooldowns()` | Devuelve copia del diccionario para UI/debug |

Evento: `OnHabilidadDisponible(string nombreHabilidad)`.

---

## RegistroHabilidades

**Archivo**: `Assets/Scripts/Habilidades/RegistroHabilidades.cs`
**Menú Unity**: `Create > Combate > Registro de Habilidades`

Singleton `ScriptableObject` cargado desde `Resources/RegistroHabilidades`. Catálogo global de todas las habilidades y pasivas del juego — necesario para save/load por nombre.

```csharp
// Acceso
RegistroHabilidades.Instancia.BuscarHabilidad("Bola de Fuego");
RegistroHabilidades.Instancia.BuscarPasiva("Escudo de Fe");
RegistroHabilidades.Instancia.ObtenerPorCategoria(CategoriaHabilidad.Curacion);
RegistroHabilidades.Instancia.ObtenerPasivasPorCategoria(CategoriaPasiva.Regeneracion);
```

Construye diccionarios internos en `OnEnable()` para búsquedas O(1). Loguea advertencia si hay nombres duplicados.

**Setup requerido**: crear el asset, colocarlo en `Assets/Resources/RegistroHabilidades`, y registrar todas las habilidades/pasivas del juego en las listas del inspector.

---

## Interfaces

### IHabilidadesCommand

**Archivo**: `Assets/Scripts/Interfaces/Habilidades/IHabilidadesCommand.cs`

```csharp
public interface IHabilidadesCommand
{
    bool EsViable(IEntidadCombate invocador, IEntidadCombate objetivo,
                  List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos);

    void Ejecutar(IEntidadCombate invocador, IEntidadCombate objetivo,
                  List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos);

    HabilidadData ObtenerDatos();
}
```

`HabilidadData` implementa esta interfaz directamente — es a la vez datos y comando.

### IHabilidadEffect

```csharp
void Aplicar(Entidad invocador, Entidad objetivo,
             List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos);
```

### IPasivaEffect

```csharp
void Aplicar(Entidad portador);
void Remover(Entidad portador);
void ProcesarTurno(Entidad portador);
string ObtenerDescripcion();
```

---

## Crear Habilidades en Unity

### Habilidad de Ataque Básico

1. **Click derecho** en el Project > `Create > Combate > Habilidad Data`
2. **Configurar**:
   ```
   Nombre Habilidad: "Ataque"
   Descripcion:      "Un golpe físico básico"
   Categoria:        Ataque
   Costos Recursos:  (vacío — sin costo)
   Cooldown Turnos:  0
   Tipo Objetivo:    EnemigoUnico
   ```
3. **Añadir efecto** (click `+` en la lista Efectos):
   ```
   Tipo: DamageEffect
     baseDamage: 0
     tipoDano:   None
     escaladoATK: 1
   ```

### Habilidad de Fuego

```
Nombre Habilidad: "Bola de Fuego"
Categoria:        Ataque
Costos Recursos:
  [0] tipo: Mana | cantidad: 15
Cooldown Turnos:  2
Tipo Objetivo:    EnemigoUnico

Efectos:
  [0] DamageEffect
      baseDamage:  25
      tipoDano:    Fire
      escaladoATK: 0.5
```

### Habilidad en Área

```
Nombre Habilidad: "Terremoto"
Categoria:        Ataque
Costos Recursos:
  [0] tipo: Mana | cantidad: 30
Cooldown Turnos:  4
Tipo Objetivo:    EnemigoTodos   ← clave para área

Efectos:
  [0] DamageEffect
      baseDamage:  15
      tipoDano:    Earth
      escaladoATK: 0.3
```

### Habilidad de Curación

```
Nombre Habilidad: "Curar"
Categoria:        Curacion
Costos Recursos:
  [0] tipo: Mana | cantidad: 12
Cooldown Turnos:  2
Tipo Objetivo:    AliadoUnico

Efectos:
  [0] HealEffect
      curacionBase:        40
      usaPorcentajeVidaMax: false
      escaladoConStat:      0
```

### Habilidad con Múltiples Efectos

```
Nombre Habilidad: "Golpe Venenoso"
Categoria:        Debuff
Costos Recursos:
  [0] tipo: Mana | cantidad: 10
Cooldown Turnos:  3
Tipo Objetivo:    EnemigoUnico

Efectos:
  [0] DamageEffect
      baseDamage: 10
      tipoDano:   None
  [1] StatusEffect
      statusAplicar:    Poisoned
      duracionTurnos:   3
      danoPorTurno:     5
      modificadorStats: 0
```

### Pasiva de Regeneración

1. **Click derecho** > `Create > Combate > Pasiva Data`
2. **Configurar**:
   ```
   Nombre Pasiva:   "Regeneración Natural"
   Categoria:       Regeneracion
   Siempre Activa:  true
   ```
3. **Añadir efecto**:
   ```
   Tipo: RegeneracionEffect
     tipo:          Vida
     cantidad:      5
     usaPorcentaje: false
   ```

### Pasiva Condicional (HP Crítico)

```
Nombre Pasiva:   "Furia de Batalla"
Categoria:       Ofensiva
Siempre Activa:  false
Condicion:       VidaCritica      ← activa cuando HP ≤ 25%
Valor Condicion: 25

Efectos:
  [0] ModificadorStatEffect
      stat:  Ataque
      tipo:  Porcentaje
      valor: 50          ← +50% ATK cuando HP crítico
```

---

## ⚠ TODOs en código

| Archivo | Descripción |
|---------|-------------|
| [HabilidadData.cs](../Assets/Scripts/SO/HabilidadData.cs) — línea 160 | Notificación visual pendiente: `// invocador.NotificarHabilidadEjecutada(this, objetivo);` — requiere hookear el sistema visual después de ejecutar la lógica |
| [TriggerCombateEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/TriggerCombateEffect.cs) — línea 51 | Suscripción a eventos de `Entidad` comentada: `// portador.OnDanoRealizado += ...` — funcionalidad completa bloqueada hasta que `Entidad` exponga los eventos de combate correspondientes |
| [ResistenciaElementalEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/ResistenciaElementalEffect.cs) — línea 28 | Modificación de resistencias comentada: `// portador.ModificarResistencia(elemento, porcentajeResistencia)` — requiere sistema de resistencias en `Entidad` |
| [RegeneracionEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/RegeneracionEffect.cs) | `TipoRegeneracion` sólo cubre `Vida`, `Mana`, `Energia` — pendiente ampliar para cubrir todos los valores de `TipoRecurso` |
| [PaladinModulo.cs](../Assets/Scripts/Subclases/Modulos/PaladinModulo.cs) — línea 68 | `OverridearRecursoPrincipal()` retorna `null` — pendiente asignar `TipoRecurso.Fe` cuando el sistema de Fe esté implementado |
```

---

## Interfaz IHabilidadesCommand

```csharp
public interface IHabilidadesCommand
{
    void Ejecutar(
        Entidad invocador, 
        Entidad objetivoPrincipal, 
        List<IEntidadCombate> aliados, 
        List<IEntidadCombate> enemigos
    );
    
    bool EsViable(
        IEntidadCombate invocador, 
        IEntidadCombate objetivo, 
        List<IEntidadCombate> aliados, 
        List<IEntidadCombate> enemigos
    );
}
```

---

## Flujo de Ejecución

```
┌─────────────────────────────────────────────────────┐
│              CombateManager                          │
│  EjecutarTurno(entidad)                             │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│         IEntidadActuable                            │
│  ObtenerAccionElegida(aliados, enemigos)            │
│  → Retorna (HabilidadData, objetivo)                │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│         HabilidadData.EsViable()                    │
│  - Verificar objetivo vivo                          │
│  - Verificar mana suficiente                        │
│  - Verificar cooldown (via GestorCooldowns)         │
└─────────────────┬───────────────────────────────────┘
                  │ Si es viable
                  ▼
┌─────────────────────────────────────────────────────┐
│         Consumir Recursos                           │
│  - Jugador.ConsumirMana(costeMana)                  │
│  - GestorCooldowns.IniciarCooldown(habilidad)       │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│         HabilidadData.Ejecutar()                    │
│  - ObtenerObjetivos() según tipoObjetivo            │
│  - Para cada objetivo:                              │
│      - Para cada efecto:                            │
│          - efecto.Aplicar(inv, obj, ali, ene)       │
└─────────────────────────────────────────────────────┘
```

---

## Tabla de Habilidades Sugeridas

| Nombre | Mana | CD | Objetivo | Efectos |
|--------|------|----|---------| --------|
| Ataque | 0 | 0 | EnemigoUnico | Damage(0) |
| Golpe Fuerte | 5 | 1 | EnemigoUnico | Damage(15) |
| Bola de Fuego | 15 | 2 | EnemigoUnico | Damage(25, Fire) |
| Terremoto | 30 | 4 | EnemigoTodos | Damage(15, Earth) |
| Curar | 12 | 2 | AliadoUnico | Heal(40) |
| Curar Grupo | 25 | 4 | AliadoTodos | Heal(25) |
| Veneno | 8 | 3 | EnemigoUnico | Status(Poison, 3t, 5dmg) |
| Aturdimiento | 15 | 4 | EnemigoUnico | Status(Stunned, 1t) |
| Bendición | 20 | 5 | AliadoUnico | Status(Buffed, 3t, +20%) |
| Furia | 10 | 3 | Self | Status(Buffed, 2t, +30%) |

# Sistema de Efectos

> Documentación de los efectos de habilidades: implementaciones activas y pasivas.
> Para detalles de estados alterados ver [07_Estados.md](07_Estados.md).
> Para detalles del sistema elemental ver [08_Elementos.md](08_Elementos.md).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Visión General](#visión-general)
- [Interfaz IHabilidadEffect](#interfaz-ihabilidadeffect)
- [Interfaz IPasivaEffect](#interfaz-ipasivaeffect)
- [DamageEffect](#damageeffect)
- [HealEffect](#healeffect)
- [StatusEffect](#statuseffect)
- [Efectos Pasivos (IPasivaEffect)](#efectos-pasivos-ipasivaeffect)
  - [ModificadorStatEffect](#modificadorstateffect)
  - [TriggerCombateEffect](#triggercombateeffect)
  - [ResistenciaElementalEffect](#resistenciaelementaleffect)
  - [RegeneracionEffect](#regeneracioneffect)
- [Combinaciones de Efectos](#combinaciones-de-efectos)
- [Crear un Nuevo Efecto Activo](#crear-un-nuevo-efecto-activo)
- [Cómo Crear Pasivas con Efectos en Unity](#cómo-crear-pasivas-con-efectos-en-unity)
- [⚠ TODOs en código](#-todos-en-código)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Interfaces/Habilidades/IHabilidadEffect.cs](../Assets/Scripts/Interfaces/Habilidades/IHabilidadEffect.cs) | Interfaz de efectos activos |
| [Assets/Scripts/Interfaces/Habilidades/IPasivaEffect.cs](../Assets/Scripts/Interfaces/Habilidades/IPasivaEffect.cs) | Interfaz de efectos pasivos |
| [Assets/Scripts/Todohabilidades/DamageEffect.cs](../Assets/Scripts/Todohabilidades/DamageEffect.cs) | Efecto que aplica daño |
| [Assets/Scripts/Todohabilidades/HealEffect.cs](../Assets/Scripts/Todohabilidades/HealEffect.cs) | Efecto que cura vida |
| [Assets/Scripts/Todohabilidades/StatusEffect.cs](../Assets/Scripts/Todohabilidades/StatusEffect.cs) | Efecto que aplica estados |
| [Assets/Scripts/Todohabilidades/Pasivas/ModificadorStatEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/ModificadorStatEffect.cs) | Efecto pasivo que modifica stats base |
| [Assets/Scripts/Todohabilidades/Pasivas/TriggerCombateEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/TriggerCombateEffect.cs) | Efecto pasivo con triggers de combate |
| [Assets/Scripts/Todohabilidades/Pasivas/ResistenciaElementalEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/ResistenciaElementalEffect.cs) | Efecto pasivo de resistencia elemental |
| [Assets/Scripts/Todohabilidades/Pasivas/RegeneracionEffect.cs](../Assets/Scripts/Todohabilidades/Pasivas/RegeneracionEffect.cs) | Efecto pasivo de regeneración por turno |

---

## Visión General

Los efectos se dividen en dos categorías:

- **Efectos activos** (`IHabilidadEffect`): se ejecutan al usar una habilidad
- **Efectos pasivos** (`IPasivaEffect`): se aplican/remueven mientras la entidad posea la pasiva

```
IHabilidadEffect (Habilidades Activas)
    ├── DamageEffect            → Inflige daño (con o sin ignorar defensa)
    ├── HealEffect              → Cura vida, con escalado opcional
    └── StatusEffect            → Aplica estados alterados

IPasivaEffect (Pasivas)
    ├── ModificadorStatEffect   → Modifica stats base (vida, ataque, defensa, velocidad)
    ├── TriggerCombateEffect    → Trigger al golpear/ser golpeado/matar/curar
    ├── ResistenciaElementalEffect → Otorga resistencia o vulnerabilidad elemental
    └── RegeneracionEffect      → Regenera HP o recurso cada turno
```

---

## Interfaz IHabilidadEffect

**Archivo**: `Assets/Scripts/Interfaces/Habilidades/IHabilidadEffect.cs`

```csharp
public interface IHabilidadEffect
{
    void Aplicar(
        Entidad invocador,           // Quien usa la habilidad
        Entidad objetivo,            // A quien afecta
        List<IEntidadCombate> aliados,
        List<IEntidadCombate> enemigos
    );
}
```

---

## Interfaz IPasivaEffect

**Archivo**: `Assets/Scripts/Interfaces/Habilidades/IPasivaEffect.cs`

```csharp
public interface IPasivaEffect
{
    void Aplicar(Entidad portador);       // Al activar la pasiva
    void Remover(Entidad portador);       // Al desactivar la pasiva
    void ProcesarTurno(Entidad portador); // Cada turno (si aplica)
    string ObtenerDescripcion();          // Texto para UI
}
```

---

## DamageEffect

**Archivo**: `Assets/Scripts/Todohabilidades/DamageEffect.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `baseDamage` | int | Daño base de la habilidad (antes de stats) |
| `tipoDano` | ElementAttribute | Tipo elemental (None = físico puro) — ver [08_Elementos.md](08_Elementos.md) |
| `escaladoATK` | float (0-3) | Escala con ATK del invocador (1 = +100% ATK) |
| `ignoraDefensa` | bool | Si true, llama `RecibirDanoPuro` (bypassa defensa y resistencias) |
| `usaPorcentajeVidaObjetivo` | bool | Si true, baseDamage es % de HP actual del objetivo |

### Flujo de ejecución

```csharp
public void Aplicar(Entidad invocador, Entidad objetivo, ...)
{
    if (objetivo == null || !objetivo.EstaVivo()) return;

    // 1. Daño base
    float danoCalculado = baseDamage;
    if (usaPorcentajeVidaObjetivo)
        danoCalculado = objetivo.VidaActual_Entidad * (baseDamage / 100f);

    // 2. Escalado con ATK del invocador
    if (escaladoATK > 0)
        danoCalculado += invocador.PuntosDeAtaque_Entidad * escaladoATK;

    // 3. Aplicar daño (la Entidad calcula mitigación internamente)
    if (ignoraDefensa)
        objetivo.RecibirDanoPuro((int)danoCalculado, tipoDano);
    else
        objetivo.RecibirDano((int)danoCalculado, tipoDano);
}
```

### Configuración en Inspector

```
[DamageEffect]
├── Base Damage: 25             ← Daño fijo de la habilidad
├── Tipo Dano: Fire             ← Elemento (None = físico)
├── Escalado ATK: 1.0           ← 100% del ATK se suma
├── Ignora Defensa: ☐           ← Bypassa defensa/resistencias
└── Usa Porcentaje Vida: ☐      ← baseDamage como % HP
```

### Ejemplos de Uso

| Habilidad | baseDamage | tipoDano | escaladoATK | Resultado |
|-----------|------------|----------|-------------|-----------|
| Ataque Básico | 0 | None | 1.0 | Solo ATK del invocador |
| Golpe Fuerte | 15 | None | 1.0 | ATK + 15 (defensa aplica) |
| Bola de Fuego | 25 | Fire | 1.0 | ATK + 25 de tipo Fuego |
| Ejecución | 20 | None | 1.5 | ATK×1.5 + 20, ignora defensa |
| Drenar Vida | 15 | Dark | 0 | Solo 15, sin escalado ATK |
| % HP Golpe | 10 | None | 0 | 10% de la HP actual del objetivo |

---

## HealEffect

**Archivo**: `Assets/Scripts/Todohabilidades/HealEffect.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `curacionBase` | int | Cantidad base de curación |
| `usaPorcentajeVidaMax` | bool | Si true, curacionBase es % de vida máxima del objetivo |
| `escaladoConStat` | float (0-2) | Escala con ATK/poder del invocador (0 = sin escalado) |

### Lógica de Curación

```csharp
public void Aplicar(Entidad invocador, Entidad objetivo, ...)
{
    if (objetivo == null || !objetivo.EstaVivo()) return;

    float curacionTotal = curacionBase;

    if (usaPorcentajeVidaMax)
        curacionTotal = objetivo.Vida_Entidad * (curacionBase / 100f);

    if (escaladoConStat > 0)
        curacionTotal += invocador.PuntosDeAtaque_Entidad * escaladoConStat;

    int vidaCurada = objetivo.Curar((int)curacionTotal);
}
```

### Configuración en Inspector

```
[HealEffect]
├── Curacion Base: 40          ← Curación fija
├── Usa Porcentaje Vida Max: ☐ ← Desactivado = fijo
└── Escalado Con Stat: 0       ← Sin escalado de invocador
```

Para curación porcentual con escalado:
```
[HealEffect]
├── Curacion Base: 25          ← 25% de vida máxima
├── Usa Porcentaje Vida Max: ☑ ← Activado = porcentaje
└── Escalado Con Stat: 0.5     ← +50% ATK del invocador
```

### Ejemplos de Uso

| Habilidad | curacionBase | usaPorcentajeVidaMax | Resultado |
|-----------|--------------|----------------------|-----------|
| Curar Menor | 30 | false | +30 HP |
| Curar Mayor | 80 | false | +80 HP |
| Curación % | 25 | true | +25% HP max |
| Curación Completa | 100 | true | HP completo |

---

## StatusEffect

**Archivo**: `Assets/Scripts/Todohabilidades/StatusEffect.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `statusAplicar` | StatusFlag | Estado a aplicar |
| `duracionTurnos` | int | Duración en turnos |
| `danoPorTurno` | int | Daño por turno (veneno, quemado) |
| `modificadorStats` | float | Modificador de stats (0.2 = -20%) |

> Para la definición completa de `StatusFlag` y el comportamiento de cada estado ver [07_Estados.md](07_Estados.md).

### Lógica de Aplicación

```csharp
public void Aplicar(Entidad invocador, Entidad objetivo, ...)
{
    if (objetivo == null || !objetivo.EstaVivo()) return;

    objetivo.AplicarEstado(
        statusAplicar, 
        duracionTurnos, 
        danoPorTurno, 
        modificadorStats
    );
}
```

### Configuración en Inspector

**Veneno**:
```
[StatusEffect]
├── Status Aplicar: Poisoned
├── Duracion Turnos: 3
├── Dano Por Turno: 5        ← 5 daño cada turno
└── Modificador Stats: 0     ← Sin efecto en stats
```

**Aturdimiento**:
```
[StatusEffect]
├── Status Aplicar: Stunned
├── Duracion Turnos: 1       ← Solo 1 turno
├── Dano Por Turno: 0        ← Sin daño
└── Modificador Stats: 0
```

**Buff de Ataque**:
```
[StatusEffect]
├── Status Aplicar: Buffed
├── Duracion Turnos: 3
├── Dano Por Turno: 0
└── Modificador Stats: 0.3   ← +30% stats
```

**Debuff (ralentización)**:
```
[StatusEffect]
├── Status Aplicar: Debuffed
├── Duracion Turnos: 2
├── Dano Por Turno: 0
└── Modificador Stats: 0.2   ← -20% stats
```

---

## Crear un Nuevo Efecto Activo

### Ejemplo: LifeStealEffect

```csharp
// Assets/Scripts/Todohabilidades/LifeStealEffect.cs
using UnityEngine;
using System.Collections.Generic;
using Interfaces;
using Padres;
using Flags;
using Habilidades;

[System.Serializable]
public class LifeStealEffect : IHabilidadEffect
{
    [Tooltip("Daño base del ataque")]
    public int baseDamage = 10;

    [Tooltip("Porcentaje de daño que se cura (0.3 = 30%)")]
    [Range(0f, 1f)]
    public float porcentajeRobo = 0.3f;

    [Tooltip("Tipo de daño")]
    public ElementAttribute tipoDano = ElementAttribute.Dark;

    public void Aplicar(
        Entidad invocador,
        Entidad objetivo,
        List<IEntidadCombate> aliados,
        List<IEntidadCombate> enemigos
    )
    {
        if (objetivo == null || !objetivo.EstaVivo()) return;

        // Calcular daño
        float dano = baseDamage + invocador.PuntosDeAtaque_Entidad;
        objetivo.RecibirDano((int)dano, tipoDano);

        // Curar al invocador (% del daño aplicado)
        int curacion = Mathf.RoundToInt(dano * porcentajeRobo);
        if (curacion > 0)
            invocador.Curar(curacion);
    }
}
```

### Ejemplo: ShieldEffect

```csharp
// Assets/Scripts/Todohabilidades/ShieldEffect.cs
[System.Serializable]
public class ShieldEffect : IHabilidadEffect
{
    [Tooltip("Cantidad de escudo temporal")]
    public int cantidadEscudo = 50;
    
    [Tooltip("Duración en turnos")]
    public int duracion = 3;

    public void Aplicar(
        Entidad invocador, 
        Entidad objetivo, 
        List<IEntidadCombate> aliados, 
        List<IEntidadCombate> enemigos
    )
    {
        if (objetivo == null || !objetivo.EstaVivo()) return;

        // Aquí implementarías la lógica de escudo
        // Por ejemplo, añadir vida temporal o aumentar defensa
        
        Debug.Log(objetivo.Nombre_Entidad + " obtiene un escudo de " + cantidadEscudo + "!");
        
        // Una opción simple: aumentar defensa temporalmente
        objetivo.AplicarEstado(StatusFlag.Buffed, duracion, 0, 0.5f);
    }
}
```

---

## Combinaciones de Efectos

### Habilidad Multi-efecto: "Explosión Venenosa"

```
Efectos:
  [0] DamageEffect
      - baseDamage: 15
      - tipoDano: None
      
  [1] StatusEffect
      - statusAplicar: Poisoned
      - duracionTurnos: 3
      - danoPorTurno: 8
      - modificadorStats: 0
```

### Habilidad de Área + Debuff: "Tormenta de Hielo"

```
Tipo Objetivo: EnemigoTodos  ← Afecta a todos

Efectos:
  [0] DamageEffect
      - baseDamage: 12
      - tipoDano: Water
      
  [1] StatusEffect
      - statusAplicar: Frozen
      - duracionTurnos: 2
      - danoPorTurno: 0
      - modificadorStats: 0.3  ← -30% velocidad
```

### Habilidad Curativa + Buff: "Inspiración"

```
Tipo Objetivo: AliadoTodos

Efectos:
  [0] HealEffect
      - curacionBase: 20
      - usaPorcentajeVidaMax: false
      
  [1] StatusEffect
      - statusAplicar: Buffed
      - duracionTurnos: 2
      - danoPorTurno: 0
      - modificadorStats: 0.15  ← +15% stats
```

---

## Efectos Pasivos (IPasivaEffect)

Los efectos pasivos se aplican/remueven automáticamente cuando una `PasivaData` se activa/desactiva en la entidad. Se configuran como ScriptableObjects en Unity.

### ModificadorStatEffect

**Archivo**: `Assets/Scripts/Todohabilidades/Pasivas/ModificadorStatEffect.cs`

Modifica una stat base del portador al activarse la pasiva, y revierte el cambio al removerla.

#### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `stat` | TipoStat | Vida, Ataque, Defensa, o Velocidad |
| `tipo` | TipoModificador | Plano (+50 ATK) o Porcentaje (+20% ATK) |
| `valor` | float | Magnitud (negativo = debuff) |

#### Configuración de Ejemplo

**Guerrero (+20% Ataque)**:
```
[ModificadorStatEffect]
├── Stat: Ataque
├── Tipo: Porcentaje
└── Valor: 20                    ← +20% ATK
```

**Defensa plana (+50 HP)**:
```
[ModificadorStatEffect]
├── Stat: Vida
├── Tipo: Plano
└── Valor: 50                    ← +50 HP máx
```

**Debuff de velocidad (-15%)**:
```
[ModificadorStatEffect]
├── Stat: Velocidad
├── Tipo: Porcentaje
└── Valor: -15                   ← -15% velocidad
```

---

### TriggerCombateEffect

**Archivo**: `Assets/Scripts/Todohabilidades/Pasivas/TriggerCombateEffect.cs`

Efecto pasivo que se activa en respuesta a eventos de combate. Llama a `EjecutarTrigger(portador, otro, valorBase)` desde el sistema de combate.

#### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `trigger` | TipoTrigger | AlGolpear, AlSerGolpeado, AlMatar, AlCurar |
| `probabilidad` | float (0-100) | Probabilidad de activarse |
| `efectoTrigger` | TipoEfectoTrigger | CurarVida, DanoAdicional, AplicarEstado, ReducirCooldown |
| `valorEfecto` | float | Magnitud del efecto |
| `usaPorcentajeDelDano` | bool | Si true, valorEfecto es % del valor base del evento |

#### Configuración de Ejemplo

**Vampirismo (robar HP al golpear, 20% chance)**:
```
[TriggerCombateEffect]
├── Trigger: AlGolpear
├── Probabilidad: 20
├── Efecto Trigger: CurarVida
├── Valor Efecto: 30             ← 30% del daño realizado
└── Usa Porcentaje Del Dano: ☑
```

**Contra-ataque (daño al ser golpeado)**:
```
[TriggerCombateEffect]
├── Trigger: AlSerGolpeado
├── Probabilidad: 15
├── Efecto Trigger: DanoAdicional
├── Valor Efecto: 10             ← 10 daño puro al atacante
└── Usa Porcentaje Del Dano: ☐
```

---

### ResistenciaElementalEffect

**Archivo**: `Assets/Scripts/Todohabilidades/Pasivas/ResistenciaElementalEffect.cs`

Otorga resistencia o vulnerabilidad a un elemento específico. La implementación real está pendiente de que `Entidad` exponga un sistema de modificación de resistencias.

> Para los elementos disponibles ver [08_Elementos.md](08_Elementos.md).

#### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `elemento` | ElementAttribute | Elemento afectado |
| `porcentajeResistencia` | float (-100 a 100) | Positivo = resistencia, negativo = vulnerabilidad |

#### Configuración de Ejemplo

**Resistencia al Fuego (+25%)**:
```
[ResistenciaElementalEffect]
├── Elemento: Fire
└── Porcentaje Resistencia: 25
```

**Vulnerabilidad al Rayo (-15%)**:
```
[ResistenciaElementalEffect]
├── Elemento: Thunder
└── Porcentaje Resistencia: -15
```

---

### RegeneracionEffect

**Archivo**: `Assets/Scripts/Todohabilidades/Pasivas/RegeneracionEffect.cs`

Regeneracón por turno de HP, Mana o Energía. Usa `IRecursoProvider` si el portador lo implementa (para Mana/Energía).

#### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `tipo` | TipoRegeneracion | Vida, Mana, o Energia |
| `cantidad` | float | Cantidad a regenerar por turno |
| `usaPorcentaje` | bool | Si true, la cantidad es % del máximo |

#### Configuración de Ejemplo

**Regeneración de Vida (5 HP/turno)**:
```
[RegeneracionEffect]
├── Tipo: Vida
├── Cantidad: 5
└── Usa Porcentaje: ☐
```

**Regeneración de Mana (10% máx/turno)**:
```
[RegeneracionEffect]
├── Tipo: Mana
├── Cantidad: 10
└── Usa Porcentaje: ☑
```

---

## Cómo Crear Pasivas con Efectos en Unity

1. **Assets → Create → Combate/Pasiva Data**
2. Configurar nombre, icono, descripción, categoría
3. En la lista de **efectos**, click en **+** y elegir el tipo:
   - `ModificadorStatEffect` para buff/debuff de stats base
   - `TriggerCombateEffect` para reacciones a eventos de combate
   - `ResistenciaElementalEffect` para resistencias/vulnerabilidades
   - `RegeneracionEffect` para regeneración de HP o recurso
4. Configurar el efecto según las tablas anteriores
5. **Asignar la pasiva** al `ClaseData.pasivasIniciales` o `EnemigoData.pasivas`
6. Al crearse la entidad, las pasivas se activan automáticamente

---

## ⚠ TODOs en código

| Archivo | TODO |
|---------|------|
| `TriggerCombateEffect.cs` | Conectar a eventos de `Entidad` en `Aplicar()`/`Remover()`. Suscribir `OnDanoRealizado` y `OnDañoRecibido` cuando la entidad exponga esos eventos. |
| `ResistenciaElementalEffect.cs` | Implementar `portador.ModificarResistencia(elemento, porcentaje)` cuando `Entidad` tenga sistema de resistencias. Actualmente sólo loggea. |

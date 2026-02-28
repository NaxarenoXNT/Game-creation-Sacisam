# Sistema de Efectos

## Visión General

Los efectos se dividen en dos categorías:

- **Efectos activos** (`IHabilidadEffect`): se ejecutan al usar una habilidad
- **Efectos pasivos** (`IPasivaEffect`): se aplican/remueven mientras la entidad posea la pasiva

```
IHabilidadEffect (Habilidades Activas)
    ├── DamageEffect   → Inflige daño vía DamagePipeline
    ├── HealEffect     → Cura vida
    └── StatusEffect   → Aplica estados

IPasivaEffect (Pasivas)
    ├── DamageModifierPasivaEffect → Registra IDamageModifier en la entidad
    ├── TriggerCombateEffect       → Trigger al golpear/ser golpeado/matar
    └── StatModifierEffect         → Modifica stats base (custom)
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

## Interfaz IPasivaEffect

**Archivo**: `Assets/Scripts/Interfaces/Habilidades/IPasivaEffect.cs`

```csharp
public interface IPasivaEffect
{
    void Aplicar(Entidad portador);      // Al activar la pasiva
    void Remover(Entidad portador);      // Al desactivar la pasiva
    void ProcesarTurno(Entidad portador); // Cada turno (si aplica)
    string ObtenerDescripcion();          // Texto para UI
}
```

---

## DamageEffect (usa DamagePipeline)

**Archivo**: `Assets/Scripts/Todohabilidades/DamageEffect.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `baseDamage` | int | Daño base de la habilidad (antes de stats) |
| `tipoDano` | ElementAttribute | Tipo elemental (None = físico puro) |
| `escaladoATK` | float (0-3) | Escala con ATK del invocador (1 = +100% ATK) |
| `ignoraDefensa` | bool | Si true, ignora defensa y resistencias |
| `usaPorcentajeVidaObjetivo` | bool | Si true, baseDamage es % de HP actual |

### Flujo de ejecución

```csharp
public void Aplicar(Entidad invocador, Entidad objetivo, ...)
{
    // 1. Daño base de la habilidad
    float danoBase = baseDamage;
    if (usaPorcentajeVidaObjetivo)
        danoBase = objetivo.VidaActual * (baseDamage / 100f);

    // 2. Escalado con ATK
    if (escaladoATK > 0)
        danoBase += invocador.PuntosDeAtaque_Entidad * escaladoATK;

    // 3. Decidir crit FUERA del pipeline
    bool isCritical = Random.value <= critChance;

    // 4. Crear contexto pre-configurado
    var context = DamagePipeline.CreateContext(invocador, objetivo, isCritical);
    context.PhysicalDamage = danoBase;
    context.HasBaseValues = true;  // BaseDamageModifier no sobreescribe

    // 5. Ejecutar pipeline completo
    DamagePipeline.Default.Execute(context);
    //    → Race → Crit → Defense → ElemResist
    //    → Entity Modifiers (pasivas)
    //    → EffectHandler (efectos activos)
    //    → FinalClamp

    // 6. Aplicar daño procesado
    objetivo.AplicarDanoDesdeContexto(context);
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
| Ataque Básico | 0 | None | 1.0 | Solo ATK (pipeline aplica race, crit, def) |
| Golpe Fuerte | 15 | None | 1.0 | ATK + 15 (pipeline) |
| Bola de Fuego | 25 | Fire | 1.0 | ATK + 25, elem resist aplica |
| Ejecución | 20 | None | 1.5 | ATK×1.5 + 20, ignora defensa si configurado |
| Drenar Vida | 15 | Dark | 0 | Solo 15 (sin escalado ATK) |
| % HP Golpe | 10 | None | 0 | 10% de la HP actual del objetivo |

---

## HealEffect

**Archivo**: `Assets/Scripts/Todohabilidades/HealEffect.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `healAmount` | int | Cantidad base de curación |
| `porcentajeVidaMax` | bool | Si es % de vida máxima |

### Lógica de Curación

```csharp
public void Aplicar(Entidad invocador, Entidad objetivo, ...)
{
    if (objetivo == null || !objetivo.EstaVivo()) return;

    int curacion;
    
    if (porcentajeVidaMax)
    {
        // healAmount es un porcentaje (ej: 25 = 25%)
        curacion = (objetivo.Vida_Entidad * healAmount) / 100;
    }
    else
    {
        // healAmount es valor fijo
        curacion = healAmount;
    }
    
    objetivo.Curar(curacion);
}
```

### Configuración en Inspector

```
[HealEffect]
├── Heal Amount: 40          ← Curación fija
└── Porcentaje Vida Max: ☐   ← Desactivado = fijo
```

O para curación porcentual:
```
[HealEffect]
├── Heal Amount: 25          ← 25% de vida máxima
└── Porcentaje Vida Max: ☑   ← Activado = porcentaje
```

### Ejemplos de Uso

| Habilidad | healAmount | porcentaje | Resultado |
|-----------|------------|------------|-----------|
| Curar Menor | 30 | false | +30 HP |
| Curar Mayor | 80 | false | +80 HP |
| Regeneración | 15 | true | +15% HP max |
| Curación Completa | 100 | true | +100% HP max |

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

### Estados Disponibles (StatusFlag)

```csharp
[Flags]
public enum StatusFlag
{
    None = 0,
    Poisoned = 1,     // Daño por turno
    Burned = 2,       // Daño por turno (fuego)
    Frozen = 4,       // Ralentizado
    Stunned = 8,      // No puede actuar
    Paralyzed = 16,   // No puede actuar
    Buffed = 32,      // Stats aumentadas
    Debuffed = 64,    // Stats reducidas
    Sleeping = 128,   // No puede actuar
    Confused = 256    // Puede atacar aliados
}
```

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

### Ejemplo: LifeStealEffect (usa Pipeline)

```csharp
// Assets/Scripts/Todohabilidades/LifeStealEffect.cs
using UnityEngine;
using System.Collections.Generic;
using Interfaces;
using Padres;
using Flags;
using Combate;

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

        // Resolver crit
        float critChance = invocador.CombatStats?.critChance ?? 0.05f;
        bool isCrit = Random.value <= critChance;

        // Crear contexto y ejecutar pipeline
        var ctx = DamagePipeline.CreateContext(invocador, objetivo, isCrit);
        ctx.PhysicalDamage = baseDamage + invocador.PuntosDeAtaque_Entidad;
        ctx.HasBaseValues = true;
        if (tipoDano != ElementAttribute.None) ctx.AttackElement = tipoDano;

        DamagePipeline.Default.Execute(ctx);
        objetivo.AplicarDanoDesdeContexto(ctx);

        // Curar al invocador (% del daño final)
        int curacion = Mathf.RoundToInt(ctx.FinalDamage * porcentajeRobo);
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
      - healAmount: 20
      - porcentajeVidaMax: false
      
  [1] StatusEffect
      - statusAplicar: Buffed
      - duracionTurnos: 2
      - danoPorTurno: 0
      - modificadorStats: 0.15  ← +15% stats
```

---

## Efectos Pasivos (IPasivaEffect)

Los efectos pasivos se aplican/remueven automáticamente cuando una `PasivaData` se activa/desactiva en la entidad. Se configuran como ScriptableObjects en Unity.

### DamageModifierPasivaEffect

**Archivo**: `Assets/Scripts/Todohabilidades/Pasivas/DamageModifierPasivaEffect.cs`

Registra un `IDamageModifier` en la entidad, que se ejecuta durante el `DamagePipeline`.

#### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `canal` | CanalDano | Physical, Elemental, o Ambos |
| `tipoMod` | TipoModificadorDano | Porcentaje, Plano, o EscaladoPorStat |
| `valor` | float | Magnitud del modificador |
| `statEscalado` | StatType | Stat a usar (solo para EscaladoPorStat) |
| `order` | int (600-900) | Prioridad en el pipeline |
| `soloComoAtacante` | bool | Si true, solo aplica cuando ataca; si false, cuando defiende |

#### Configuración de Ejemplo

**Mago (+20% daño cuando ataca)**:
```
[DamageModifierPasivaEffect]
├── Canal: Ambos
├── Tipo Mod: Porcentaje
├── Valor: 20                    ← +20%
├── Order: 650
└── Solo Como Atacante: ☑
```

**Arquero (bonus por velocidad)**:
```
[DamageModifierPasivaEffect]
├── Canal: Physical
├── Tipo Mod: EscaladoPorStat
├── Valor: 2                     ← +2% de VEL como daño
├── Stat Escalado: Velocidad
├── Order: 650
└── Solo Como Atacante: ☑
```

**Goblin (0.8x daño)**:
```
[DamageModifierPasivaEffect]
├── Canal: Ambos
├── Tipo Mod: Porcentaje
├── Valor: -20                   ← -20% = ×0.8
├── Order: 650
└── Solo Como Atacante: ☑
```

### TriggerCombateEffect

**Archivo**: `Assets/Scripts/Todohabilidades/Pasivas/TriggerCombateEffect.cs`

Efecto pasivo que se activa en respuesta a eventos de combate.

#### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `trigger` | TipoTrigger | AlGolpear, AlSerGolpeado, AlMatar, AlCurar |
| `probabilidad` | float (0-100) | Probabilidad de activarse |
| `efectoTrigger` | TipoEfectoTrigger | CurarVida, DanoAdicional, AplicarEstado, ReducirCooldown |
| `valorEfecto` | float | Magnitud del efecto |
| `usaPorcentajeDelDano` | bool | Si true, valorEfecto es % del daño |

#### Configuración de Ejemplo

**Vampirismo (robar HP al golpear)**:
```
[TriggerCombateEffect]
├── Trigger: AlGolpear
├── Probabilidad: 20             ← 20% de chance
├── Efecto Trigger: CurarVida
├── Valor Efecto: 30             ← 30% del daño
└── Usa Porcentaje Del Dano: ☑
```

> **Nota**: `TriggerCombateEffect` actualmente tiene un TODO para conectar con los eventos de `Entidad`. Los eventos `OnDañoRecibido` y `OnMuerte` ya existen — falta suscribir el trigger a ellos.

---

## Cómo Crear Pasivas con Efectos en Unity

1. **Assets → Create → Combate/Pasiva Data**
2. Configurar nombre, icono, descripción, categoría
3. En la lista de **efectos**, click en **+** y elegir:
   - `DamageModifierPasivaEffect` para bonus de daño
   - `TriggerCombateEffect` para triggers de combate
4. Configurar el efecto según la tabla anterior
5. **Asignar la pasiva** al `ClaseData.pasivasIniciales` o `EnemigoData.pasivas`
6. Al crearse la entidad, las pasivas se activan automáticamente

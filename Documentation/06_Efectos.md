# Sistema de Efectos

## Visión General

Los efectos son clases que implementan `IHabilidadEffect` y definen qué sucede cuando se usa una habilidad. Se pueden combinar múltiples efectos en una sola habilidad.

```
IHabilidadEffect
    ├── DamageEffect   → Inflige daño
    ├── HealEffect     → Cura vida
    └── StatusEffect   → Aplica estados
```

---

## Interfaz IHabilidadEffect

**Archivo**: `Assets/Scripts/Interfaces/IHabilidadesCommands.cs`

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

## DamageEffect

**Archivo**: `Assets/Scripts/Todohabilidades/DamageEffect.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `baseDamage` | int | Daño base adicional |
| `tipoDano` | ElementAttribute | Tipo elemental del daño |

### Cálculo de Daño

```csharp
public void Aplicar(Entidad invocador, Entidad objetivo, ...)
{
    if (objetivo == null || !objetivo.EstaVivo()) return;

    // Daño = base + ATK del invocador
    int danoBruto = baseDamage + invocador.PuntosDeAtaque_Entidad;

    // El objetivo calcula mitigación internamente
    objetivo.RecibirDano(danoBruto, tipoDano);
}
```

### Configuración en Inspector

```
[DamageEffect]
├── Base Damage: 20      ← Daño fijo de la habilidad
└── Tipo Dano: Fire      ← Elemento (None = físico)
```

### Ejemplos de Uso

| Habilidad | baseDamage | tipoDano | Resultado |
|-----------|------------|----------|-----------|
| Ataque Básico | 0 | None | Solo ATK |
| Golpe Fuerte | 15 | None | ATK + 15 |
| Bola de Fuego | 25 | Fire | ATK + 25 (fuego) |
| Rayo | 30 | Wind | ATK + 30 (viento) |

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

## Crear un Nuevo Efecto

### Ejemplo: LifeStealEffect

```csharp
// Assets/Scripts/Todohabilidades/LifeStealEffect.cs
using UnityEngine;
using System.Collections.Generic;
using Interfaces;
using Padres;
using Flags;

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
        int dano = baseDamage + invocador.PuntosDeAtaque_Entidad;
        
        // Aplicar daño
        objetivo.RecibirDano(dano, tipoDano);
        
        // Calcular curación (% del daño infligido)
        int curacion = (int)(dano * porcentajeRobo);
        
        // Curar al invocador
        if (curacion > 0)
        {
            invocador.Curar(curacion);
            Debug.Log(invocador.Nombre_Entidad + " roba " + curacion + " de vida!");
        }
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

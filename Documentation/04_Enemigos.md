# Sistema de Enemigos

> Documentación de la clase abstracta `Enemigos` y todas sus subclases concretas (Goblin, Orcos, Dragon), el sistema de IA por arquetipos y la configuración via `EnemigoData`.
> Para la infraestructura base compartida ver [02_Entidades.md](02_Entidades.md).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Jerarquía de Clases](#jerarquía-de-clases)
- [EnemigoData — Configuración en Unity](#enemigodataconfiguración-en-unity)
- [Goblin](#goblin)
- [Orcos](#orcos)
- [Dragon](#dragon)
- [Tabla Comparativa](#tabla-comparativa)
- [Crear un Nuevo Tipo de Enemigo](#crear-un-nuevo-tipo-de-enemigo)
- [Flujo de Inicialización](#flujo-de-inicialización)
- [Flujo por Turno](#flujo-por-turno)
- [⚠ TODOs en código](#-todos-en-código)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Padres/Enemigos.cs](../Assets/Scripts/Padres/Enemigos.cs) | Clase abstracta base para todos los enemigos |
| [Assets/Scripts/Subclases/Enemigos/Goblin.cs](../Assets/Scripts/Subclases/Enemigos/Goblin.cs) | Clase Goblin |
| [Assets/Scripts/Subclases/Enemigos/Orcos.cs](../Assets/Scripts/Subclases/Enemigos/Orcos.cs) | Clase Orcos |
| [Assets/Scripts/Subclases/Enemigos/Dragon.cs](../Assets/Scripts/Subclases/Enemigos/Dragon.cs) | Clase Dragon |
| [Assets/Scripts/SO/EnemigoData.cs](../Assets/Scripts/SO/EnemigoData.cs) | ScriptableObject de configuración de enemigo |
| [Assets/Scripts/Controllers/EnemyController.cs](../Assets/Scripts/Controllers/EnemyController.cs) | Componente Unity que envuelve una entidad enemigo |
| [Assets/Scripts/IA/CerebroIA.cs](../Assets/Scripts/IA/CerebroIA.cs) | Cerebro de IA generado por arquetipo |

---

## Jerarquía de Clases

```
Enemigos (abstracta) : Entidad, IEntidadActuable
    ├── Goblin      → stats bajos, velocidad alta
    ├── Orcos       → stats medios-altos, resistente
    └── Dragon      → stats máximos, críticos
```

La clase abstracta `Enemigos` contiene toda la lógica común: stats, `GestorHabilidades`, `HabilidadPorDefecto`, `CerebroIA` y la implementación de `ObtenerAccionElegida`. Las subclases aportan el escalado de stats, `DecidirObjetivo` (fallback de objetivo) y, opcionalmente, configuran `CombatStats` en el constructor (ej: Dragon con 30% crítico). El comportamiento de combate se define en el `EnemigoData` mediante el `arquetipoIA`.

---

## EnemigoData — Configuración en Unity

Cada enemigo se configura mediante un `EnemigoData` (ScriptableObject).

`[CreateAssetMenu(menuName = "Combate/Enemigo Data")]`

| Header Unity | Campos |
|---|---|
| Info General | `nombreEnemigo`, `tipoEnemigo` (string: `"Goblin"`, `"Orcos"`, `"Dragon"`) |
| Stats Base | `vidaBase`, `ataqueBase`, `defensaBase`, `velocidadBase`, `nivelBase` |
| Recompensas | `xpOtorgada` |
| Atributos | `atributos`, `tipoEntidad`, `estiloCombate` |
| Arquetipo IA | `arquetipoIA` (`ArquetipoIA`) — define el rol de combate |
| IA y Roaming | `radioDeteccion`, `radioPersecucion`, `rangoAliados`, `alertaAliados` |
| Visual | `modeloPrefab`, `animatorOverride` |
| Habilidades | `habilidades` (`List<HabilidadData>`), `pasivas` (`List<PasivaData>`), `habilidadPorDefecto` |

**Un mismo tipo de enemigo puede tener distintos arquetipos**: `GoblinGuerrero.asset`, `GoblinSanador.asset` y `GoblinBerserk.asset` usan todos la subclase `Goblin` pero con `arquetipoIA` diferente.

`CrearInstancia()` usa switch expression para instanciar la subclase correcta por string:

```csharp
return tipoEnemigo switch
{
    "Goblin" => new Subclases.Goblin(this),
    "Orcos"  => new Subclases.Orcos(this),
    "Dragon" => new Subclases.Dragon(this),
    _        => throw new System.Exception($"Tipo '{tipoEnemigo}' no implementado")
};
```

---

## Goblin

**Archivo**: `Assets/Scripts/Subclases/Enemigos/Goblin.cs`

### Características

| Stat | Valor Base | Por Nivel |
|------|------------|-----------|
| Vida | 50 | +50 |
| Ataque | 8 | +8 |
| Defensa | 3 | +3 |
| Velocidad | 12 | +3 |
| XP | 25 | — |

### Escalado (EscaladoGoblin)

```csharp
private static readonly EscaladoEnemigo EscaladoGoblin = new EscaladoEnemigo(
    vida: 50, ataque: 8, defensa: 3f, velocidad: 3
);
```

### Cálculo de Daño

Goblins aplican un penalizador de 0.8× al daño para representar su debilidad física. Lo compensan con mayor velocidad (actúan antes).

```csharp
public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    return (int)(PuntosDeAtaque_Entidad * 0.8f);
}
```

### Comportamiento de IA

El comportamiento **no está en la subclase** sino en el `arquetipoIA` del `EnemigoData`. Un `GoblinGuerrero.asset` usa `RolGuerrero`, un `GoblinSanador.asset` usa `RolSanador`. `DecidirObjetivo` es solo el fallback del árbol; el `CerebroIA` tiene prioridad.

```csharp
// Fallback si CerebroIA no retorna resultado
public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
{
    var vivos = jugadores.Where(j => j.EstaVivo()).ToList();
    if (vivos.Count == 0) return null;
    return vivos[UnityEngine.Random.Range(0, vivos.Count)];
}
```

### Configuración EnemigoData (ejemplo: GoblinGuerrero)

```
tipoEnemigo:      "Goblin"
arquetipoIA:      Guerrero
Vida Base:        50
Ataque Base:      8
Defensa Base:     3
Velocidad Base:   12
XP Otorgada:      25
Tipo Entidad:     Humanoide
Estilo Combate:   Melee
habilidades:      [ AtaqueSable, EmpujonGoblin ]
habilidadPorDefecto: AtaqueSable
```

---

## Orcos

**Archivo**: `Assets/Scripts/Subclases/Enemigos/Orcos.cs`

### Características

| Stat | Valor Base | Por Nivel |
|------|------------|-----------|
| Vida | 80 | +150 |
| Ataque | 12 | +15 |
| Defensa | 6 | +8 |
| Velocidad | 6 | +1 |
| XP | 40 | — |

### Escalado (EscaladoOrco)

```csharp
private static readonly EscaladoEnemigo EscaladoOrco = new EscaladoEnemigo(
    vida: 150, ataque: 15, defensa: 8f, velocidad: 1
);
```

### Cálculo de Daño

Daño estándar sin modificadores.

```csharp
public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    return PuntosDeAtaque_Entidad;
}
```

### Configuración EnemigoData (ejemplo: OrcoGuerrero)

```
tipoEnemigo:      "Orcos"
arquetipoIA:      Guerrero
Vida Base:        80
Ataque Base:      12
Defensa Base:     6
Velocidad Base:   6
XP Otorgada:      40
Tipo Entidad:     Humanoide
Estilo Combate:   Melee
```

---

## Dragon

**Archivo**: `Assets/Scripts/Subclases/Enemigos/Dragon.cs`

### Características

| Stat | Valor Base | Por Nivel |
|------|------------|-----------|
| Vida | 500 | +300 |
| Ataque | 45 | +25 |
| Defensa | 25 | +15 |
| Velocidad | 8 | +2 |
| XP | 500 | — |

### Escalado (EscaladoDragon)

```csharp
private static readonly EscaladoEnemigo EscaladoDragon = new EscaladoEnemigo(
    vida: 300, ataque: 25, defensa: 15f, velocidad: 2
);
```

### Críticos vía CombatStats

Dragon no sobreescribe `CalcularDanoContra` — usa el pipeline de `DamageCalculator` vía clase base. Sus críticos se configuran en el constructor:

```csharp
CombatStats.critChance     = 0.30f;  // 30% crítico
CombatStats.critMultiplier = 2.0f;   // ×2 daño crítico
CombatStats.elementoAtaque = datos.atributos;
```

### DecidirObjetivo

Dragon prioriza al jugador con más vida (fallback del árbol IA):

```csharp
public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
{
    var vivos = jugadores.Where(j => j.EstaVivo()).ToList();
    if (vivos.Count == 0) return null;
    return vivos.OrderByDescending(j => j.VidaActual_Entidad).First();
}
```

### Configuración EnemigoData

```
tipoEnemigo:      "Dragon"
arquetipoIA:      Berserk   ← siempre ataca al tanque
Atributos:        Fire
Tipo Entidad:     Dragon
Estilo Combate:   Ranged
```

---

## Tabla Comparativa

| Enemigo | HP | ATK | DEF | VEL | Modificador daño | Especial |
|---------|----|----|-----|-----|-----------------|----------|
| Goblin  | 50 | 8  | 3   | 12  | ×0.8 | Velocidad alta |
| Orcos   | 80 | 12 | 6   | 6   | ×1.0 | — |
| Dragon  | 500 | 45 | 25 | 8  | ×1.0 / ×2 crit | 30% crit vía CombatStats |
| Esqueleto | configurable | configurable | configurable | configurable | ×1.0 | Inmune veneno, +50% daño luz |

---

## Crear un Nuevo Tipo de Enemigo

### Paso 1 — Crear la subclase

Solo necesitás definir el escalado de stats y el cálculo de daño. El comportamiento de IA no va acá.

```csharp
// Assets/Scripts/Subclases/Enemigos/Esqueleto.cs
namespace Subclases
{
    public class Esqueleto : Enemigos
    {
        private static readonly EscaladoEnemigo EscaladoEsqueleto = new EscaladoEnemigo(
            vida: 12, ataque: 3, defensa: 2f, velocidad: 1
        );

        public Esqueleto(EnemigoData datos)
            : base(datos.nombreEnemigo, datos.vidaBase, datos.ataqueBase, datos.defensaBase,
                   datos.nivelBase, datos.velocidadBase, (int)datos.xpOtorgada,
                   datos.atributos, datos.tipoEntidad, datos.estiloCombate, EscaladoEsqueleto)
        {
            InicializarDesdeEnemigoData(datos); // inicializa habilidades + CerebroIA
        }

        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Fallback del árbol: ataca al jugador con menos defensa
            var vivos = jugadores.Where(j => j.EstaVivo()).ToList();
            return vivos.Count == 0 ? null : vivos.OrderBy(j => j.PuntosDeDefensa_Entidad).First();
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            return PuntosDeAtaque_Entidad;
        }

        // Inmune a veneno
        public override void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno, float modificador)
        {
            if (status == StatusFlag.Poisoned) return;
            base.AplicarEstado(status, duracion, danoPorTurno, modificador);
        }

        // Recibe +50% daño de luz
        protected override int AplicarMitigacionPorFaccion(int danoBruto, ElementAttribute tipo)
        {
            if (tipo == ElementAttribute.Light) return (int)(danoBruto * 1.5f);
            return danoBruto;
        }
    }
}
```

### Paso 2 — Registrar en EnemigoData

```csharp
// SO/EnemigoData.cs — método CrearInstancia()
public Enemigos CrearInstancia() => tipoEnemigo switch
{
    "Goblin"   => new Subclases.Goblin(this),
    "Orcos"    => new Subclases.Orcos(this),
    "Dragon"   => new Subclases.Dragon(this),
    "Esqueleto" => new Subclases.Esqueleto(this),  // ← agregar
    _ => throw new System.Exception($"Tipo '{tipoEnemigo}' no implementado")
};
```

### Paso 3 — Crear el ScriptableObject en Unity

1. Click derecho en el Project → **Create > Combate > Enemigo Data**
2. Configurar stats, tipo y habilidades
3. Asignar `arquetipoIA` según el rol deseado
4. No hay paso 4 — el `CerebroIA` se crea automáticamente al inicializar

---

## Flujo de Inicialización

```
EnemyController.Inicializar(EnemigoData datos)
    └── CrearEntidadLogica(datos)
            └── datos.CrearInstancia()            → new Goblin(datos)
                    └── base(...)                  → Enemigos constructor
                    └── InicializarDesdeEnemigoData(datos)
                            └── GestorHabilidades = new GestorHabilidades(this, datos.habilidades)
                            └── HabilidadPorDefecto = datos.habilidadPorDefecto
                            └── foreach pasiva → GestorPasivas.AgregarPasiva(pasiva)
                            └── CerebroIA = CerebroIA.CrearParaArquetipo(datos.arquetipoIA)
                            └── CerebroIA.Configurar(this)
```

---

## Flujo por Turno

```
CombateManager llama a EnemyController.ObtenerAccionElegida(aliados, enemigos)
    └── delega a enemigoLogica.ObtenerAccionElegida(aliados, enemigos)
            └── CerebroIA.Decidir(jugadores: enemigos, aliados: aliados)
                    └── IDecisionRol.Decidir(yo, jugadores, aliados)
                            └── árbol de NodoIA evalúa condiciones
                            └── nodo de acción selecciona habilidad del GestorHabilidades
                            └── setea ContextoIA.UltimoResultado
            └── retorna (resultado.Habilidad, resultado.Objetivo)
    └── CombateManager ejecuta la habilidad
```

---

## ⚠ TODOs en código

> Extraídos de controladores de enemigos.

- **`EnemyController.cs:358`** — `TODO: Pasar atacante si está disponible` — Al publicar `EventoEnemigoDerrotado`, el campo `Asesino` se fija siempre en `null`; se necesita propagar el atacante desde el pipeline de daño.
- **`EnemyController.cs:567`** — `TODO: Agregar más condiciones específicas del enemigo` — El método de evaluación de condiciones tiene un placeholder para: cooldown de combate, estado de misión, hora del día/bioma, grupo de enemigos.

# Sistema de Enemigos

## Jerarquía de Clases

```
Enemigos (abstracta)
    ├── Goblin      → Ataque aleatorio, rápido
    ├── Orcos       → Ataque aleatorio, resistente
    └── Dragon      → Ataca al más fuerte, críticos
```

---

## Goblin

**Archivo**: `Assets/Scripts/Subclases/Goblin.cs`

### Características

| Stat | Valor Base | Por Nivel |
|------|------------|-----------|
| Vida | 50 | +10 |
| Ataque | 8 | +3 |
| Defensa | 3 | +1 |
| Velocidad | 12 | +2 |
| XP | 25 | x1.2 |
| Oro | 10 | x1.1 |

### Comportamiento de IA

```csharp
public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
{
    // Goblin ataca ALEATORIAMENTE
    var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
    if (jugadoresVivos.Count == 0) return null;

    int indice = UnityEngine.Random.Range(0, jugadoresVivos.Count);
    return jugadoresVivos[indice];
}
```

### Cálculo de Daño

```csharp
public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    // Goblins hacen MENOS daño pero atacan más rápido
    return (int)(PuntosDeAtaque_Entidad * 0.8f);
}
```

### Configuración EnemigoData

```
Nombre: "Goblin"
Tipo Enemigo: Goblin
Vida Base: 50
Ataque Base: 8
Defensa Base: 3
Velocidad Base: 12
XP Otorgada: 25
Oro Otorgado: 10
Tipo Entidad: Humanoide
Estilo Combate: Melee
```

---

## Orcos

**Archivo**: `Assets/Scripts/Subclases/Orcos.cs`

### Características

| Stat | Valor Base | Por Nivel |
|------|------------|-----------|
| Vida | 80 | +18 |
| Ataque | 12 | +4 |
| Defensa | 6 | +2 |
| Velocidad | 6 | +1 |
| XP | 40 | x1.25 |
| Oro | 20 | x1.15 |

### Comportamiento de IA

```csharp
public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
{
    // Orcos atacan ALEATORIAMENTE (igual que Goblin)
    var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
    if (jugadoresVivos.Count == 0) return null;

    int indice = UnityEngine.Random.Range(0, jugadoresVivos.Count);
    return jugadoresVivos[indice];
}
```

### Cálculo de Daño

```csharp
public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    // Orcos hacen daño estándar
    return PuntosDeAtaque_Entidad;
}
```

### Configuración EnemigoData

```
Nombre: "Orco Guerrero"
Tipo Enemigo: Orcos
Vida Base: 80
Ataque Base: 12
Defensa Base: 6
Velocidad Base: 6
XP Otorgada: 40
Oro Otorgado: 20
Tipo Entidad: Humanoide
Estilo Combate: Melee
```

---

## Dragon

**Archivo**: `Assets/Scripts/Subclases/Dragon.cs`

### Características

| Stat | Valor Base | Por Nivel |
|------|------------|-----------|
| Vida | 500 | +50 |
| Ataque | 45 | +8 |
| Defensa | 25 | +5 |
| Velocidad | 8 | +1 |
| XP | 500 | x1.5 |
| Oro | 200 | x1.3 |

### Constantes Especiales

```csharp
private const float PROBABILIDAD_CRITICO = 0.2f;  // 20%
private const int MULTIPLICADOR_CRITICO = 2;       // x2 daño
```

### Comportamiento de IA

```csharp
public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
{
    // Dragon ataca al jugador con MÁS VIDA (el más amenazante)
    var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
    if (jugadoresVivos.Count == 0) return null;
    
    return jugadoresVivos.OrderByDescending(j => j.VidaActual_Entidad).First();
}
```

### Cálculo de Daño con Críticos

```csharp
public override int CalcularDanoContra(IEntidadCombate objetivo)
{
    int danoBase = PuntosDeAtaque_Entidad;
    
    // 20% de probabilidad de crítico
    if (UnityEngine.Random.value < PROBABILIDAD_CRITICO)
    {
        Debug.Log(Nombre_Entidad + " hace un ataque critico!");
        return danoBase * MULTIPLICADOR_CRITICO;
    }
    
    return danoBase;
}
```

### Configuración EnemigoData

```
Nombre: "Dragon Ancestral"
Tipo Enemigo: Dragon
Vida Base: 500
Ataque Base: 45
Defensa Base: 25
Velocidad Base: 8
XP Otorgada: 500
Oro Otorgado: 200
Atributos: Fire
Tipo Entidad: Dragon
Estilo Combate: Ranged
```

---

## Crear un Nuevo Tipo de Enemigo

### Paso 1: Crear la Subclase

```csharp
// Assets/Scripts/Subclases/Esqueleto.cs
using System.Collections.Generic;
using System.Linq;
using Padres;
using Interfaces;
using Flags;

namespace Subclases
{
    public class Esqueleto : Enemigos
    {
        // Escalado específico
        private static readonly EscaladoEnemigo EscaladoEsqueleto = new EscaladoEnemigo
        {
            vidaPorNivel = 12,
            ataquePorNivel = 3,
            defensaPorNivel = 2f,
            velocidadPorNivel = 1,
            xpPorNivel = 1.15f,
            oroPorNivel = 1.1f
        };

        public Esqueleto(EnemigoData datos) 
            : base(
                datos.nombre,
                datos.vidaBase,
                datos.ataqueBase,
                datos.defensaBase,
                datos.nivelBase,
                datos.velocidadBase,
                datos.xpOtorgada,
                datos.oroOtorgado,
                datos.atributos,
                datos.tipoEntidad,
                datos.estiloCombate,
                EscaladoEsqueleto
            )
        {
        }

        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Esqueletos atacan al jugador con MENOS defensa
            var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
            if (jugadoresVivos.Count == 0) return null;
            
            return jugadoresVivos.OrderBy(j => j.PuntosDeDefensa_Entidad).First();
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            return PuntosDeAtaque_Entidad;
        }

        // Esqueletos son inmunes a veneno
        public override void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno, float modificador)
        {
            if (status == StatusFlag.Poisoned)
            {
                Debug.Log(Nombre_Entidad + " es inmune al veneno!");
                return;
            }
            base.AplicarEstado(status, duracion, danoPorTurno, modificador);
        }

        // Esqueletos son débiles a daño sagrado
        protected override int AplicarMitigacionPorFaccion(int danoBruto, ElementAttribute tipo)
        {
            if (tipo == ElementAttribute.Light)
            {
                return (int)(danoBruto * 1.5f); // +50% daño de luz
            }
            return danoBruto;
        }
    }
}
```

### Paso 2: Añadir al Enum

```csharp
public enum TipoEnemigo
{
    Goblin,
    Orcos,
    Dragon,
    Esqueleto  // Añadir
}
```

### Paso 3: Actualizar EnemigoData.CrearInstancia()

```csharp
public Enemigos CrearInstancia()
{
    switch (tipoEnemigo)
    {
        case TipoEnemigo.Goblin:
            return new Goblin(this);
        case TipoEnemigo.Orcos:
            return new Orcos(this);
        case TipoEnemigo.Dragon:
            return new Dragon(this);
        case TipoEnemigo.Esqueleto:    // Añadir case
            return new Esqueleto(this);
        default:
            return new Goblin(this);
    }
}
```

---

## Tabla Comparativa de Enemigos

| Enemigo | HP | ATK | DEF | VEL | Comportamiento | Especial |
|---------|----|----|-----|-----|----------------|----------|
| Goblin | 50 | 8 | 3 | 12 | Aleatorio | -20% daño |
| Orcos | 80 | 12 | 6 | 6 | Aleatorio | Estándar |
| Dragon | 500 | 45 | 25 | 8 | Más vida | 20% crítico x2 |
| Esqueleto | 60 | 10 | 4 | 8 | Menos DEF | Inmune veneno |

---

## Sistema de IA Avanzado (Opcional)

Para IA más compleja, usar el sistema de árboles de comportamiento:

```csharp
// En EnemyController o una subclase
private CerebroIA cerebroIA;

void Start()
{
    // IA básica
    cerebroIA = CerebroIA.CrearBasico();
    
    // O IA agresiva para jefes
    cerebroIA = CerebroIA.CrearAgresivo();
    
    cerebroIA.Configurar(enemigoLogica);
}

public override (IHabilidadesCommand, IEntidadCombate) ObtenerAccionElegida(...)
{
    var resultado = cerebroIA.Decidir(jugadores, aliados);
    
    if (resultado != null)
    {
        return (datosEnemigo.HabilidadPorDefecto, resultado.Objetivo);
    }
    
    return (null, null);
}
```

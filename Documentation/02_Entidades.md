# Sistema de Entidades

## Jerarquía de Clases

```
Entidad (abstracta)
    ├── Jugador (abstracta)
    │       └── Guerrero
    │       └── [Otras clases futuras]
    │
    └── Enemigos (abstracta)
            ├── Goblin
            ├── Orcos
            └── Dragon
```

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
| `GestorEstados` | GestorEstados | Maneja estados alterados |

### Propiedades Abstractas

```csharp
public abstract TipoEntidades TipoEntidad { get; }
public abstract ElementAttribute AtributosEntidad { get; }
```

### Métodos Principales

```csharp
// Verificación de estado
bool EstaVivo()      // VidaActual > 0 && !EstaMuerto
bool PuedeActuar()   // EstaVivo && !EsDerrotado && !Incapacitado

// Combate
void RecibirDano(int danoBruto, ElementAttribute tipo)
int Curar(int cantidad)
int CalcularDanoContra(IEntidadCombate objetivo)

// Estados
void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno, float modificador)
bool TieneEstado(StatusFlag status)
void RemoverEstado(StatusFlag status)
bool ProcesarEstadosInicioTurno()  // Retorna true si puede actuar

// Abstractos (implementar en subclases)
abstract bool EsTipoEntidad(TipoEntidades tipo)
abstract bool UsaEstiloDeCombate(CombatStyle estilo)
```

### Eventos

```csharp
event Action<int, int> OnVidaCambiada;  // (vidaActual, vidaMaxima)
event Action<int> OnDañoRecibido;       // (cantidad)
event Action OnMuerte;
```

### Fórmula de Mitigación de Daño

```csharp
// 1. Mitigación por facción (virtual, override en subclases)
int danoDespuesFaccion = AplicarMitigacionPorFaccion(danoBruto, tipo);

// 2. Mitigación por defensa (fórmula logarítmica)
float multiplicador = 1f - (Defensa / (Defensa + 100f));
int danoFinal = Max(1, danoDespuesFaccion * multiplicador);
```

---

## Jugador

**Archivo**: `Assets/Scripts/Padres/Jugador.cs`  
**Namespace**: `Padres`

### Propiedades Adicionales

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Mana_jugador` | int | Mana máximo |
| `ManaActual_jugador` | int | Mana actual |
| `Experiencia_Actual` | float | XP acumulada en nivel actual |
| `Experiencia_Progreso` | float | XP necesaria para subir |

### Eventos Adicionales

```csharp
event Action<int> OnNivelSubido;           // (nuevoNivel)
event Action<float, float> OnXPGanada;     // (xpActual, xpNecesaria)
event Action<int, int> OnManaCambiado;     // (manaActual, manaMaximo)
```

### Sistema de Progresión

```csharp
// Recibir experiencia
void RecibirXP(float xp)

// Cálculo de XP necesaria (curva exponencial)
float CalcularXPNecesaria(int nivel)
// Fórmula: 100 * (nivel ^ 1.5)
// Nivel 1: 100 XP
// Nivel 5: 559 XP
// Nivel 10: 1581 XP
```

### Escalado de Jugador (EscaladoJugador)

```csharp
public class EscaladoJugador
{
    public int vidaPorNivel = 12;
    public int ataquePorNivel = 3;
    public float defensaPorNivel = 2f;
    public int manaPorNivel = 5;
    public int velocidadPorNivel = 1;
}
```

### Consumo de Mana

```csharp
bool TieneMana(int cantidad)
void ConsumirMana(int cantidad)
void RestaurarMana(int cantidad)
```

---

## Enemigos

**Archivo**: `Assets/Scripts/Padres/Enemigos.cs`  
**Namespace**: `Padres`

### Propiedades Adicionales

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `XPOtorgada` | float | XP que da al morir |
| `OroOtorgado` | int | Oro que da al morir |

### Métodos Abstractos

```csharp
// Decidir a quién atacar (implementar en subclases)
abstract IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
```

### Escalado de Enemigo (EscaladoEnemigo)

```csharp
public class EscaladoEnemigo
{
    public int vidaPorNivel = 15;
    public int ataquePorNivel = 4;
    public float defensaPorNivel = 2f;
    public int velocidadPorNivel = 1;
    public float xpPorNivel = 1.2f;      // Multiplicador
    public float oroPorNivel = 1.15f;    // Multiplicador
}
```

### Subir de Nivel (Enemigos)

```csharp
void SubirANivel(int nivelObjetivo)
// Aplica escalado multiplicativo para XP y oro
```

---

## Uso con Controladores

### EntityController (para Jugador)

```csharp
// El controlador crea la instancia lógica
public void Inicializar(ClaseData datos)
{
    entidadLogica = datos.CrearInstancia(); // Crea Guerrero, etc.
    entityStats.VincularEntidad(entidadLogica);
    
    if (entidadLogica is Jugador jugador)
    {
        jugador.VincularEntityStats(entityStats);
    }
}
```

### EnemyController (para Enemigos)

```csharp
public void Inicializar(EnemigoData datos)
{
    enemigoLogica = datos.CrearInstancia(); // Crea Goblin, etc.
    entityStats.VincularEntidad(enemigoLogica);
}
```

---

## Diagrama de Flujo: Recibir Daño

```
RecibirDano(danoBruto, tipo)
        │
        ▼
┌───────────────────────┐
│ AplicarMitigacionPor  │
│ Faccion (virtual)     │
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ Calcular mitigación   │
│ por defensa           │
│ mult = 1 - DEF/(DEF+100)
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ VidaActual -= dano    │
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ OnDañoRecibido.Invoke │
│ OnVidaCambiada.Invoke │
└───────────┬───────────┘
            │
            ▼
    ┌───────┴───────┐
    │ VidaActual<=0 │
    └───────┬───────┘
            │ Sí
            ▼
┌───────────────────────┐
│      Morir()          │
│ EstaMuerto = true     │
│ OnMuerte.Invoke       │
└───────────────────────┘
```

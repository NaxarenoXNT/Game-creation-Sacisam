# Sistema de Estados Alterados

## Visión General

El sistema de estados permite aplicar efectos temporales a las entidades: veneno, aturdimiento, buffs, debuffs, etc.

```
Entidad
    └── GestorEstados
            └── List<EstadoActivo>
                    ├── tipo (StatusFlag)
                    ├── turnosRestantes
                    ├── danoPorTurno
                    └── modificadorStats
```

---

## GestorEstados

**Archivo**: `Assets/Scripts/Estados/GestorEstados.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `EstaIncapacitado` | bool | Si tiene un estado que impide actuar |
| `estadosActivos` | List<EstadoActivo> | Lista de estados actuales |

### Estados Incapacitantes

```csharp
// Estos estados impiden actuar
StatusFlag.Stunned    // Aturdido
StatusFlag.Paralyzed  // Paralizado
StatusFlag.Sleeping   // Dormido
StatusFlag.Frozen     // Congelado
```

### Métodos Principales

```csharp
// Aplicar un nuevo estado
void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno, float modificador)

// Verificar si tiene un estado
bool TieneEstado(StatusFlag status)

// Remover un estado específico
bool RemoverEstado(StatusFlag status)

// Procesar al inicio del turno (retorna daño de estados)
int ProcesarInicioTurno()
```

---

## EstadoActivo

**Archivo**: `Assets/Scripts/Estados/EstadoActivo.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `tipo` | StatusFlag | Tipo de estado |
| `turnosRestantes` | int | Turnos que quedan |
| `danoPorTurno` | int | Daño cada turno (veneno, quemado) |
| `modificadorStats` | float | Modificador de stats |

### Verificar si Impide Actuar

```csharp
public bool ImpidenActuar
{
    get
    {
        return tipo == StatusFlag.Stunned ||
               tipo == StatusFlag.Paralyzed ||
               tipo == StatusFlag.Sleeping ||
               tipo == StatusFlag.Frozen;
    }
}
```

---

## StatusFlag (Enum)

**Archivo**: `Assets/Scripts/Flags/Tipo.cs`

```csharp
[Flags]
public enum StatusFlag
{
    None = 0,           // Sin estado
    
    // Estados de daño por turno
    Poisoned = 1,       // Envenenado (daño por turno)
    Burned = 2,         // Quemado (daño por turno)
    
    // Estados de control
    Frozen = 4,         // Congelado (no puede actuar, -velocidad)
    Stunned = 8,        // Aturdido (no puede actuar)
    Paralyzed = 16,     // Paralizado (no puede actuar)
    Sleeping = 128,     // Dormido (no puede actuar)
    Confused = 256,     // Confundido (puede atacar aliados)
    
    // Estados de modificación de stats
    Buffed = 32,        // Mejorado (+stats)
    Debuffed = 64       // Debilitado (-stats)
}
```

---

## Flujo de Estados

### Aplicar Estado

```
StatusEffect.Aplicar()
        │
        ▼
Entidad.AplicarEstado(status, duracion, dano, mod)
        │
        ▼
GestorEstados.AplicarEstado()
        │
        ├── ¿Ya tiene el estado?
        │       │
        │       ├── Sí → Refrescar duración (usar la mayor)
        │       │
        │       └── No → Crear nuevo EstadoActivo
        │
        ▼
Debug.Log("[Estado]: X ahora tiene Poisoned x3 turnos")
```

### Procesar Inicio de Turno

```
CombateManager → Entidad.ProcesarEstadosInicioTurno()
                        │
                        ▼
                GestorEstados.ProcesarInicioTurno()
                        │
                        ├── Para cada estado:
                        │       │
                        │       ├── Si tiene daño → sumar al total
                        │       │
                        │       ├── Reducir turnosRestantes
                        │       │
                        │       └── Si turnos = 0 → remover
                        │
                        ▼
                Retornar daño total
                        │
                        ▼
                Entidad aplica daño a VidaActual
                        │
                        ▼
                Retornar true/false (puede actuar)
```

---

## Uso en Combate

### En CombateManager

```csharp
private void EjecutarTurno(IEntidadCombate entidad)
{
    // Obtener la entidad lógica
    Entidad entidadLogica = ObtenerEntidadLogica(entidad);
    
    // Procesar estados al inicio del turno
    bool puedeActuar = entidadLogica.ProcesarEstadosInicioTurno();
    
    if (!puedeActuar)
    {
        Debug.Log(entidad.Nombre_Entidad + " está incapacitado!");
        return; // Saltar turno
    }
    
    // Continuar con el turno normal...
}
```

### En Habilidades (StatusEffect)

```csharp
// Aplicar veneno que hace 5 de daño por 3 turnos
objetivo.AplicarEstado(StatusFlag.Poisoned, 3, 5, 0);

// Aplicar aturdimiento por 1 turno
objetivo.AplicarEstado(StatusFlag.Stunned, 1, 0, 0);

// Aplicar buff de +20% stats por 3 turnos
aliado.AplicarEstado(StatusFlag.Buffed, 3, 0, 0.2f);
```

---

## Ejemplos de Configuración

### Veneno Débil
```
StatusFlag: Poisoned
Duración: 3 turnos
Daño/turno: 5
Modificador: 0
```

### Veneno Fuerte
```
StatusFlag: Poisoned
Duración: 5 turnos
Daño/turno: 12
Modificador: 0
```

### Quemadura
```
StatusFlag: Burned
Duración: 3 turnos
Daño/turno: 8
Modificador: 0
```

### Aturdimiento
```
StatusFlag: Stunned
Duración: 1 turno
Daño/turno: 0
Modificador: 0
```

### Parálisis
```
StatusFlag: Paralyzed
Duración: 2 turnos
Daño/turno: 0
Modificador: 0
```

### Congelación
```
StatusFlag: Frozen
Duración: 2 turnos
Daño/turno: 0
Modificador: 0.3  ← -30% velocidad si se implementa
```

### Buff de Ataque
```
StatusFlag: Buffed
Duración: 3 turnos
Daño/turno: 0
Modificador: 0.25  ← +25% stats
```

### Debuff de Defensa
```
StatusFlag: Debuffed
Duración: 3 turnos
Daño/turno: 0
Modificador: 0.20  ← -20% stats
```

---

## Tabla de Estados

| Estado | Impide Actuar | Daño/Turno | Mod Stats | Descripción |
|--------|---------------|------------|-----------|-------------|
| Poisoned | No | Sí | No | Daño constante |
| Burned | No | Sí | No | Daño de fuego |
| Frozen | Sí | No | Sí (-vel) | No puede moverse |
| Stunned | Sí | No | No | Aturdido |
| Paralyzed | Sí | No | No | Paralizado |
| Sleeping | Sí | No | No | Dormido |
| Confused | No | No | No | Ataca aleatorio |
| Buffed | No | No | Sí (+) | Stats aumentadas |
| Debuffed | No | No | Sí (-) | Stats reducidas |

---

## Integración con EventBus

```csharp
// Cuando se aplica un estado
EventBus.Publicar(new EventoEstadoAplicado 
{
    Entidad = entidad,
    Estado = statusFlag,
    Duracion = duracion
});

// Cuando se remueve un estado
EventBus.Publicar(new EventoEstadoRemovido 
{
    Entidad = entidad,
    Estado = statusFlag
});
```

Esto permite que la UI escuche y muestre iconos de estado:

```csharp
// En un script de UI
void Start()
{
    EventBus.Suscribir<EventoEstadoAplicado>(MostrarIconoEstado);
    EventBus.Suscribir<EventoEstadoRemovido>(OcultarIconoEstado);
}
```

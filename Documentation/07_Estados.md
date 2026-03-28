# Sistema de Estados Alterados

> Documentación del sistema de estados temporales sobre entidades: veneno, aturdimiento, quemado, congelado.
> Para el efecto que aplica estados desde habilidades ver [06_Efectos.md](06_Efectos.md) (`StatusEffect`).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Visión General](#visión-general)
- [StatusFlag (Enum)](#statusflag-enum)
- [EstadoActivo](#estadoactivo)
- [GestorEstados](#gestorestados)
- [Flujo de Estados](#flujo-de-estados)
- [Uso en Combate](#uso-en-combate)
- [Tabla de Estados](#tabla-de-estados)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Flags/Tipo.cs](../Assets/Scripts/Flags/Tipo.cs) | Define el enum `StatusFlag` |
| [Assets/Scripts/Estados/EstadoActivo.cs](../Assets/Scripts/Estados/EstadoActivo.cs) | Clase que representa un estado en curso |
| [Assets/Scripts/Estados/GestorEstados.cs](../Assets/Scripts/Estados/GestorEstados.cs) | Gestor de todos los estados de una entidad |

---

## Visión General

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

## StatusFlag (Enum)

**Archivo**: `Assets/Scripts/Flags/Tipo.cs`

```csharp
[Flags]
public enum StatusFlag
{
    None       = 0,
    Envenenado = 1 << 0,   // Daño por turno
    Aturdido   = 1 << 1,   // No puede actuar (impide actuar)
    Quemado    = 1 << 2,   // Daño por turno (fuego)
    Congelado  = 1 << 3    // No puede actuar, reducción de velocidad
}
```

---

## EstadoActivo

**Archivo**: `Assets/Scripts/Estados/EstadoActivo.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `tipo` | StatusFlag | Tipo de estado |
| `turnosRestantes` | int | Turnos que quedan |
| `danoPorTurno` | int | Daño aplicado cada turno (veneno, quemado) |
| `modificadorStats` | float | Modificador de stats (ej: 0.3 = -30% velocidad) |
| `HaExpirado` | bool | `turnosRestantes <= 0` |
| `ImpidenActuar` | bool | `true` si es `Aturdido` o `Congelado` |

### ImpidenActuar

```csharp
public bool ImpidenActuar => tipo == StatusFlag.Aturdido || tipo == StatusFlag.Congelado;
```

### Color por Estado (UI)

```csharp
public Color ObtenerColor()
{
    return tipo switch
    {
        StatusFlag.Envenenado => new Color(0.5f, 0f, 0.5f),   // Violeta
        StatusFlag.Aturdido   => Color.yellow,
        StatusFlag.Quemado    => new Color(1f, 0.5f, 0f),     // Naranja
        StatusFlag.Congelado  => Color.cyan,
        _                     => Color.white
    };
}
```

---

## GestorEstados

**Archivo**: `Assets/Scripts/Estados/GestorEstados.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `EstadosActivos` | IReadOnlyList\<EstadoActivo\> | Lista de estados activos (solo lectura) |
| `EstaIncapacitado` | bool | `true` si algún estado tiene `ImpidenActuar` |
| `EstadosActualesFlag` | StatusFlag | OR de todos los tipos activos |

### Eventos

| Evento | Firma | Cuándo se dispara |
|--------|-------|-------------------|
| `OnEstadoAplicado` | `Action<EstadoActivo>` | Al agregar un estado nuevo |
| `OnEstadoExpirado` | `Action<StatusFlag>` | Al expirar o remover manualmente |
| `OnDanoPorEstado` | `Action<int, StatusFlag>` | Cada turno que un estado causa daño |

### Métodos Principales

```csharp
// Aplicar un estado (si ya existe, sube duración/daño al mayor de los dos)
void AplicarEstado(StatusFlag tipo, int duracion, int danoPorTurno = 0, float modificador = 0f)

// Verificar si tiene un estado
bool TieneEstado(StatusFlag tipo)

// Obtener instancia de un estado activo
EstadoActivo ObtenerEstado(StatusFlag tipo)

// Remover un estado específico manualmente
bool RemoverEstado(StatusFlag tipo)

// Limpiar todos los estados (fin de combate, muerte, etc.)
void LimpiarTodosLosEstados()

// Procesar al inicio del turno: reduce duración, retorna daño total
int ProcesarInicioTurno()

// Retorna multiplicador de velocidad (Congelado = 0, resto acumulativo)
float ObtenerModificadorVelocidad()
```

### Comportamiento al Aplicar Estado Existente

Si se intenta aplicar un estado que ya está activo:
- La **duración** se reemplaza solo si la nueva es **mayor**
- El **daño por turno** se reemplaza solo si el nuevo es **mayor**

---

## Flujo de Estados

### Aplicar Estado

```
StatusEffect.Aplicar()
        │
        ▼
Entidad.AplicarEstado(tipo, duracion, dano, modificador)
        │
        ▼
GestorEstados.AplicarEstado()
        │
        ├── ¿Ya existe ese tipo?
        │       ├── Sí → Actualizar duración/daño si el nuevo es mayor
        │       └── No → Crear EstadoActivo, disparar OnEstadoAplicado
        │
        ▼
Debug.Log("[Estado]: X tiene Envenenado x3 turnos")
```

### Procesar Inicio de Turno

```
CombateManager → entidad.gestorEstados.ProcesarInicioTurno()
                        │
                        ▼
                GestorEstados.ProcesarInicioTurno()
                        │
                        ├── Por cada EstadoActivo:
                        │       ├── estado.ProcesarTurno() → retorna danoPorTurno
                        │       ├── turnosRestantes--
                        │       └── Si HaExpirado → marcar para remover
                        │
                        ├── Remover los expirados (dispara OnEstadoExpirado)
                        │
                        └── Retornar dañoTotal
                                │
                                ▼
                        Entidad aplica daño recibido
                                │
                                ▼
                        Consultar EstaIncapacitado
```

---

## Uso en Combate

### En CombateManager

```csharp
private void EjecutarTurno(Entidad entidad)
{
    // Procesar estados al inicio del turno
    int danoEstados = entidad.gestorEstados.ProcesarInicioTurno();
    if (danoEstados > 0)
        entidad.RecibirDanoPuro(danoEstados, ElementAttribute.None);

    // Verificar si puede actuar
    if (entidad.gestorEstados.EstaIncapacitado)
    {
        Debug.Log(entidad.Nombre_Entidad + " está incapacitado!");
        return;
    }

    // Continuar con el turno normal...
}
```

### Desde StatusEffect (Habilidades)

```csharp
// Envenenar: 5 daño por turno, 3 turnos
objetivo.AplicarEstado(StatusFlag.Envenenado, 3, 5, 0);

// Aturdir 1 turno (sin daño)
objetivo.AplicarEstado(StatusFlag.Aturdido, 1, 0, 0);

// Congelar 2 turnos con -30% velocidad
objetivo.AplicarEstado(StatusFlag.Congelado, 2, 0, 0.3f);

// Quemar: 8 daño por turno, 3 turnos
objetivo.AplicarEstado(StatusFlag.Quemado, 3, 8, 0);
```

---

## Tabla de Estados

| Estado | Impide Actuar | Daño/Turno | modificadorStats | Descripción |
|--------|:---:|:---:|:---:|-------------|
| `Envenenado` | No | Sí | No | Daño por veneno cada turno |
| `Quemado` | No | Sí | No | Daño por fuego cada turno |
| `Aturdido` | **Sí** | No | No | Incapacitado por impacto |
| `Congelado` | **Sí** | No | Sí (vel.) | Incapacitado, reduce velocidad vía `ObtenerModificadorVelocidad()` |

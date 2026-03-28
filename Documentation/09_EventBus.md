# Sistema de Eventos (EventBus)

> Comunicación desacoplada entre sistemas mediante publicación/suscripción de eventos tipados.
> Para eventos de misiones ver [22_Misiones.md](22_Misiones.md).
> Para eventos de combate en detalle ver [17_Sistema_Combate.md](17_Sistema_Combate.md).

## Índice

- [Archivos Asociados](#archivos-asociados)
- [Visión General](#visión-general)
- [EventBus (Core)](#eventbus-core)
- [Interfaz IEvento](#interfaz-ievento)
- [Eventos de Entidad](#eventos-de-entidad)
- [Eventos de Combate](#eventos-de-combate)
- [Eventos de Encounter / Detección](#eventos-de-encounter--detección)
- [Eventos de Party y Refuerzos](#eventos-de-party-y-refuerzos)
- [Eventos de Progresión](#eventos-de-progresión)
- [Eventos de Enemigos](#eventos-de-enemigos)
- [Eventos de UI](#eventos-de-ui)
- [Eventos de Travel](#eventos-de-travel)
- [Eventos de GameFlow](#eventos-de-gameflow)
- [Eventos de Misiones](#eventos-de-misiones)
- [Uso Básico](#uso-básico)
- [Publicación Diferida](#publicación-diferida)
- [Buenas Prácticas](#buenas-prácticas)
- [Diagrama de Flujo](#diagrama-de-flujo)
- [Limpiar al Cambiar Escena](#limpiar-al-cambiar-escena)

---

## Archivos Asociados

| Archivo | Descripción |
|---------|-------------|
| [Assets/Scripts/Managers/EventBus.cs](../Assets/Scripts/Managers/EventBus.cs) | Core estático del sistema de eventos |
| [Assets/Scripts/Events/IEvento.cs](../Assets/Scripts/Events/IEvento.cs) | Interfaz marcador para todos los eventos |
| [Assets/Scripts/Events/EventosEntidad.cs](../Assets/Scripts/Events/EventosEntidad.cs) | Eventos de entidad: daño, curación, muerte, estados |
| [Assets/Scripts/Events/EventosCombate.cs](../Assets/Scripts/Events/EventosCombate.cs) | Eventos de combate: turnos, habilidades, acciones UI |
| [Assets/Scripts/Events/EventosEncounter.cs](../Assets/Scripts/Events/EventosEncounter.cs) | Eventos de detección y encuentros de combate |
| [Assets/Scripts/Events/EventosParty.cs](../Assets/Scripts/Events/EventosParty.cs) | Eventos de party y refuerzos |
| [Assets/Scripts/Events/EventosProgresion.cs](../Assets/Scripts/Events/EventosProgresion.cs) | Eventos de nivel, XP, traits y evoluciones |
| [Assets/Scripts/Events/EventosEnemigo.cs](../Assets/Scripts/Events/EventosEnemigo.cs) | Eventos de spawn y derrota de enemigos |
| [Assets/Scripts/Events/EventosUI.cs](../Assets/Scripts/Events/EventosUI.cs) | Eventos de actualización de UI y mensajes |
| [Assets/Scripts/Events/EventosTravel.cs](../Assets/Scripts/Events/EventosTravel.cs) | Eventos de viaje y waypoints |
| [Assets/Scripts/Events/EventosGameFlow.cs](../Assets/Scripts/Events/EventosGameFlow.cs) | Eventos de cambio de estado del juego |
| [Assets/Scripts/Missions/EventosMision.cs](../Assets/Scripts/Missions/EventosMision.cs) | Eventos del sistema de misiones |

---

## Visión General

EventBus permite comunicación desacoplada entre sistemas. En lugar de que un sistema llame directamente a otro, publica eventos que cualquier interesado puede escuchar.

```
Publicador                          Suscriptores
    │                                    │
    │    EventBus.Publicar(evento)       │
    ├───────────────────────────────────►│ UI
    │                                    │ Audio
    │                                    │ Logros
    │                                    │ Analytics
```

Todos los eventos están en `namespace Managers` y deben implementar `IEvento`. La única excepción son los eventos de misiones, que están en `namespace Missions`.

---

## EventBus (Core)

**Archivo**: `Assets/Scripts/Managers/EventBus.cs`

Clase estática. Internamente usa un `Dictionary<Type, List<Delegate>>` de suscriptores y una `Queue<Action>` para publicación diferida.

### Métodos

| Método | Descripción |
|--------|-------------|
| `Suscribir<T>(Action<T>)` | Registrar un listener para eventos tipo T |
| `Desuscribir<T>(Action<T>)` | Remover un listener |
| `Publicar<T>(T evento)` | Enviar un evento a todos los suscriptores de inmediato |
| `PublicarDiferido<T>(T evento)` | Encolar el evento para publicarlo en el próximo `ProcesarCola()` |
| `ProcesarCola()` | Procesar todos los eventos encolados (llamar desde el game loop) |
| `LimpiarTodo()` | Remover todos los suscriptores y vaciar la cola |
| `Limpiar<T>()` | Limpiar solo los suscriptores del tipo T |
| `ObtenerCantidadSuscriptores<T>()` | Retorna cuántos listeners están registrados para T |

> **Restricción genérica**: todos los métodos requieren `where T : IEvento`.

---

## Interfaz IEvento

**Archivo**: `Assets/Scripts/Events/IEvento.cs`

```csharp
namespace Managers
{
    public interface IEvento { }
}
```

Interfaz marcador vacía. Todo struct/class de evento debe implementarla para poder usarse con el EventBus. Preferir **structs** para evitar allocations.

---

## Eventos de Entidad

**Archivo**: `Assets/Scripts/Events/EventosEntidad.cs`

```csharp
public struct EventoDanoRecibido : IEvento
{
    public IEntidadCombate Entidad;
    public int Cantidad;
    public ElementAttribute TipoDano;
    public IEntidadCombate Atacante;
}

public struct EventoCuracion : IEvento
{
    public IEntidadCombate Entidad;
    public int Cantidad;
}

public struct EventoMuerte : IEvento
{
    public IEntidadCombate Entidad;
    public IEntidadCombate Asesino;
}

public struct EventoEstadoAplicado : IEvento
{
    public IEntidadCombate Entidad;
    public StatusFlag Estado;
    public int Duracion;
}

public struct EventoEstadoRemovido : IEvento
{
    public IEntidadCombate Entidad;
    public StatusFlag Estado;
}
```

---

## Eventos de Combate

**Archivo**: `Assets/Scripts/Events/EventosCombate.cs`

### Flujo de combate

```csharp
public struct EventoCombateIniciado : IEvento
{
    public List<IEntidadCombate> Jugadores;
    public List<IEntidadCombate> Enemigos;
}

public struct EventoCombateFinalizado : IEvento
{
    public bool Victoria;
    public int XPGanada;
    public int OroGanado;
}

public struct EventoTurnoIniciado : IEvento
{
    public IEntidadCombate Entidad;
    public int NumeroTurno;
    public bool EsJugador;
}

public struct EventoTurnoFinalizado : IEvento { public IEntidadCombate Entidad; }
```

### Acciones del jugador (UI)

```csharp
public struct EventoEsperandoAccionJugador : IEvento
{
    public EntityController Entidad;
    public List<IEntidadCombate> Aliados;
    public List<IEntidadCombate> Enemigos;
}

public struct EventoAccionSeleccionada : IEvento
{
    public EntityController Entidad;
    public CombatActionType TipoAccion;
    public HabilidadData Habilidad;
}

public struct EventoObjetivoSeleccionado : IEvento
{
    public EntityController Atacante;
    public IEntidadCombate Objetivo;
    public HabilidadData Habilidad;
}

public struct EventoAccionCancelada : IEvento { public EntityController Entidad; }

public enum CombatActionType { Atacar, UsarItem, Defender, CederTurno, Huir }
```

### Habilidades y pasivas

```csharp
public struct EventoHabilidadUsada : IEvento
{
    public IEntidadCombate Invocador;
    public IEntidadCombate Objetivo;
    public HabilidadData Habilidad;
}

public struct EventoHabilidadDesbloqueada : IEvento { public IEntidadCombate Entidad; public HabilidadData Habilidad; }
public struct EventoHabilidadRemovida : IEvento { public IEntidadCombate Entidad; public HabilidadData Habilidad; }
public struct EventoPasivaDesbloqueada : IEvento { public IEntidadCombate Entidad; public PasivaData Pasiva; }
public struct EventoPasivaRemovida : IEvento { public IEntidadCombate Entidad; public PasivaData Pasiva; }
```

---

## Eventos de Encounter / Detección

**Archivo**: `Assets/Scripts/Events/EventosEncounter.cs`

```csharp
public struct EventoCandidatoDetectado : IEvento { public ICombatCandidate Candidato; public bool EnRangoEngagement; }
public struct EventoCandidatoFueraDeRango : IEvento { public ICombatCandidate Candidato; }
public struct EventoCandidatoEnRangoCombate : IEvento { public ICombatCandidate Candidato; }
public struct EventoCandidatoSalioRangoCombate : IEvento { public ICombatCandidate Candidato; }

public struct EventoEncounterIniciado : IEvento
{
    public List<EntityController> Party;
    public List<EnemyController> Enemigos;
}

public struct EventoEnemigosAgregados : IEvento { public List<EnemyController> NuevosEnemigos; }
```

---

## Eventos de Party y Refuerzos

**Archivo**: `Assets/Scripts/Events/EventosParty.cs`

### Gestión de party

```csharp
public struct EventoPersonajeRegistrado : IEvento { public EntityController Personaje; }
public struct EventoMainCambiado : IEvento { public EntityController MainAnterior; public EntityController NuevoMain; }
public struct EventoPersonajeUnidoParty : IEvento { public EntityController Personaje; public int TamanoPartyActual; }
public struct EventoPersonajeSalioParty : IEvento { public EntityController Personaje; public bool FueEstacionado; }

public struct EventoPersonajeEstacionado : IEvento
{
    public EntityController Personaje;
    public Vector3 Ubicacion;
    public string NombreUbicacion;
}
```

### Refuerzos

```csharp
public struct EventoRefuerzosSolicitados : IEvento
{
    public List<EntityController> RefuerzosDisponibles;
    public List<EntityController> Refuerzos;
    public int CantidadSolicitada;
    public Vector3 PosicionCombate;
}

public struct EventoRefuerzoProgramado : IEvento
{
    public EntityController Refuerzo;
    public EntityController Personaje;
    public int TurnoLlegada;
    public int TurnosRestantes;
    public float Distancia;
}

public struct EventoRefuerzoLlegado : IEvento { public EntityController Refuerzo; public EntityController Personaje; public int TurnoLlegada; }
public struct EventoRefuerzosCancelados : IEvento { public List<EntityController> RefuerzosCancelados; }
```

---

## Eventos de Progresión

**Archivo**: `Assets/Scripts/Events/EventosProgresion.cs`

```csharp
public struct EventoNivelSubido : IEvento { public IEntidadCombate Entidad; public int NuevoNivel; }

public struct EventoXPGanada : IEvento
{
    public IEntidadCombate Entidad;
    public float Cantidad;
    public float Total;
    public float Necesaria;
}

public struct EventoTraitObtenido : IEvento
{
    public string TraitId;
    public string CharacterId;
    public int StacksActuales;
    public bool EsGlobalmenteUnico;
}

public struct EventoEvolucionAplicada : IEvento
{
    public string EvolucionId;
    public string CharacterId;
}
```

---

## Eventos de Enemigos

**Archivo**: `Assets/Scripts/Events/EventosEnemigo.cs`

```csharp
public struct EventoEnemigoDerrotado : IEvento
{
    public string IDInstanciaEnemigo;
    public TipoEntidades TipoEnemigo;
    public string NombreEnemigo;
    public int NivelEnemigo;
    public float XPOtorgada;
    public Vector3 PosicionMuerte;
    public IEntidadCombate Asesino;
    public float Timestamp;
}

public struct EventoEnemigoSpawneado : IEvento
{
    public string IDInstanciaEnemigo;
    public TipoEntidades TipoEnemigo;
    public Vector3 Posicion;
}
```

---

## Eventos de UI

**Archivo**: `Assets/Scripts/Events/EventosUI.cs`

```csharp
public struct EventoMostrarMensaje : IEvento { public string Mensaje; public float Duracion; public Color? ColorTexto; }
public struct EventoActualizarUI : IEvento { public string PanelId; }
```

---

## Eventos de Travel

**Archivo**: `Assets/Scripts/Events/EventosTravel.cs`

```csharp
public struct EventoTravelSolicitado : IEvento { public TravelRequest Request; }
public struct EventoTravelIniciado : IEvento { public TravelRequest Request; }
public struct EventoTravelCompletado : IEvento { public TravelRequest Request; public Vector3 PosicionFinal; }
public struct EventoTravelCancelado : IEvento { public TravelRequest Request; public string Razon; }
public struct EventoWaypointDesbloqueado : IEvento { public string WaypointId; public string NombreWaypoint; }
```

---

## Eventos de GameFlow

**Archivo**: `Assets/Scripts/Events/EventosGameFlow.cs`

```csharp
public struct EventoGameFlowChanged : IEvento
{
    public IGameFlowState NuevoEstado;
    public string TipoEstado;
}
```

---

## Eventos de Misiones

**Archivo**: `Assets/Scripts/Missions/EventosMision.cs` — `namespace Missions`

> Ver [22_Misiones.md](22_Misiones.md) para detalles del sistema de misiones.

```csharp
public struct EventoMisionDisponible : IEvento { public MissionDefinitionSO Mision; }
public struct EventoMisionAceptada : IEvento { public MissionInstance Instancia; }

public struct EventoMisionProgreso : IEvento
{
    public MissionInstance Instancia;
    public int IndiceObjetivo;
    public float ProgresoAnterior;
    public float ProgresoNuevo;
}

public struct EventoObjetivoCompletado : IEvento { public MissionInstance Instancia; public int IndiceObjetivo; }
public struct EventoMisionCompletada : IEvento { public MissionInstance Instancia; public MissionRewards Recompensas; }
public struct EventoMisionFallida : IEvento { public MissionInstance Instancia; public string Razon; }
```

---

## Uso Básico

### Suscribirse y desuscribirse

```csharp
public class UIVida : MonoBehaviour
{
    void OnEnable()
    {
        EventBus.Suscribir<EventoDanoRecibido>(OnDano);
        EventBus.Suscribir<EventoCuracion>(OnCuracion);
        EventBus.Suscribir<EventoMuerte>(OnMuerte);
    }

    void OnDisable()
    {
        EventBus.Desuscribir<EventoDanoRecibido>(OnDano);
        EventBus.Desuscribir<EventoCuracion>(OnCuracion);
        EventBus.Desuscribir<EventoMuerte>(OnMuerte);
    }

    private void OnDano(EventoDanoRecibido e)
    {
        float porcentaje = (float)e.Entidad.Stats.VidaActual / e.Entidad.Stats.VidaMaxima;
        barraVida.fillAmount = porcentaje;
    }

    private void OnCuracion(EventoCuracion e) { /* actualizar barra */ }
    private void OnMuerte(EventoMuerte e) { panelMuerte.SetActive(true); }
}
```

### Publicar un evento

```csharp
// Al recibir daño
EventBus.Publicar(new EventoDanoRecibido
{
    Entidad = this,
    Cantidad = danioFinal,
    TipoDano = elemento,
    Atacante = atacante
});

// Al morir
EventBus.Publicar(new EventoMuerte { Entidad = this, Asesino = atacante });
```

---

## Publicación Diferida

`PublicarDiferido` encola el evento en lugar de enviarlo de inmediato. Útil para evitar modificar colecciones mientras se itera, o para diferir efectos a final de frame.

```csharp
// Encolar para procesar después
EventBus.PublicarDiferido(new EventoEnemigoDerrotado { ... });

// Procesar toda la cola (llamar desde un manager central, ej. en LateUpdate)
EventBus.ProcesarCola();
```

---

## Buenas Prácticas

### ✅ Hacer

```csharp
// Siempre desuscribirse en OnDisable para evitar memory leaks
void OnDisable()
{
    EventBus.Desuscribir<EventoDanoRecibido>(OnDano);
}

// Usar structs para eventos (sin GC allocation)
public struct MiEvento : IEvento { ... }

// Usar Limpiar<T>() si solo querés resetear un tipo específico
EventBus.Limpiar<EventoCombateIniciado>();
```

### ❌ Evitar

```csharp
// NO olvidar desuscribirse (el objeto nunca se libera)
void OnEnable()
{
    EventBus.Suscribir<EventoDanoRecibido>(OnDano);
    // falta el OnDisable correspondiente
}

// NO usar clases para eventos cuando un struct alcanza
public class MiEvento : IEvento { ... } // genera GC

// NO modificar estado crítico del juego en múltiples handlers del mismo evento
// puede causar orden de ejecución impredecible
```

---

## Diagrama de Flujo

```
Jugador usa Ataque Pesado
        │
        ▼
DamageEffect.Aplicar()
        │
        ▼
Entidad.RecibirDano(50)
        │
        ├─────────────────────────────────┐
        ▼                                 ▼
EventBus.Publicar(EventoDanoRecibido)  EventBus.Publicar(EventoMuerte)
        │                                 │
        ├──────────┐                      ├──────────┐
        ▼          ▼                      ▼          ▼
   AudioManager  UIPopups           LogrosManager  CombatManager
        │          │                      │          │
        ▼          ▼                      ▼          ▼
   PlaySound   ShowNumber          CheckLogro   TerminarCombate
```

---

## Limpiar al Cambiar Escena

```csharp
public class GameManager : MonoBehaviour
{
    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EventBus.LimpiarTodo();
    }
}
```

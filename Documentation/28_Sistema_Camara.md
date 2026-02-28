# Sistema de Cámara Dual

## Visión General

El sistema de cámara de Saclisam soporta **dos modos**: isométrico y tercera persona. El modo activo depende del contexto:

- **En combate** → siempre Isométrico (forzado automáticamente).
- **Fuera de combate** → el jugador puede alternar libremente entre ambos modos con **Tab**.

La transición entre modos es suavizada mediante interpolación `smoothstep`.

```
┌──────────────────────────────────────────────────────────────┐
│                  SISTEMA DE CÁMARA DUAL                      │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │             IsometricCameraController               │    │
│  │                                                     │    │
│  │   currentMode ─► Isometric  │  ThirdPerson          │    │
│  │                             │                       │    │
│  │   HandleIsoInput()          │  HandleTpInput()      │    │
│  │   UpdateIsoPosition()       │  UpdateTpPosition()   │    │
│  │   GetMovementForward/Right  ◄► GameInputManager     │    │
│  └──────────────────┬──────────────────────────────────┘    │
│                     │                                        │
│         ┌───────────┴────────────┐                          │
│         ▼                        ▼                          │
│  EventoCombateIniciado   EventoCombateFinalizado             │
│  (fuerza Isometric)      (restaura modo por defecto)         │
└──────────────────────────────────────────────────────────────┘
```

---

## Archivos del Sistema

| Archivo | Responsabilidad |
|---|---|
| `Scripts/Camera/IsometricCameraController.cs` | Controlador principal (lógica de cámara y movimiento) |
| `Scripts/Camera/CameraSettings.cs` | ScriptableObject con todos los parámetros configurables |
| `Scripts/Input/GameInputManager.cs` | Usa `GetMovementForward/Right` para traducir WASD a mundo 3D |

---

## CameraSettings (ScriptableObject)

**Menú**: `Assets/Create/Saclisam/Camera Settings`

### Modo General

| Campo | Tipo | Descripción |
|---|---|---|
| `defaultMode` | `CameraMode` | Modo al iniciar fuera de combate (`Isometric` o `ThirdPerson`) |
| `toggleModeKey` | `KeyCode` | Tecla para alternar modos fuera de combate (por defecto: **Tab**) |
| `modeTransitionDuration` | `float` | Duración en segundos de la transición suave (por defecto: `0.4s`) |

### Isométrica

| Campo | Por Defecto | Descripción |
|---|---|---|
| `pitchAngle` | `45°` | Inclinación vertical de la cámara |
| `initialYawAngle` | `45°` | Rotación horizontal inicial |
| `minZoomDistance` | `5` | Zoom mínimo |
| `maxZoomDistance` | `20` | Zoom máximo |
| `defaultZoomDistance` | `12` | Zoom inicial |
| `zoomSpeed` | `5` | Velocidad del zoom |
| `zoomSmoothing` | `10` | Suavizado del zoom y la rotación |
| `allowRotation` | `true` | Habilita Q/E y clic derecho para rotar |
| `rotationSpeed` | `90°/s` | Velocidad de rotación con Q/E |
| `mouseRotation` | `true` | Rotación con clic derecho + arrastrar |
| `mouseRotationSensitivity` | `2` | Sensibilidad del mouse |
| `followSmoothing` | `8` | Suavizado del seguimiento al personaje |
| `targetHeightOffset` | `1.5` | Altura del punto de enfoque sobre el personaje |
| `useBounds` | `false` | Limitar área de la cámara al mapa |
| `boundsMin / boundsMax` | — | Límites XZ del área permitida |

### Tercera Persona

| Campo | Por Defecto | Descripción |
|---|---|---|
| `tpDistance` | `6` | Distancia horizontal inicial |
| `tpMinDistance` | `2` | Zoom mínimo |
| `tpMaxDistance` | `14` | Zoom máximo |
| `tpHeight` | `2.5` | Altura del punto de posición de la cámara (afecta pitch visual) |
| `tpPitchAngle` | `18°` | Ángulo de depresión fijo de la cámara hacia el personaje |
| `tpTargetHeightOffset` | `1.5` | Altura del punto de enfoque sobre el personaje |
| `tpRotationSpeed` | `90°/s` | Velocidad de rotación orbital con Q/E |
| `tpMouseRotation` | `true` | Rotación orbital con clic derecho + arrastrar |
| `tpMouseRotationSensitivity` | `2.5` | Sensibilidad del mouse en TP |
| `tpSnapBehindOnEnter` | `true` | Al entrar en TP, alinear cámara detrás del personaje |
| `tpFollowSmoothing` | `10` | Suavizado del seguimiento en TP |
| `tpZoomSpeed` | `4` | Velocidad del zoom en TP |
| `tpZoomSmoothing` | `10` | Suavizado del zoom en TP |

---

## IsometricCameraController

### Cambio de modo

```csharp
// Desde código, cambio con transición suave:
IsometricCameraController.Instance.SetMode(CameraMode.ThirdPerson, smooth: true);

// Desde código, cambio instantáneo:
IsometricCameraController.Instance.SetMode(CameraMode.Isometric, smooth: false);
```

Reglas de `SetMode`:
- Si `inCombat == true` y se intenta cambiar a `ThirdPerson`, la llamada es ignorada con un `LogWarning`.
- El combate se detecta automáticamente via `EventoCombateIniciado` / `EventoCombateFinalizado`.

### Propiedades públicas

| Propiedad/Método | Descripción |
|---|---|
| `CurrentMode` | Modo activo actual |
| `InCombat` | Si hay un combate en progreso |
| `CurrentTarget` | Transform que sigue la cámara |
| `CurrentYaw` | Yaw crudo del modo activo (en grados) |
| `GetMovementForward()` | Vector XZ "adelante" correcto para WASD según modo |
| `GetMovementRight()` | Vector XZ "derecha" correcto para WASD según modo |

### Métodos de ajuste programático

```csharp
cam.SetTarget(transform);          // Cambiar objetivo
cam.SetIsoZoom(10f);               // Zoom isométrico
cam.SetTpZoom(5f, instant: true);  // Zoom TP instantáneo
cam.SetRotation(90f);              // Rotar modo activo
cam.ResetCamera();                 // Restaurar valores del SO
cam.SnapToTarget();                // Teletransportar sin suavizado
```

---

## Movimiento WASD relativo a la cámara

El `GameInputManager` llama a `GetMovementForward()` y `GetMovementRight()` en cada frame de exploración para calcular el `WorldDirection` del input:

```csharp
Vector3 forward = cam.GetMovementForward();
Vector3 right   = cam.GetMovementRight();
input.WorldDirection = (right * input.Direction.x + forward * input.Direction.y).normalized;
```

### Diferencias entre modos

| Modo | W mueve hacia... | D mueve hacia... |
|---|---|---|
| **Isométrico** | Convención isométrica clásica (relativo a posición XZ de la cámara) | Derecha isométrica |
| **Tercera Persona** | Dirección en que la cámara **mira** (entrando en la pantalla) | Derecha real de la cámara |

#### Derivación matemática (TP)

La cámara en TP se posiciona en:

```
cameraPos = focusPoint + (sin(yaw)·cos(pitch), sin(pitch), cos(yaw)·cos(pitch)) · distance
```

Por tanto, el vector forward de la cámara (XZ, hacia el personaje) es:

```
forward_TP = (-sin(yaw), 0, -cos(yaw))
right_TP   = cross(forward, up) = (cos(yaw), 0, -sin(yaw))
```

Con yaw=0° (cámara en +Z): `forward=(-0,0,-1)` → W mueve en -Z (hacia dentro de la pantalla) ✓

---

## Controles en juego

| Acción | Isométrico | Tercera Persona |
|---|---|---|
| Mover personaje | WASD (relativo a iso) | WASD (relativo a TP) |
| Rotar cámara | Q / E · clic derecho | Q / E · clic derecho |
| Zoom | Rueda del mouse | Rueda del mouse |
| Alternar modo | **Tab** | **Tab** |

> **Nota**: Tab solo funciona **fuera de combate**. En combate, la cámara siempre es isométrica.

---

## Integración con otros sistemas

### EventBus

El controlador se suscribe automáticamente a:

```csharp
EventBus.Suscribir<EventoCombateIniciado>(OnCombateIniciado);
EventBus.Suscribir<EventoCombateFinalizado>(OnCombateFinalizado);
```

No hace falta ninguna configuración manual para la transición de cámara durante el combate.

### PlayerPartyManager

La cámara sigue al `MainTransform` del party y escucha `OnMainChanged` para cambiar de objetivo automáticamente cuando el main del party cambia.

```csharp
// Cuando cambia el main:
private void OnMainCharacterChanged(EntityController oldMain, EntityController newMain)
{
    currentTarget = newMain.transform;
}
```

### CombatEncounterManager / CombateManager

No requieren referencia directa a la cámara. La comunicación es exclusivamente por EventBus.

---

## Setup en Unity

1. Crear el GameObject de cámara (puede ser el mismo `Main Camera`).
2. Añadir el componente `IsometricCameraController`.
3. Crear el asset `CameraSettings` (`Assets/Create/Saclisam/Camera Settings`).
4. Asignar el asset al campo **Configuracion** del componente.
5. *(Opcional)* Asignar un **Objetivo Manual** si no hay `PlayerPartyManager` en escena.

### Parámetros recomendados por tipo de cámara

**Isométrica clásica**:
- `pitchAngle = 45`, `initialYawAngle = 45`, `defaultZoomDistance = 12`

**Tercera persona cercana** (acción):
- `tpDistance = 5`, `tpPitchAngle = 15`, `tpHeight = 2`

**Tercera persona lejana** (exploración):
- `tpDistance = 8`, `tpPitchAngle = 20`, `tpHeight = 3`

---

## Gizmos (Editor)

Con `showDebugGizmos = true` en el Inspector se visualizan:

- **Línea amarilla**: vector cámara → objetivo.
- **Esfera cian** (ISO) / **Esfera verde** (TP): punto de enfoque del personaje.
- **Etiqueta**: modo activo y si está en combate.
- **Cubo rojo** (solo ISO con `useBounds`): límites del área permitida.

# 31 – Recuperación de Escena y Setup Inicial

> **Cuándo usar esta guía:** perdiste la escena de Unity (corrupción, pérdida de datos, reset del proyecto) y necesitas volver a montarla desde cero sin recordar qué iba dónde.

---

## Herramientas disponibles

Hay dos ventanas de editor que automatizan todo:

| Menú | Uso |
|---|---|
| **Tools → Setup Game Scene** | Crea y verifica todos los managers, la cámara y el player |
| **Tools → Setup Player and Camera** | Herramienta especializada solo en cámara + player |

Para la recuperación completa usa **Tools → Setup Game Scene → ⚡ SETUP COMPLETO**.

---

## 1. GameObjects que deben estar en la escena

El botón **"3. Setup Managers en Escena"** los crea automáticamente si faltan. Nunca duplica: comprueba con `FindFirstObjectByType` antes de crear.

| GameObject | Componente principal | Notas de configuración |
|---|---|---|
| `WorldChunkManager` | `WorldChunkManager` | Asignar **Player Transform** en el Inspector |
| `DynamicEnemyPoolManager` | `DynamicEnemyPoolManager` | Asignar **Enemy Prefab** |
| `PlayerPartyManager` | `PlayerPartyManager` | El `PlayerInitializer` del player lo configura en runtime |
| `CombatEncounterManager` | `CombatEncounterManager` | Sin refs manuales obligatorias |
| `CombateManager` | `CombateManager` | Sin refs manuales obligatorias |
| `GameFlowController` | `GameFlowController` | Singleton; inicia en estado `ExplorationFlowState` |
| `GameInputManager` | `GameInputManager` | Asignar las **LayerMask**: Ground, Entity, Enemy |
| `MainCamera` | `IsometricCameraController` + `Camera` + `AudioListener` | Ver sección 3 |

---

## 2. Prefab del Player – estructura de componentes

El botón **"5. Verificar Player Setup"** diagnostica todo esto en tiempo de editor.

### Root del prefab

| Componente | Obligatorio | Notas |
|---|---|---|
| `EntityController` | ✅ | `ClaseData` asignado + `isPlayerOwned = true` |
| `EntityStats` | ✅ | Se auto-crea en Awake si falta, pero mejor ponerlo manualmente. Arrastrar la ref al `EntityController` |
| `NavMeshAgent` | ✅ | `[RequireComponent]` de `PlayerMovementController`. Sin él no compila |
| `PlayerMovementController` | ✅ | Sin este componente no hay WASD ni click-to-move |
| `PlayerInitializer` | ✅ | Registra el player en `PlayerPartyManager` como main en Start |
| `CapsuleCollider` | ✅ | Para colisiones físicas con el mundo |
| `Animator` | ⚠️ opcional | Espera los parámetros `Speed` (float) e `IsMoving` (bool) |

### Hijo: `InterestZone`

| Componente | Obligatorio | Notas |
|---|---|---|
| `PlayerInterestZone` | ✅ | Detecta candidatos a combate; notifica al `CombatEncounterManager` |
| `SphereCollider` | ✅ | **`IsTrigger = true`**; radio ≈ `CombatRules.detectionRadius` (~20 u.) |

### Checklist del Inspector

```
EntityController
  ├─ Datos Clase        → [ClaseData asset]        ← si es null la entidad no existe
  ├─ Is Player Owned    → ✅ true                  ← si es false no se puede controlar
  └─ Entity Stats       → [EntityStats del root]

PlayerInitializer
  ├─ Auto Register As Main  → ✅ true
  └─ Register In Chunk Manager → ✅ true
```

---

## 3. Cámara – por qué puede quedar rota

### Causa raíz

`IsometricCameraController.LateUpdate()` tiene esta guarda:

```csharp
if (currentTarget == null || settings == null) return;
```

Si el campo `settings` (`CameraSettings` ScriptableObject) no está asignado, la cámara **no se mueve ni un frame** aunque el componente esté activo. Esto pasa cuando se añade el componente a mano sin asignar el asset.

### Solución

El botón **"4. Setup Cámara"** (o el SETUP COMPLETO) hace tres cosas en orden:

1. Crea `Assets/Resources/CameraSettings.asset` si no existe.
2. Crea el GameObject `MainCamera` con `Camera` + `AudioListener` + `IsometricCameraController` (o reutiliza la Main Camera existente).
3. **Asigna el asset al campo `settings`** via `SerializedObject` — este paso es el crítico.

### Configuración del asset CameraSettings

Crear manualmente: clic derecho en Project → **Create → Saclisam → Camera Settings** → mover a `Assets/Resources/CameraSettings.asset`.

| Campo importante | Valor por defecto | Qué controla |
|---|---|---|
| `Default Mode` | `ThirdPerson` | Modo al iniciar |
| `Toggle Mode Key` | `Tab` | Alterna Isométrico ↔ Tercera persona |
| `Pitch Angle` | `45°` | Inclinación en modo isométrico |
| `Default Zoom Distance` | `12` | Distancia inicial al player |
| `Follow Smoothing` | `8` | Suavizado del seguimiento |

---

## 4. Assets en Resources que deben existir

Estos se cargan con `Resources.Load<T>("nombre")`. Si no existen el juego lanza warnings o usa valores por defecto.

| Archivo | Tipo | Cómo crearlo |
|---|---|---|
| `Assets/Resources/CombatRules.asset` | `CombatRules` SO | Tools → Setup Game Scene → "1. Verificar/Crear CombatRules" |
| `Assets/Resources/GameConfig.asset` | `GameConfig` SO | Clic derecho → Create → Combate → Game Config |
| `Assets/Resources/CameraSettings.asset` | `CameraSettings` SO | Tools → Setup Game Scene → "4. Setup Cámara" |
| `Assets/Resources/CombatConfig.asset` | `CombatConfig` SO | Clic derecho → Create → Combate → CombatConfig |

---

## 5. CombatConfig – referencia rápida

ScriptableObject singleton que centraliza los parámetros de la fórmula de daño.

| Campo | Default | Qué controla |
|---|---|---|
| `defenseConstantK` | `5` | Constante K de la fórmula de defensa. Más bajo = defensa más fuerte |
| `minElementalMultiplier` | `0.1` | Multiplicador mínimo cuando el defensor tiene alta resistencia elemental |
| `maxElementalMultiplier` | `1.5` | Multiplicador máximo cuando el defensor es vulnerable |
| `baseCritChance` | `0.05` | Chance base de crítico (5 %) para todas las entidades |
| `baseCritMultiplier` | `1.5` | Daño de crítico × 1.5 por defecto |
| `minimumDamage` | `1` | Daño mínimo garantizado (ningún ataque hace 0) |
| `raceModifiers` | null | Asset opcional con modificadores por raza |
| `debugDamageCalculation` | false | Imprime desglose completo del daño en Consola |

---

## 6. Orden de setup recomendado tras pérdida de datos

```
1. Abrir la escena vacía (o la escena principal)
2. Tools → Setup Game Scene → ⚡ SETUP COMPLETO
   └─ Crea: CombatRules, managers, GameFlowController,
            GameInputManager, cámara con CameraSettings
3. Verificar en el Inspector:
   └─ GameInputManager   → asignar Ground/Entity/Enemy layers
   └─ WorldChunkManager  → asignar Player Transform
   └─ DynamicEnemyPoolManager → asignar Enemy Prefab
4. Arrastrar el prefab del player a la escena
5. Tools → Setup Game Scene → "5. Verificar Player Setup"
   └─ Corregir los ❌ que aparezcan
6. Si GameConfig no existe crearlo y moverlo a Resources/
7. Window → AI → Navigation → Bake (NavMesh del nivel)
8. Play ▶
```

---

## 7. Diagnóstico rápido de problemas comunes

| Síntoma | Causa probable | Solución |
|---|---|---|
| El PJ no se puede controlar | `isPlayerOwned = false` o `GameInputManager` no existe en escena | Activar flag / ejecutar Setup Managers |
| El PJ no se mueve (WASD no funciona) | `PlayerMovementController` no está en el prefab | Añadir componente al root del prefab |
| Cámara estática, no sigue al player | `CameraSettings` no asignado en `IsometricCameraController` | Tools → "4. Setup Cámara" |
| Cámara sigue pero está en posición rara | `PlayerPartyManager.MainCharacter` es null | Verificar que `PlayerInitializer.autoRegisterAsMain = true` |
| `EntidadLogica` null en runtime | `ClaseData` no asignado en `EntityController` | Asignar el asset de clase en el Inspector |
| Enemigos no detectados | `SphereCollider` del hijo no es trigger o `CombatRules` no existe | Activar IsTrigger / crear CombatRules |
| Logs de `CombatConfig` usando defaults | `Assets/Resources/CombatConfig.asset` no existe | Crear y mover a Resources/ |

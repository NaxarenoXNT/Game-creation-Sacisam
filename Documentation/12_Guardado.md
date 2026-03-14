# Sistema de Guardado

## Visión General

El sistema de guardado persiste el progreso del juego usando serialización JSON. Soporta múltiples slots, auto-guardado, y estructura **per-personaje** para soportar el sistema multi-character.

```
SaveSystem
    │
    ├── Guardar(slot) → SaveData → JSON → Archivo
    │
    └── Cargar(slot) → Archivo → JSON → SaveData
```

### Estructura de Datos

```
SaveData
├── Metadatos (versión, fecha, tiempo jugado)
├── GlobalPlayerState
│   ├── misionesGlobalesCompletadas
│   ├── traitsGlobalmenteBloqueados (traitId → characterId)
│   ├── misionesExclusivasAsignadas (misionId → characterId)
│   └── flagsGlobales
├── MissionSaveData
│   ├── globalesCompletadas / globalesFallidas / globalesActivas
│   ├── exclusivasAsignadas
│   └── datosPersonajes[] (per-character mission states)
├── PersonajesSaveData[] (per-character)
│   ├── characterId
│   ├── claseId
│   ├── EvolutionState completo
│   ├── posición en el mundo
│   └── estado (main/party/stationed)
└── Configuración (volumen, etc.)
```

---

## SaveData (Estructura Principal)

```csharp
[System.Serializable]
public class SaveData
{
    // Metadatos
    public string version = "2.0";
    public System.DateTime fechaGuardado;
    public float tiempoJugado;

    // Estado global del jugador (compartido entre personajes)
    public GlobalPlayerState globalState;

    // Datos de misiones (global + per-character)
    public MissionSaveData missionData;

    // Per-personaje
    public List<PersonajeSaveData> personajes;

    // Party state
    public string mainCharacterId;
    public List<string> activePartyIds;

    // Configuración
    public float volumenMusica;
    public float volumenEfectos;
}
```

### PersonajeSaveData (per-character)

```csharp
[System.Serializable]
public class PersonajeSaveData
{
    public string characterId;
    public string claseId;
    public EvolutionState evolutionState;  // Estado completo serializable
    public Vector3Serializable posicion;
    public string escenaActual;
    public CharacterPartyStatus partyStatus;  // Main, ActiveParty, Stationed
}
```

### MissionSaveData

```csharp
[System.Serializable]
public class MissionSaveData
{
    // Estado global de misiones
    public List<string> globalesCompletadas;
    public List<string> globalesFallidas;
    public List<MissionExclusiveAssignment> exclusivasAsignadas;
    public List<MissionActiveSaveData> globalesActivas;

    // Per-personaje
    public List<CharacterMissionSaveData> datosPersonajes;
}
```

---

## Flujo de Guardado

```csharp
// 1. Recopilar estado global
var datos = new SaveData();
datos.globalState = globalPlayerState;

// 2. Recopilar misiones
datos.missionData = missionManager.ObtenerDatosGuardado();

// 3. Recopilar per-personaje
foreach (var character in partyManager.AllOwnedCharacters)
{
    datos.personajes.Add(new PersonajeSaveData
    {
        characterId = character.CharacterId,
        claseId = character.DatosClase.name,
        evolutionState = evolutionController.GetState(character.CharacterId),
        posicion = new Vector3Serializable(character.transform.position),
        // ...
    });
}

// 4. Guardar
SaveSystem.Instance.Guardar(slot, datos);
```

## Flujo de Carga

```csharp
// 1. Cargar datos
SaveData datos = SaveSystem.Instance.Cargar(slot);

// 2. Restaurar estado global
globalPlayerState = datos.globalState;

// 3. Restaurar personajes
foreach (var pjData in datos.personajes)
{
    // Instanciar personaje, restaurar EvolutionState, registrar en PartyManager
}

// 4. Restaurar misiones
var estados = /* mapear characterId → EvolutionState restaurados */;
missionManager.CargarDatosGuardado(datos.missionData, globalPlayerState, estados);
```

---

## Ubicación de Archivos

Los archivos se guardan en:

- **Windows**: `C:\Users\<Usuario>\AppData\LocalLow\<Company>\<Product>\saves\`
- **Mac**: `~/Library/Application Support/<Company>/<Product>/saves/`
- **Linux**: `~/.config/unity3d/<Company>/<Product>/saves/`

### Formato de Nombre

```
save_slot_0.json
save_slot_1.json
save_slot_2.json
```

---

## Implementación Interna

```csharp
public void Guardar(int slot, SaveData datos)
{
    string ruta = ObtenerRutaGuardado(slot);
    string json = JsonUtility.ToJson(datos, prettyPrint: true);
    System.IO.File.WriteAllText(ruta, json);
}

public SaveData Cargar(int slot)
{
    string ruta = ObtenerRutaGuardado(slot);
    if (!System.IO.File.Exists(ruta)) return null;
    string json = System.IO.File.ReadAllText(ruta);
    return JsonUtility.FromJson<SaveData>(json);
}
```

---

## Notas de Integración Multi-Personaje

- `EvolutionState` es serializable por diseño — cada campo es `[System.Serializable]`.
- `GlobalPlayerState` se guarda una sola vez (estado compartido).
- `MissionManager.ObtenerDatosGuardado()` empaqueta todo el estado de misiones.
- `MissionManager.CargarDatosGuardado()` restaura globales + per-personaje.
- Al cargar, cada personaje debe registrarse en `PlayerPartyManager` y `MissionManager`.

# Sistema de Guardado

## Visión General

El sistema de guardado permite persistir el progreso del juego usando serialización JSON. Soporta múltiples slots y auto-guardado.

```
SaveSystem
    │
    ├── Guardar(slot) → JSON → Archivo
    │
    └── Cargar(slot) → Archivo → JSON → SaveData
```

---

## SaveSystem (Singleton)

**Archivo**: `Assets/Scripts/Managers/SaveSystem.cs`

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | SaveSystem | Instancia singleton |
| `AutoSaveInterval` | float | Intervalo de auto-guardado (segundos) |

### Métodos Principales

```csharp
// Guardar en un slot específico
void Guardar(int slot, SaveData datos)

// Cargar desde un slot
SaveData Cargar(int slot)

// Verificar si existe guardado
bool ExisteGuardado(int slot)

// Eliminar un guardado
void EliminarGuardado(int slot)

// Obtener lista de slots disponibles
List<SaveSlotInfo> ObtenerSlots()
```

---

## SaveData

Clase que contiene todos los datos a guardar.

```csharp
[System.Serializable]
public class SaveData
{
    // Metadatos
    public string version = "1.0";
    public System.DateTime fechaGuardado;
    public float tiempoJugado;
    
    // Datos del jugador
    public JugadorSaveData jugador;
    
    // Progresión
    public int nivelActual;
    public List<string> objetivosCompletados;
    public List<string> itemsDesbloqueados;
    
    // Configuración
    public float volumenMusica;
    public float volumenEfectos;
}
```

### JugadorSaveData

```csharp
[System.Serializable]
public class JugadorSaveData
{
    public string nombre;
    public string claseId;  // ID del ClaseData
    public int nivel;
    public int experiencia;
    
    // Stats actuales
    public int vidaActual;
    public int manaActual;
    
    // Inventario (si tienes)
    public List<ItemSaveData> inventario;
    
    // Posición en el mundo
    public Vector3Serializable posicion;
    public string escenaActual;
}
```

### Vector3Serializable

Unity's Vector3 no es serializable por JSON, así que usamos una versión propia:

```csharp
[System.Serializable]
public class Vector3Serializable
{
    public float x, y, z;
    
    public Vector3Serializable() { }
    
    public Vector3Serializable(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
```

---

## Uso Básico

### Guardar Partida

```csharp
public void GuardarPartida(int slot)
{
    // Crear datos de guardado
    var datos = new SaveData
    {
        fechaGuardado = System.DateTime.Now,
        tiempoJugado = Time.timeSinceLevelLoad,
        nivelActual = SceneManager.GetActiveScene().buildIndex
    };
    
    // Datos del jugador
    datos.jugador = new JugadorSaveData
    {
        nombre = jugador.Nombre,
        claseId = jugador.ClaseData.name,
        nivel = jugador.Nivel,
        experiencia = jugador.Experiencia,
        vidaActual = jugador.Stats.VidaActual,
        posicion = new Vector3Serializable(jugador.transform.position),
        escenaActual = SceneManager.GetActiveScene().name
    };
    
    // Guardar
    SaveSystem.Instance.Guardar(slot, datos);
    
    Debug.Log($"Partida guardada en slot {slot}");
}
```

### Cargar Partida

```csharp
public void CargarPartida(int slot)
{
    if (!SaveSystem.Instance.ExisteGuardado(slot))
    {
        Debug.LogWarning($"No existe guardado en slot {slot}");
        return;
    }
    
    SaveData datos = SaveSystem.Instance.Cargar(slot);
    
    if (datos == null)
    {
        Debug.LogError("Error al cargar datos");
        return;
    }
    
    // Cargar escena
    SceneManager.LoadScene(datos.jugador.escenaActual);
    
    // Aplicar datos después de cargar escena
    StartCoroutine(AplicarDatosCargados(datos));
}

private IEnumerator AplicarDatosCargados(SaveData datos)
{
    // Esperar a que la escena cargue
    yield return new WaitForSeconds(0.1f);
    
    // Buscar jugador
    var jugador = FindObjectOfType<Jugador>();
    
    // Cargar clase
    var clase = Resources.Load<ClaseData>($"Clases/{datos.jugador.claseId}");
    if (clase != null)
        jugador.InicializarClase(clase);
    
    // Aplicar stats
    jugador.Nivel = datos.jugador.nivel;
    jugador.Experiencia = datos.jugador.experiencia;
    jugador.Stats.VidaActual = datos.jugador.vidaActual;
    
    // Aplicar posición
    jugador.transform.position = datos.jugador.posicion.ToVector3();
    
    Debug.Log("Partida cargada correctamente");
}
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

### Guardar

```csharp
public void Guardar(int slot, SaveData datos)
{
    string ruta = ObtenerRutaGuardado(slot);
    
    // Serializar a JSON
    string json = JsonUtility.ToJson(datos, prettyPrint: true);
    
    // Escribir archivo
    System.IO.File.WriteAllText(ruta, json);
}

private string ObtenerRutaGuardado(int slot)
{
    string carpeta = Application.persistentDataPath + "/saves";
    
    // Crear carpeta si no existe
    if (!System.IO.Directory.Exists(carpeta))
        System.IO.Directory.CreateDirectory(carpeta);
    
    return $"{carpeta}/save_slot_{slot}.json";
}
```

### Cargar

```csharp
public SaveData Cargar(int slot)
{
    string ruta = ObtenerRutaGuardado(slot);
    
    if (!System.IO.File.Exists(ruta))
        return null;
    
    string json = System.IO.File.ReadAllText(ruta);
    return JsonUtility.FromJson<SaveData>(json);
}
```

---

## Auto-Guardado

```csharp
public class AutoSaveManager : MonoBehaviour
{
    [SerializeField] private float intervalo = 300f;  // 5 minutos
    [SerializeField] private int slotAutoSave = 99;   // Slot especial
    
    private float tiempoUltimoGuardado;
    
    void Update()
    {
        if (Time.time - tiempoUltimoGuardado > intervalo)
        {
            AutoGuardar();
            tiempoUltimoGuardado = Time.time;
        }
    }
    
    private void AutoGuardar()
    {
        var datos = CrearSaveData();
        SaveSystem.Instance.Guardar(slotAutoSave, datos);
        
        // Mostrar indicador en UI
        MostrarIconoGuardado();
    }
}
```

---

## Menú de Guardado/Carga

### UI de Slots

```csharp
public class MenuGuardado : MonoBehaviour
{
    [SerializeField] private Transform contenedorSlots;
    [SerializeField] private GameObject prefabSlot;
    [SerializeField] private int maxSlots = 5;
    
    void Start()
    {
        RefrescarSlots();
    }
    
    private void RefrescarSlots()
    {
        // Limpiar slots existentes
        foreach (Transform child in contenedorSlots)
            Destroy(child.gameObject);
        
        // Crear slots
        for (int i = 0; i < maxSlots; i++)
        {
            var slotGO = Instantiate(prefabSlot, contenedorSlots);
            var slotUI = slotGO.GetComponent<SlotGuardadoUI>();
            
            if (SaveSystem.Instance.ExisteGuardado(i))
            {
                SaveData datos = SaveSystem.Instance.Cargar(i);
                slotUI.MostrarDatos(i, datos);
            }
            else
            {
                slotUI.MostrarVacio(i);
            }
            
            int slotIndex = i;  // Capturar para closure
            slotUI.OnSeleccionado += () => OnSlotSeleccionado(slotIndex);
        }
    }
    
    private void OnSlotSeleccionado(int slot)
    {
        if (modoGuardar)
            GuardarEnSlot(slot);
        else
            CargarDeSlot(slot);
    }
}
```

### SlotGuardadoUI

```csharp
public class SlotGuardadoUI : MonoBehaviour
{
    [SerializeField] private Text textoNombre;
    [SerializeField] private Text textoFecha;
    [SerializeField] private Text textoNivel;
    [SerializeField] private Button boton;
    
    public event System.Action OnSeleccionado;
    
    public void MostrarDatos(int slot, SaveData datos)
    {
        textoNombre.text = datos.jugador.nombre;
        textoFecha.text = datos.fechaGuardado.ToString("dd/MM/yyyy HH:mm");
        textoNivel.text = $"Nivel {datos.jugador.nivel}";
        
        boton.onClick.AddListener(() => OnSeleccionado?.Invoke());
    }
    
    public void MostrarVacio(int slot)
    {
        textoNombre.text = "--- Vacío ---";
        textoFecha.text = "";
        textoNivel.text = "";
        
        boton.onClick.AddListener(() => OnSeleccionado?.Invoke());
    }
}
```

---

## Migración de Versiones

Cuando actualizas el juego y cambia la estructura de SaveData:

```csharp
public SaveData Cargar(int slot)
{
    string json = System.IO.File.ReadAllText(ObtenerRuta(slot));
    var datos = JsonUtility.FromJson<SaveData>(json);
    
    // Migrar versiones antiguas
    if (datos.version == "0.9")
    {
        datos = MigrarV09aV10(datos);
    }
    
    return datos;
}

private SaveData MigrarV09aV10(SaveData viejo)
{
    // Aplicar cambios de estructura
    viejo.version = "1.0";
    
    // Migrar campos nuevos con valores por defecto
    if (viejo.objetivosCompletados == null)
        viejo.objetivosCompletados = new List<string>();
    
    return viejo;
}
```

---

## Encriptación (Opcional)

Para prevenir edición de guardados:

```csharp
private string Encriptar(string texto)
{
    // XOR simple (no es seguro, solo dificulta edición casual)
    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(texto);
    byte[] clave = System.Text.Encoding.UTF8.GetBytes("mi_clave_secreta");
    
    for (int i = 0; i < bytes.Length; i++)
        bytes[i] ^= clave[i % clave.Length];
    
    return System.Convert.ToBase64String(bytes);
}

private string Desencriptar(string textoEncriptado)
{
    byte[] bytes = System.Convert.FromBase64String(textoEncriptado);
    byte[] clave = System.Text.Encoding.UTF8.GetBytes("mi_clave_secreta");
    
    for (int i = 0; i < bytes.Length; i++)
        bytes[i] ^= clave[i % clave.Length];
    
    return System.Text.Encoding.UTF8.GetString(bytes);
}
```

---

## Ejemplo de Archivo JSON

```json
{
    "version": "1.0",
    "fechaGuardado": "2024-01-15T14:30:00",
    "tiempoJugado": 3600.5,
    "jugador": {
        "nombre": "Héroe",
        "claseId": "Guerrero",
        "nivel": 5,
        "experiencia": 450,
        "vidaActual": 85,
        "manaActual": 30,
        "posicion": {
            "x": 10.5,
            "y": 0,
            "z": 25.3
        },
        "escenaActual": "Bosque"
    },
    "nivelActual": 2,
    "objetivosCompletados": [
        "tutorial_completado",
        "primera_mision"
    ],
    "volumenMusica": 0.8,
    "volumenEfectos": 1.0
}
```

---

## Checklist de Implementación

- [ ] Crear clase SaveData con todos los campos necesarios
- [ ] Implementar SaveSystem singleton
- [ ] Crear JugadorSaveData y otros sub-datos
- [ ] Implementar Vector3Serializable
- [ ] Crear UI de menú de guardado
- [ ] Agregar auto-guardado (opcional)
- [ ] Probar guardado/carga
- [ ] Verificar persistencia entre sesiones

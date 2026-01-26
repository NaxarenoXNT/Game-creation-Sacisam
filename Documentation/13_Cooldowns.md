# Sistema de Cooldowns

## Visión General

El sistema de cooldowns controla qué habilidades pueden usarse y cuándo. Cada habilidad tiene un cooldown en turnos que se reduce al final del turno de la entidad.

```
IGestorHabilidades
        │
        └── GestorCooldowns
                │
                └── Dictionary<HabilidadData, int>
                        │
                        ├── "Bola de Fuego" → 2 turnos
                        ├── "Golpe Fuerte" → 0 turnos (disponible)
                        └── "Curación" → 1 turno
```

---

## Interfaz IGestorHabilidades

**Archivo**: `Assets/Scripts/Interfaces/IHabilidadesCommand.cs`

```csharp
public interface IGestorHabilidades
{
    // Verificar si una habilidad está en cooldown
    bool EstaEnCooldown(HabilidadData habilidad);
    
    // Obtener turnos restantes de cooldown
    int ObtenerCooldownRestante(HabilidadData habilidad);
    
    // Activar el cooldown de una habilidad
    void IniciarCooldown(HabilidadData habilidad);
    
    // Reducir todos los cooldowns (llamar al final del turno)
    void ReducirCooldowns();
    
    // Resetear todos los cooldowns
    void ResetearCooldowns();
}
```

---

## Implementación: GestorCooldowns

**Archivo**: `Assets/Scripts/Managers/GestorCooldowns.cs`

```csharp
using System.Collections.Generic;

namespace Managers
{
    public class GestorCooldowns : IGestorHabilidades
    {
        private Dictionary<HabilidadData, int> cooldowns = new Dictionary<HabilidadData, int>();
        
        public bool EstaEnCooldown(HabilidadData habilidad)
        {
            return cooldowns.ContainsKey(habilidad) && cooldowns[habilidad] > 0;
        }
        
        public int ObtenerCooldownRestante(HabilidadData habilidad)
        {
            if (cooldowns.TryGetValue(habilidad, out int turnos))
                return turnos;
            return 0;
        }
        
        public void IniciarCooldown(HabilidadData habilidad)
        {
            if (habilidad.cooldown > 0)
            {
                cooldowns[habilidad] = habilidad.cooldown;
            }
        }
        
        public void ReducirCooldowns()
        {
            var keys = new List<HabilidadData>(cooldowns.Keys);
            
            foreach (var habilidad in keys)
            {
                cooldowns[habilidad]--;
                
                if (cooldowns[habilidad] <= 0)
                    cooldowns.Remove(habilidad);
            }
        }
        
        public void ResetearCooldowns()
        {
            cooldowns.Clear();
        }
    }
}
```

---

## Integración con EntityController

**Archivo**: `Assets/Scripts/Controllers/EntityController.cs`

```csharp
public class EntityController : MonoBehaviour, IEntidadActuable
{
    // Gestor de cooldowns para esta entidad
    public IGestorHabilidades GestorCooldowns { get; private set; }
    
    void Awake()
    {
        GestorCooldowns = new GestorCooldowns();
    }
    
    // ... resto de la implementación
}
```

---

## Uso en Combate

### Al Usar una Habilidad

```csharp
public void UsarHabilidad(HabilidadData habilidad, Entidad objetivo)
{
    // Verificar cooldown
    if (GestorCooldowns.EstaEnCooldown(habilidad))
    {
        int turnos = GestorCooldowns.ObtenerCooldownRestante(habilidad);
        Debug.Log($"{habilidad.nombre} en cooldown por {turnos} turnos");
        return;
    }
    
    // Ejecutar la habilidad
    habilidad.Ejecutar(this.entidad, objetivo);
    
    // Iniciar cooldown
    GestorCooldowns.IniciarCooldown(habilidad);
}
```

### Al Finalizar Turno

```csharp
// En CombateManager
private void FinalizarTurno(IEntidadActuable entidad)
{
    // Reducir cooldowns de la entidad
    if (entidad is EntityController controller)
    {
        controller.GestorCooldowns.ReducirCooldowns();
    }
    
    // Pasar al siguiente turno
    SiguienteTurno();
}
```

---

## Configurar Cooldown en HabilidadData

**Archivo**: `Assets/Scripts/Habilidades/HabilidadData.cs`

```csharp
[CreateAssetMenu(fileName = "NuevaHabilidad", menuName = "RPG/Habilidad")]
public class HabilidadData : ScriptableObject
{
    [Header("Información")]
    public string nombre;
    public string descripcion;
    public Sprite icono;
    
    [Header("Cooldown")]
    [Tooltip("Turnos de espera después de usar la habilidad")]
    [Range(0, 10)]
    public int cooldown = 0;
    
    [Header("Efectos")]
    // ... otros campos
}
```

### Ejemplos de Cooldown

| Habilidad | Cooldown | Razón |
|-----------|----------|-------|
| Ataque Básico | 0 | Siempre disponible |
| Golpe Fuerte | 2 | Más poderoso |
| Bola de Fuego | 3 | Alto daño en área |
| Curación | 4 | Evita spam de curas |
| Resurrección | 5 | Muy poderoso |
| Ulti | 8 | Devastador |

---

## UI de Cooldowns

### En BotonHabilidad

```csharp
public class BotonHabilidad : MonoBehaviour
{
    [SerializeField] private Image imagenCooldown;
    [SerializeField] private Text textoCooldown;
    [SerializeField] private Button boton;
    
    private HabilidadData habilidad;
    private IGestorHabilidades gestor;
    
    void Update()
    {
        ActualizarCooldown();
    }
    
    private void ActualizarCooldown()
    {
        if (habilidad == null || gestor == null) return;
        
        int restante = gestor.ObtenerCooldownRestante(habilidad);
        bool enCooldown = restante > 0;
        
        // Actualizar visuales
        boton.interactable = !enCooldown;
        imagenCooldown.gameObject.SetActive(enCooldown);
        textoCooldown.gameObject.SetActive(enCooldown);
        
        if (enCooldown)
        {
            textoCooldown.text = restante.ToString();
            
            // Llenar el overlay proporcionalmente
            imagenCooldown.fillAmount = (float)restante / habilidad.cooldown;
        }
    }
}
```

### Visual del Cooldown

```
┌─────────────┐
│    ████     │  ← Overlay semitransparente
│    ████     │
│     3       │  ← Número de turnos
│    ████     │
└─────────────┘
     Icono
```

---

## Diagrama de Flujo

```
Jugador selecciona "Bola de Fuego"
                │
                ▼
        ¿Está en cooldown?
               / \
              /   \
           Sí      No
            │       │
            ▼       ▼
      Mostrar    Ejecutar
      mensaje    habilidad
                    │
                    ▼
              IniciarCooldown(3)
                    │
                    ▼
              Fin del turno
                    │
                    ▼
              ReducirCooldowns()
                    │
                    ▼
              "Bola de Fuego" → 2 turnos
                    │
                    ▼
              (2 turnos después)
                    │
                    ▼
              "Bola de Fuego" → 0 turnos
                    │
                    ▼
              ¡Disponible de nuevo!
```

---

## Casos Especiales

### Reducción de Cooldown (Buff)

```csharp
public void ReducirCooldownEspecifico(HabilidadData habilidad, int cantidad)
{
    if (cooldowns.ContainsKey(habilidad))
    {
        cooldowns[habilidad] = Mathf.Max(0, cooldowns[habilidad] - cantidad);
        
        if (cooldowns[habilidad] <= 0)
            cooldowns.Remove(habilidad);
    }
}
```

### Resetear Cooldown (Habilidad Especial)

```csharp
// Habilidad que resetea todos los cooldowns
public class ResetCooldownEffect : IHabilidadEffect
{
    public void Aplicar(Entidad objetivo, Entidad lanzador, int potencia, bool esMagico)
    {
        if (objetivo is Jugador jugador)
        {
            // Obtener el gestor desde el controller
            var controller = jugador.GetComponent<EntityController>();
            controller?.GestorCooldowns.ResetearCooldowns();
            
            Debug.Log($"¡{objetivo.Nombre} tiene todos los cooldowns reseteados!");
        }
    }
}
```

### Cooldown Aumentado (Debuff)

```csharp
public void AumentarCooldownsActivos(int cantidad)
{
    var keys = new List<HabilidadData>(cooldowns.Keys);
    
    foreach (var habilidad in keys)
    {
        cooldowns[habilidad] += cantidad;
    }
}
```

---

## Persistencia (Guardado)

### En SaveData

```csharp
[System.Serializable]
public class CooldownSaveData
{
    public string habilidadId;
    public int turnosRestantes;
}

[System.Serializable]
public class JugadorSaveData
{
    // ... otros campos
    public List<CooldownSaveData> cooldowns;
}
```

### Guardar Cooldowns

```csharp
public List<CooldownSaveData> ObtenerCooldownsParaGuardar()
{
    var lista = new List<CooldownSaveData>();
    
    foreach (var kvp in cooldowns)
    {
        lista.Add(new CooldownSaveData
        {
            habilidadId = kvp.Key.name,
            turnosRestantes = kvp.Value
        });
    }
    
    return lista;
}
```

### Cargar Cooldowns

```csharp
public void CargarCooldowns(List<CooldownSaveData> datos, List<HabilidadData> habilidades)
{
    cooldowns.Clear();
    
    foreach (var dato in datos)
    {
        var habilidad = habilidades.Find(h => h.name == dato.habilidadId);
        if (habilidad != null)
        {
            cooldowns[habilidad] = dato.turnosRestantes;
        }
    }
}
```

---

## Resumen

| Componente | Responsabilidad |
|------------|-----------------|
| `HabilidadData.cooldown` | Define el cooldown base |
| `GestorCooldowns` | Almacena y gestiona cooldowns activos |
| `EntityController` | Expone el gestor a otros sistemas |
| `CombateManager` | Llama a ReducirCooldowns() al fin de turno |
| `BotonHabilidad` | Muestra el estado del cooldown en UI |

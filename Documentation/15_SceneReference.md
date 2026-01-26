# SceneReference

## Visión General

SceneReference proporciona referencias type-safe a escenas de Unity, evitando los problemas de usar strings mágicos o índices que se rompen fácilmente.

```
Problema con strings:
    SceneManager.LoadScene("MiEscena");  // ¿Existe? ¿Está bien escrito?

Solución con SceneReference:
    [SerializeField] SceneReference escena;
    SceneManager.LoadScene(escena.ScenePath);  // Validado en editor
```

---

## SceneReference

**Archivo**: `Assets/Scripts/Utils/SceneReference.cs`

### Definición

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utils
{
    [System.Serializable]
    public class SceneReference
    {
        [SerializeField] private string scenePath;
        [SerializeField] private string sceneName;
        
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif
        
        public string ScenePath => scenePath;
        public string SceneName => sceneName;
        
        public void LoadScene(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("SceneReference: No hay escena asignada");
                return;
            }
            
            SceneManager.LoadScene(scenePath, mode);
        }
        
        public AsyncOperation LoadSceneAsync(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogError("SceneReference: No hay escena asignada");
                return null;
            }
            
            return SceneManager.LoadSceneAsync(scenePath, mode);
        }
        
        // Conversión implícita a string
        public static implicit operator string(SceneReference sceneRef)
        {
            return sceneRef?.scenePath ?? "";
        }
    }
}
```

---

## Custom Editor

Para que funcione arrastrando escenas en el inspector:

**Archivo**: `Assets/Editor/SceneReferenceDrawer.cs`

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Utils.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var sceneAssetProp = property.FindPropertyRelative("sceneAsset");
            var scenePathProp = property.FindPropertyRelative("scenePath");
            var sceneNameProp = property.FindPropertyRelative("sceneName");
            
            // Mostrar campo de SceneAsset
            EditorGUI.BeginChangeCheck();
            var newScene = EditorGUI.ObjectField(
                position, 
                label, 
                sceneAssetProp.objectReferenceValue, 
                typeof(SceneAsset), 
                false
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProp.objectReferenceValue = newScene;
                
                if (newScene != null)
                {
                    string path = AssetDatabase.GetAssetPath(newScene);
                    scenePathProp.stringValue = path;
                    sceneNameProp.stringValue = newScene.name;
                }
                else
                {
                    scenePathProp.stringValue = "";
                    sceneNameProp.stringValue = "";
                }
            }
            
            EditorGUI.EndProperty();
        }
    }
}
#endif
```

---

## Uso Básico

### En MonoBehaviour

```csharp
public class CargadorEscenas : MonoBehaviour
{
    [Header("Escenas del Juego")]
    [SerializeField] private SceneReference escenaMenu;
    [SerializeField] private SceneReference escenaNivel1;
    [SerializeField] private SceneReference escenaNivel2;
    [SerializeField] private SceneReference escenaCreditos;
    
    public void IrAMenu()
    {
        escenaMenu.LoadScene();
    }
    
    public void IrANivel1()
    {
        escenaNivel1.LoadScene();
    }
    
    public void CargarNivelAsync()
    {
        StartCoroutine(CargarConPantallaCarga());
    }
    
    private IEnumerator CargarConPantallaCarga()
    {
        // Mostrar pantalla de carga
        pantallaCargar.SetActive(true);
        
        // Cargar escena asíncronamente
        AsyncOperation operacion = escenaNivel1.LoadSceneAsync();
        operacion.allowSceneActivation = false;
        
        while (operacion.progress < 0.9f)
        {
            barraProgreso.fillAmount = operacion.progress;
            yield return null;
        }
        
        // Activar escena
        operacion.allowSceneActivation = true;
    }
}
```

### En ScriptableObject

```csharp
[CreateAssetMenu(fileName = "NuevoNivel", menuName = "RPG/Nivel")]
public class NivelData : ScriptableObject
{
    public string nombreNivel;
    public SceneReference escena;
    public int dificultad;
    public Sprite imagen;
}
```

---

## Configurar en Unity

### Paso 1: Agregar Campo

```csharp
[SerializeField] private SceneReference miEscena;
```

### Paso 2: Arrastrar Escena

1. En el Inspector, ver el campo "Mi Escena"
2. Arrastrar un archivo `.unity` desde Project
3. El campo mostrará el nombre de la escena

### Paso 3: Usar

```csharp
miEscena.LoadScene();
// o
SceneManager.LoadScene(miEscena.ScenePath);
```

---

## Comparación

### Sin SceneReference (Problemático)

```csharp
// Problema 1: String mágico
SceneManager.LoadScene("Nivel_1");  // ¿Typo? No hay validación

// Problema 2: Índice frágil
SceneManager.LoadScene(2);  // ¿Cuál es el 2? Se rompe si reordenas

// Problema 3: Cambios de nombre
// Si renombras "Nivel_1" a "Nivel_Tutorial", debes buscar todos los strings
```

### Con SceneReference (Seguro)

```csharp
[SerializeField] private SceneReference nivel1;

// El editor valida que la escena existe
// Si renombras, el path se actualiza automáticamente
// Intellisense y autocompletado
nivel1.LoadScene();
```

---

## Integración con GameManager

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Escenas")]
    [SerializeField] private SceneReference escenaMenu;
    [SerializeField] private SceneReference escenaCombate;
    [SerializeField] private SceneReference escenaMundoAbierto;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void IrAMenu()
    {
        escenaMenu.LoadScene();
    }
    
    public void IniciarCombate()
    {
        // Cargar escena de combate de forma aditiva
        escenaCombate.LoadScene(LoadSceneMode.Additive);
    }
    
    public void VolverAlMundo()
    {
        SceneManager.UnloadSceneAsync(escenaCombate.ScenePath);
    }
}
```

---

## Validación

### Verificar en Build Settings

```csharp
#if UNITY_EDITOR
public void OnValidate()
{
    if (!string.IsNullOrEmpty(scenePath))
    {
        // Verificar que la escena está en Build Settings
        bool enBuild = false;
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == scenePath)
            {
                enBuild = true;
                break;
            }
        }
        
        if (!enBuild)
        {
            Debug.LogWarning($"SceneReference: '{sceneName}' no está en Build Settings");
        }
    }
}
#endif
```

---

## Lista de Escenas

Para gestionar múltiples niveles:

```csharp
[CreateAssetMenu(fileName = "ListaNiveles", menuName = "RPG/Lista de Niveles")]
public class ListaNiveles : ScriptableObject
{
    [System.Serializable]
    public class NivelInfo
    {
        public string nombre;
        public SceneReference escena;
        public bool desbloqueado;
    }
    
    public List<NivelInfo> niveles;
    
    public void CargarNivel(int indice)
    {
        if (indice >= 0 && indice < niveles.Count)
        {
            niveles[indice].escena.LoadScene();
        }
    }
}
```

### Uso en Menú de Selección

```csharp
public class MenuNiveles : MonoBehaviour
{
    [SerializeField] private ListaNiveles listaNiveles;
    [SerializeField] private Transform contenedorBotones;
    [SerializeField] private GameObject prefabBoton;
    
    void Start()
    {
        for (int i = 0; i < listaNiveles.niveles.Count; i++)
        {
            var nivel = listaNiveles.niveles[i];
            var boton = Instantiate(prefabBoton, contenedorBotones);
            
            boton.GetComponentInChildren<Text>().text = nivel.nombre;
            boton.GetComponent<Button>().interactable = nivel.desbloqueado;
            
            int indice = i;  // Capturar para closure
            boton.GetComponent<Button>().onClick.AddListener(() => {
                listaNiveles.CargarNivel(indice);
            });
        }
    }
}
```

---

## Resumen

| Característica | Sin SceneReference | Con SceneReference |
|----------------|--------------------|--------------------|
| Validación en editor | ❌ | ✅ |
| Refactoring seguro | ❌ | ✅ |
| Arrastrar y soltar | ❌ | ✅ |
| Autocompletado | ❌ | ✅ |
| Errores en runtime | Posibles | Evitados |

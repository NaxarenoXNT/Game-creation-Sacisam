using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Managers
{
    /// <summary>
    /// Referencia type-safe a una escena de Unity.
    /// Evita el uso de strings magicos para nombres de escenas.
    /// </summary>
    [System.Serializable]
    public class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField] private Object sceneAsset;
#endif
        [SerializeField] private string scenePath = "";
        [SerializeField] private string sceneName = "";
        
        /// <summary>
        /// Nombre de la escena (sin extension ni ruta).
        /// </summary>
        public string SceneName
        {
            get
            {
#if UNITY_EDITOR
                if (sceneAsset != null)
                {
                    return sceneAsset.name;
                }
#endif
                return sceneName;
            }
        }
        
        /// <summary>
        /// Ruta completa de la escena.
        /// </summary>
        public string ScenePath
        {
            get
            {
#if UNITY_EDITOR
                if (sceneAsset != null)
                {
                    return AssetDatabase.GetAssetPath(sceneAsset);
                }
#endif
                return scenePath;
            }
        }
        
        /// <summary>
        /// Verifica si la referencia es valida.
        /// </summary>
        public bool IsValid
        {
            get
            {
#if UNITY_EDITOR
                return sceneAsset != null;
#else
                return !string.IsNullOrEmpty(scenePath);
#endif
            }
        }
        
        /// <summary>
        /// Operador implicito para usar como string.
        /// </summary>
        public static implicit operator string(SceneReference reference)
        {
            return reference.SceneName;
        }
        
        public override string ToString()
        {
            return SceneName;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Actualiza los datos serializados desde el asset de escena.
        /// Llamado automaticamente desde el PropertyDrawer.
        /// </summary>
        public void ValidateSceneAsset()
        {
            if (sceneAsset != null)
            {
                scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                sceneName = sceneAsset.name;
            }
            else
            {
                scenePath = "";
                sceneName = "";
            }
        }
#endif
    }
    
    /// <summary>
    /// Utilidades para cargar escenas de forma segura.
    /// </summary>
    public static class SceneLoader
    {
        /// <summary>
        /// Carga una escena de forma asincrona.
        /// </summary>
        public static AsyncOperation CargarEscenaAsync(SceneReference escena, UnityEngine.SceneManagement.LoadSceneMode modo = UnityEngine.SceneManagement.LoadSceneMode.Single)
        {
            if (!escena.IsValid)
            {
                Debug.LogError("SceneReference invalida");
                return null;
            }
            
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(escena.SceneName, modo);
        }
        
        /// <summary>
        /// Carga una escena de forma sincrona.
        /// </summary>
        public static void CargarEscena(SceneReference escena, UnityEngine.SceneManagement.LoadSceneMode modo = UnityEngine.SceneManagement.LoadSceneMode.Single)
        {
            if (!escena.IsValid)
            {
                Debug.LogError("SceneReference invalida");
                return;
            }
            
            UnityEngine.SceneManagement.SceneManager.LoadScene(escena.SceneName, modo);
        }
        
        /// <summary>
        /// Descarga una escena de forma asincrona.
        /// </summary>
        public static AsyncOperation DescargarEscenaAsync(SceneReference escena)
        {
            if (!escena.IsValid)
            {
                Debug.LogError("SceneReference invalida");
                return null;
            }
            
            return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(escena.SceneName);
        }
    }
}

#if UNITY_EDITOR
namespace Managers.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var sceneAssetProp = property.FindPropertyRelative("sceneAsset");
            
            EditorGUI.BeginChangeCheck();
            var newScene = EditorGUI.ObjectField(position, label, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);
            
            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProp.objectReferenceValue = newScene;
                
                // Actualizar path y name
                var pathProp = property.FindPropertyRelative("scenePath");
                var nameProp = property.FindPropertyRelative("sceneName");
                
                if (newScene != null)
                {
                    pathProp.stringValue = AssetDatabase.GetAssetPath(newScene);
                    nameProp.stringValue = newScene.name;
                }
                else
                {
                    pathProp.stringValue = "";
                    nameProp.stringValue = "";
                }
            }
            
            EditorGUI.EndProperty();
        }
    }
}
#endif

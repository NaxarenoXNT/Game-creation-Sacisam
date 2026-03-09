#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace World.ChunkSystem
{
    [CustomEditor(typeof(PropData))]
    public class PropDataEditor : UnityEditor.Editor
    {
        private SerializedProperty propNameProp;
        private SerializedProperty categoryProp;
        private SerializedProperty prefabProp;
        private SerializedProperty isInteractiveProp;
        private SerializedProperty consumeOnInteractProp;
        private SerializedProperty persistConsumedStateProp;

        private void OnEnable()
        {
            propNameProp = serializedObject.FindProperty("propName");
            categoryProp = serializedObject.FindProperty("category");
            prefabProp = serializedObject.FindProperty("prefab");
            isInteractiveProp = serializedObject.FindProperty("isInteractive");
            consumeOnInteractProp = serializedObject.FindProperty("consumeOnInteract");
            persistConsumedStateProp = serializedObject.FindProperty("persistConsumedState");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(propNameProp);
            EditorGUILayout.PropertyField(categoryProp);

            var currentPrefab = prefabProp.objectReferenceValue as GameObject;
            var picked = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Prefab", "Podés arrastrar un prefab del Project, o una instancia desde la Hierarchy (se convertirá al prefab asset automáticamente)."),
                currentPrefab,
                typeof(GameObject),
                true);

            if (picked != currentPrefab)
            {
                if (picked == null)
                {
                    prefabProp.objectReferenceValue = null;
                }
                else if (EditorUtility.IsPersistent(picked))
                {
                    // Prefab/model asset del Project
                    prefabProp.objectReferenceValue = picked;
                }
                else
                {
                    // Instancia en escena → intentar resolver al prefab asset
                    var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(picked);
                    if (source == null)
                        source = PrefabUtility.GetCorrespondingObjectFromSource(picked);

                    if (source != null && EditorUtility.IsPersistent(source))
                    {
                        prefabProp.objectReferenceValue = source;
                    }
                    else
                    {
                        Debug.LogWarning("[PropData] No se pudo asignar el objeto porque no es un prefab asset ni una instancia de prefab.");
                    }
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(isInteractiveProp);
            EditorGUILayout.PropertyField(consumeOnInteractProp);
            EditorGUILayout.PropertyField(persistConsumedStateProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif

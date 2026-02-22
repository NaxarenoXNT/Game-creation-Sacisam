// Este archivo va en una carpeta Editor/
// Ruta sugerida: Assets/Editor/World/WorldBiomeMapEditor.cs

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using World.BiomeSystem;

namespace World.Editor
{
    /// <summary>
    /// Custom Editor para WorldBiomeMap.
    /// Permite colocar, mover y eliminar BiomeControlPoints directamente en Scene View.
    ///
    /// USO:
    /// - Seleccioná el GameObject con WorldBiomeMap en Hierarchy
    /// - En Scene View aparecen esferas de colores (una por punto de control)
    /// - Click en esfera → seleccionar ese punto (se resalta y muestra sus datos en Inspector)
    /// - Drag de esfera → mover el punto
    /// - En Inspector → botón "+" → agregar nuevo punto en el origen del chunk actual
    /// - En Inspector → botón "✕" junto a un punto → eliminarlo
    /// - Botón "Samplear aquí" → muestra en consola qué bioma domina en el centro de la Scene View
    /// </summary>
    [CustomEditor(typeof(WorldBiomeMap))]
    public class WorldBiomeMapEditor : UnityEditor.Editor
    {
        private WorldBiomeMap biomeMap;
        private int selectedPointIndex = -1;
        private bool isDragging = false;

        // Para agregar nuevos puntos
        private BiomeSettings newPointBiome;

        private void OnEnable()
        {
            biomeMap = (WorldBiomeMap)target;
        }

        // ─── Inspector ────────────────────────────────────────────────────────────

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("─── Herramientas ───────────────────────────", EditorStyles.boldLabel);

            // Panel para agregar nuevo punto
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Agregar Punto de Control", EditorStyles.boldLabel);

            newPointBiome = (BiomeSettings)EditorGUILayout.ObjectField(
                "Bioma", newPointBiome, typeof(BiomeSettings), false);

            EditorGUI.BeginDisabledGroup(newPointBiome == null);
            if (GUILayout.Button("➕ Agregar en (0, 0, 0)"))
            {
                Undo.RecordObject(biomeMap, "Agregar BiomeControlPoint");
                biomeMap.AddControlPoint(Vector3.zero, newPointBiome);
                EditorUtility.SetDirty(biomeMap);
            }
            EditorGUI.EndDisabledGroup();

            if (newPointBiome == null)
                EditorGUILayout.HelpBox("Asigná un BiomeSettings para habilitar el botón.", MessageType.Info);

            // Lista de puntos con botón de eliminar
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Puntos de Control Actuales", EditorStyles.boldLabel);

            var points = biomeMap.ControlPoints;
            int indexToRemove = -1;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                string biomeName = point.dominantBiome != null ? point.dominantBiome.biomeName : "⚠️ Sin bioma";

                EditorGUILayout.BeginHorizontal();

                // Resaltar el seleccionado
                if (i == selectedPointIndex)
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);

                if (GUILayout.Button($"[{i}] {biomeName} ({point.worldPosition.x:F0}, {point.worldPosition.z:F0})",
                    GUILayout.ExpandWidth(true)))
                {
                    selectedPointIndex = i;
                    SceneView.RepaintAll();
                }

                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                    indexToRemove = i;
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            if (indexToRemove >= 0)
            {
                Undo.RecordObject(biomeMap, "Eliminar BiomeControlPoint");
                biomeMap.RemoveControlPoint(indexToRemove);
                if (selectedPointIndex >= biomeMap.ControlPoints.Count)
                    selectedPointIndex = biomeMap.ControlPoints.Count - 1;
                EditorUtility.SetDirty(biomeMap);
            }

            // Botones de debug
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("─── Debug ──────────────────────────────────", EditorStyles.boldLabel);

            if (GUILayout.Button("🔍 Samplear bioma en origen (0,0,0)"))
            {
                if (Application.isPlaying && WorldBiomeMap.Instance != null)
                {
                    var sample = WorldBiomeMap.Instance.GetBiomeAt(Vector3.zero);
                    Debug.Log("=== BiomeSample en (0,0,0) ===");
                    foreach (var (biome, weight) in sample.Influences)
                        Debug.Log($"  {biome.biomeName}: {weight:P0}");
                }
                else
                {
                    Debug.Log("El sampleo en runtime requiere Play Mode. " +
                              "En Edit Mode podés ver las esferas de influencia en Scene View.");
                }
            }
        }

        // ─── Scene View ───────────────────────────────────────────────────────────

        private void OnSceneGUI()
        {
            var points = biomeMap.ControlPoints;
            if (points == null || points.Count == 0) return;

            float blendRadius = biomeMap.BlendRadius;
            Event e = Event.current;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point.dominantBiome == null) continue;

                Color biomeColor = GetBiomeColor(point.dominantBiome.category);
                bool isSelected = (i == selectedPointIndex);

                // Tamaño del handle: más grande si está seleccionado
                float handleSize = HandleUtility.GetHandleSize(point.worldPosition) *
                                   (isSelected ? 0.15f : 0.10f);

                // Dibujar esfera del punto
                Handles.color = isSelected ? Color.white : biomeColor;
                if (Handles.Button(point.worldPosition, Quaternion.identity,
                    handleSize, handleSize, Handles.SphereHandleCap))
                {
                    selectedPointIndex = i;
                    Repaint();
                }

                // Handle de movimiento para el punto seleccionado
                if (isSelected)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.PositionHandle(point.worldPosition, Quaternion.identity);
                    // Bloquear Y: los puntos de bioma solo se mueven en XZ
                    newPos.y = point.worldPosition.y;

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(biomeMap, "Mover BiomeControlPoint");

                        // ⚠️ ASUNCIÓN: BiomeControlPoint es una clase (referencia), no struct.
                        // Si es struct, necesitás acceso diferente. Verificar.
                        // Como controlPoints es privado con IReadOnlyList, necesitamos
                        // acceder via serializedObject para que Undo funcione correctamente.
                        var so = new SerializedObject(biomeMap);
                        var pointsProp = so.FindProperty("controlPoints");
                        if (pointsProp != null && i < pointsProp.arraySize)
                        {
                            var pointProp = pointsProp.GetArrayElementAtIndex(i);
                            pointProp.FindPropertyRelative("worldPosition").vector3Value = newPos;
                            so.ApplyModifiedProperties();
                        }

                        EditorUtility.SetDirty(biomeMap);
                    }

                    // Label con nombre del bioma sobre el punto seleccionado
                    Handles.Label(point.worldPosition + Vector3.up * (handleSize * 2f),
                        point.dominantBiome.biomeName,
                        new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                }
                else
                {
                    // Label pequeño para puntos no seleccionados
                    Handles.Label(point.worldPosition + Vector3.up * (handleSize * 1.5f),
                        point.dominantBiome.biomeName);
                }

                // Radio de influencia (wireframe disc en el suelo)
                Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, isSelected ? 0.3f : 0.1f);
                Handles.DrawSolidDisc(point.worldPosition, Vector3.up, blendRadius);
                Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, isSelected ? 0.8f : 0.3f);
                Handles.DrawWireDisc(point.worldPosition, Vector3.up, blendRadius);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static Color GetBiomeColor(BiomeCategory category)
        {
            return category switch
            {
                BiomeCategory.Forest      => new Color(0.1f, 0.7f, 0.1f),
                BiomeCategory.Plains      => new Color(0.7f, 0.9f, 0.2f),
                BiomeCategory.Mountain    => new Color(0.6f, 0.5f, 0.4f),
                BiomeCategory.Arid        => new Color(0.9f, 0.8f, 0.2f),
                BiomeCategory.Coastal     => new Color(0.2f, 0.7f, 0.9f),
                BiomeCategory.Dark        => new Color(0.5f, 0.1f, 0.6f),
                BiomeCategory.Urban       => new Color(0.7f, 0.7f, 0.7f),
                BiomeCategory.Underground => new Color(0.3f, 0.2f, 0.1f),
                _                         => Color.white
            };
        }
    }
}
#endif
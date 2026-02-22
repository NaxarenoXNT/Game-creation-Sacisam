#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using World.BiomeSystem;

namespace World.BiomeSystem.Editor
{
    /// <summary>
    /// Custom Editor para WorldBiomeMap.
    /// Permite colocar, mover y eliminar BiomeControlPoints directamente en Scene View.
    ///
    /// USO:
    /// - Seleccioná el GameObject con WorldBiomeMap en Hierarchy
    /// - En Scene View aparecen esferas de colores (una por punto de control)
    /// - Click en esfera → seleccionar punto (se resalta y muestra datos en Inspector)
    /// - Drag de esfera → mover el punto
    /// - Botón "+" en Inspector → agregar nuevo punto
    /// - Botón "✕" junto a un punto → eliminarlo
    /// </summary>
    [CustomEditor(typeof(WorldBiomeMap))]
    public class WorldBiomeMapEditor : UnityEditor.Editor
    {
        private WorldBiomeMap biomeMap;
        private int selectedPointIndex = -1;

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

            if (points.Count == 0)
            {
                EditorGUILayout.HelpBox("No hay puntos de control. Agregá uno arriba.", MessageType.Warning);
            }

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
                    // Mover la vista hacia el punto seleccionado
                    SceneView.lastActiveSceneView?.LookAt(point.worldPosition);
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

            // Info general
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                $"Puntos: {points.Count} | " +
                $"Blend Radius: {biomeMap.BlendRadius}u\n" +
                "Seleccioná un punto en la lista o en Scene View para moverlo.",
                MessageType.None);
        }

        // ─── Scene View ───────────────────────────────────────────────────────────

        private void OnSceneGUI()
        {
            var points = biomeMap.ControlPoints;
            if (points == null || points.Count == 0) return;

            float blendRadius = biomeMap.BlendRadius;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point.dominantBiome == null) continue;

                Color biomeColor = GetBiomeColor(point.dominantBiome.category);
                bool isSelected = (i == selectedPointIndex);

                // Tamaño del handle: más grande si está seleccionado
                float handleSize = HandleUtility.GetHandleSize(point.worldPosition) *
                                   (isSelected ? 0.15f : 0.10f);

                // Dibujar esfera clickeable del punto
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
                    var labelStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 13,
                        normal = { textColor = Color.white }
                    };
                    Handles.Label(point.worldPosition + Vector3.up * (handleSize * 3f),
                        $"{point.dominantBiome.biomeName}\n({point.worldPosition.x:F0}, {point.worldPosition.z:F0})",
                        labelStyle);
                }
                else
                {
                    // Label pequeño para puntos no seleccionados
                    Handles.Label(point.worldPosition + Vector3.up * (handleSize * 1.5f),
                        point.dominantBiome.biomeName);
                }

                // Radio de influencia (disco en el suelo)
                Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, isSelected ? 0.12f : 0.04f);
                Handles.DrawSolidDisc(point.worldPosition, Vector3.up, blendRadius);
                Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, isSelected ? 0.8f : 0.25f);
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

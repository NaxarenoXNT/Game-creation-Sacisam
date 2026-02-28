using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Interfaces;
using Habilidades;
using Camera;
using Flags;

namespace UI.Combat
{
    /// <summary>
    /// Maneja la selección de objetivos durante el combate.
    /// Muestra indicadores sobre los objetivos válidos.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private GameObject targetIndicatorPrefab;
        [SerializeField] private Color allyIndicatorColor = Color.green;
        [SerializeField] private Color enemyIndicatorColor = Color.red;
        [SerializeField] private Color selectedColor = Color.yellow;
        
        [Header("UI")]
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private GameObject instructionPanel;
        
        // Estado
        private HabilidadData selectedSkill;
        private List<IEntidadCombate> validTargets = new List<IEntidadCombate>();
        private List<GameObject> activeIndicators = new List<GameObject>();
        private IEntidadCombate hoveredTarget;
        
        public HabilidadData SelectedSkill => selectedSkill;
        
        void Start()
        {
            Hide();
        }
        
        void Update()
        {
            if (validTargets.Count == 0) return;
            
            // Detectar hover sobre objetivos
            UpdateHover();
        }
        
        /// <summary>
        /// Muestra el selector de objetivos para la habilidad dada.
        /// </summary>
        public void Show(HabilidadData skill, List<IEntidadCombate> allies, List<IEntidadCombate> enemies)
        {
            selectedSkill = skill;
            validTargets.Clear();
            
            // Determinar objetivos válidos según el tipo de habilidad
            if (skill != null)
            {
                switch (skill.tipoObjetivo)
                {
                    case TargetType.EnemigoUnico:
                        AddValidTargets(enemies, false);
                        break;
                    
                    case TargetType.EnemigoTodos:
                        // Para AOE, mostrar todos pero seleccionar cualquiera
                        AddValidTargets(enemies, false);
                        break;
                    
                    case TargetType.AliadoUnico:
                        AddValidTargets(allies, true);
                        break;
                    
                    case TargetType.AliadoTodos:
                        AddValidTargets(allies, true);
                        break;
                    
                    case TargetType.Self:
                        // Self no necesita selección manual
                        break;
                }
            }
            
            // Crear indicadores visuales
            CreateIndicators();
            
            // Mostrar instrucción
            ShowInstruction();
            
            gameObject.SetActive(true);
            
            Debug.Log($"[TargetSelector] {validTargets.Count} objetivos válidos para {skill?.nombreHabilidad}");
        }
        
        /// <summary>
        /// Establece la habilidad seleccionada.
        /// </summary>
        public void SetSelectedSkill(HabilidadData skill)
        {
            selectedSkill = skill;
        }
        
        /// <summary>
        /// Oculta el selector.
        /// </summary>
        public void Hide()
        {
            ClearIndicators();
            validTargets.Clear();
            selectedSkill = null;
            hoveredTarget = null;
            
            if (instructionPanel != null)
                instructionPanel.SetActive(false);
            
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Agrega objetivos válidos a la lista.
        /// </summary>
        private void AddValidTargets(List<IEntidadCombate> targets, bool areAllies)
        {
            foreach (var target in targets)
            {
                if (target != null && target.EstaVivo())
                {
                    validTargets.Add(target);
                }
            }
        }
        
        /// <summary>
        /// Crea los indicadores visuales sobre los objetivos.
        /// </summary>
        private void CreateIndicators()
        {
            ClearIndicators();
            
            if (targetIndicatorPrefab == null) return;
            
            foreach (var target in validTargets)
            {
                // Obtener el transform del objetivo
                Transform targetTransform = GetTargetTransform(target);
                if (targetTransform == null) continue;
                
                // Crear indicador
                GameObject indicator = Instantiate(targetIndicatorPrefab, targetTransform);
                indicator.transform.localPosition = Vector3.up * 2.5f;
                
                // Colorear según tipo
                bool isAlly = target is IJugadorProgresion;
                Color color = isAlly ? allyIndicatorColor : enemyIndicatorColor;
                
                var renderers = indicator.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.material.color = color;
                }
                
                var images = indicator.GetComponentsInChildren<Image>();
                foreach (var image in images)
                {
                    image.color = color;
                }
                
                activeIndicators.Add(indicator);
            }
        }
        
        /// <summary>
        /// Limpia los indicadores existentes.
        /// </summary>
        private void ClearIndicators()
        {
            foreach (var indicator in activeIndicators)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
            }
            activeIndicators.Clear();
        }
        
        /// <summary>
        /// Actualiza el estado de hover.
        /// </summary>
        private void UpdateHover()
        {
            // Obtener ray desde el mouse
            Ray ray;
            if (IsometricCameraController.Instance != null)
            {
                ray = IsometricCameraController.Instance.GetMouseRay();
            }
            else if (UnityEngine.Camera.main != null)
            {
                ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            }
            else
            {
                return;
            }
            
            // Raycast para detectar entidades
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Buscar si es un objetivo válido
                var entity = hit.collider.GetComponent<EntityController>();
                var enemy = hit.collider.GetComponent<EnemyController>();
                
                IEntidadCombate hoveredEntity = null;
                
                if (entity != null)
                    hoveredEntity = entity.EntidadLogica;
                else if (enemy != null)
                    hoveredEntity = enemy.EnemigoLogica;
                
                if (hoveredEntity != null && validTargets.Contains(hoveredEntity))
                {
                    if (hoveredTarget != hoveredEntity)
                    {
                        // Nuevo hover
                        hoveredTarget = hoveredEntity;
                        HighlightTarget(hoveredEntity, true);
                    }
                }
                else if (hoveredTarget != null)
                {
                    // Perdió el hover
                    HighlightTarget(hoveredTarget, false);
                    hoveredTarget = null;
                }
            }
            else if (hoveredTarget != null)
            {
                HighlightTarget(hoveredTarget, false);
                hoveredTarget = null;
            }
        }
        
        /// <summary>
        /// Resalta o des-resalta un objetivo.
        /// </summary>
        private void HighlightTarget(IEntidadCombate target, bool highlight)
        {
            Transform targetTransform = GetTargetTransform(target);
            if (targetTransform == null) return;
            
            int index = validTargets.IndexOf(target);
            if (index < 0 || index >= activeIndicators.Count) return;
            
            var indicator = activeIndicators[index];
            if (indicator == null) return;
            
            // Cambiar color o escala para indicar highlight
            Color color = highlight ? selectedColor : 
                (target is IJugadorProgresion ? allyIndicatorColor : enemyIndicatorColor);
            
            var renderers = indicator.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material.color = color;
            }
            
            var images = indicator.GetComponentsInChildren<Image>();
            foreach (var image in images)
            {
                image.color = color;
            }
            
            // Escalar
            indicator.transform.localScale = highlight ? Vector3.one * 1.3f : Vector3.one;
        }
        
        /// <summary>
        /// Obtiene el transform de una entidad de combate.
        /// </summary>
        private Transform GetTargetTransform(IEntidadCombate target)
        {
            // Buscar EntityController o EnemyController
            var entities = Object.FindObjectsByType<EntityController>(FindObjectsSortMode.None);
            foreach (var e in entities)
            {
                if (e.EntidadLogica == target)
                    return e.transform;
            }
            
            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e.EnemigoLogica == target)
                    return e.transform;
            }
            
            return null;
        }
        
        /// <summary>
        /// Muestra la instrucción al usuario.
        /// </summary>
        private void ShowInstruction()
        {
            if (instructionPanel != null)
                instructionPanel.SetActive(true);
            
            if (instructionText != null)
            {
                string targetTypeText = selectedSkill?.tipoObjetivo switch
                {
                    TargetType.EnemigoUnico => "Selecciona un enemigo",
                    TargetType.EnemigoTodos => "Selecciona un enemigo (afecta a todos)",
                    TargetType.AliadoUnico => "Selecciona un aliado",
                    TargetType.AliadoTodos => "Selecciona un aliado (afecta a todos)",
                    TargetType.Self => "Objetivo: Tú mismo",
                    _ => "Selecciona un objetivo"
                };
                
                instructionText.text = targetTypeText;
            }
        }
        
        /// <summary>
        /// Verifica si un objetivo es válido.
        /// </summary>
        public bool IsValidTarget(IEntidadCombate target)
        {
            return validTargets.Contains(target);
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using Interfaces;
using Habilidades;
using Camera;
using Flags;

namespace UI.Combat
{
    /// <summary>
    /// Maneja la selección de objetivos durante el combate.
    /// Muestra indicadores 3D (prefabs) sobre los objetivos válidos
    /// y resalta el que tiene el mouse encima (hover via raycast desde cámara isométrica).
    ///
    /// La confirmación del objetivo NO la maneja este componente:
    /// la hace CombatUIController vía OnEnemyClicked / OnAllyClicked.
    /// Este componente solo gestiona la retroalimentación visual.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private GameObject targetIndicatorPrefab;
        [SerializeField] private Color allyIndicatorColor = Color.green;
        [SerializeField] private Color enemyIndicatorColor = Color.red;
        [SerializeField] private Color selectedColor = Color.yellow;
        
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
            UpdateHover();
        }
        
        /// <summary>
        /// Muestra indicadores 3D sobre los objetivos válidos para la habilidad dada.
        /// </summary>
        public void Show(HabilidadData skill, List<IEntidadCombate> allies, List<IEntidadCombate> enemies)
        {
            selectedSkill = skill;
            validTargets.Clear();
            
            if (skill != null)
            {
                switch (skill.tipoObjetivo)
                {
                    case TargetType.EnemigoUnico:
                    case TargetType.EnemigoTodos:
                        AddValidTargets(enemies);
                        break;
                    
                    case TargetType.AliadoUnico:
                    case TargetType.AliadoTodos:
                        AddValidTargets(allies);
                        break;
                    
                    case TargetType.Self:
                        // Self no necesita selección manual
                        break;
                }
            }
            
            CreateIndicators();
            gameObject.SetActive(true);
            
            Debug.Log($"[TargetSelector] {validTargets.Count} objetivos válidos para {skill?.nombreHabilidad}");
        }
        
        /// <summary>
        /// Oculta todos los indicadores y limpia estado.
        /// </summary>
        public void Hide()
        {
            ClearIndicators();
            validTargets.Clear();
            selectedSkill = null;
            hoveredTarget = null;
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Verifica si un objetivo es válido para la selección actual.
        /// </summary>
        public bool IsValidTarget(IEntidadCombate target)
        {
            return validTargets.Contains(target);
        }
        
        // =================== INTERNOS ===================
        
        private void AddValidTargets(List<IEntidadCombate> targets)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null && target.EstaVivo())
                    validTargets.Add(target);
            }
        }
        
        /// <summary>
        /// Crea indicadores 3D (prefabs) como hijos del transform de cada objetivo.
        /// </summary>
        private void CreateIndicators()
        {
            ClearIndicators();
            if (targetIndicatorPrefab == null) return;
            
            foreach (var target in validTargets)
            {
                Transform targetTransform = GetTargetTransform(target);
                if (targetTransform == null) continue;
                
                GameObject indicator = Instantiate(targetIndicatorPrefab, targetTransform);
                indicator.transform.localPosition = Vector3.up * 2.5f;
                
                bool isAlly = target is IJugadorProgresion;
                Color color = isAlly ? allyIndicatorColor : enemyIndicatorColor;
                ApplyColorToIndicator(indicator, color);
                
                activeIndicators.Add(indicator);
            }
        }
        
        private void ClearIndicators()
        {
            foreach (var indicator in activeIndicators)
            {
                if (indicator != null)
                    Destroy(indicator);
            }
            activeIndicators.Clear();
        }
        
        /// <summary>
        /// Detecta hover sobre objetivos válidos usando raycast desde la cámara isométrica.
        /// </summary>
        private void UpdateHover()
        {
            Ray ray;
            if (IsometricCameraController.Instance != null)
                ray = IsometricCameraController.Instance.GetMouseRay();
            else if (UnityEngine.Camera.main != null)
                ray = UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            else
                return;
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
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
                        if (hoveredTarget != null)
                            HighlightTarget(hoveredTarget, false);
                        
                        hoveredTarget = hoveredEntity;
                        HighlightTarget(hoveredEntity, true);
                    }
                }
                else if (hoveredTarget != null)
                {
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
        
        private void HighlightTarget(IEntidadCombate target, bool highlight)
        {
            int index = validTargets.IndexOf(target);
            if (index < 0 || index >= activeIndicators.Count) return;
            
            var indicator = activeIndicators[index];
            if (indicator == null) return;
            
            Color color = highlight ? selectedColor : 
                (target is IJugadorProgresion ? allyIndicatorColor : enemyIndicatorColor);
            
            ApplyColorToIndicator(indicator, color);
            indicator.transform.localScale = highlight ? Vector3.one * 1.3f : Vector3.one;
        }
        
        /// <summary>
        /// Aplica color a todos los Renderers del indicador 3D.
        /// </summary>
        private void ApplyColorToIndicator(GameObject indicator, Color color)
        {
            var renderers = indicator.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material.color = color;
            }
        }
        
        /// <summary>
        /// Obtiene el transform de una entidad de combate buscando los controllers en escena.
        /// </summary>
        private Transform GetTargetTransform(IEntidadCombate target)
        {
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
    }
}

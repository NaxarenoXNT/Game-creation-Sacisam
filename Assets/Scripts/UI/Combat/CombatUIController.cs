using UnityEngine;
using System.Collections.Generic;
using Managers;
using Interfaces;
using GameInput;
using Flags;

namespace UI.Combat
{
    /// <summary>
    /// Controlador principal de UI de combate.
    /// Coordina el estado global (highlights, selector de objetivo) y
    /// delega la visualización del HUD al HUDController (UI Toolkit).
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        public static CombatUIController Instance { get; private set; }
        
        [Header("Paneles Principales")]
        [SerializeField] private HUDController hudController;
        
        [Header("Selector de Objetivo")]
        [SerializeField] private TargetSelector targetSelector;
        
        [Header("Indicadores")]
        [SerializeField] private GameObject playerTurnHighlightPrefab;
        
        // Estado
        private bool isInCombat;
        private EntityController currentTurnEntity;
        private CombatUIState currentState = CombatUIState.Hidden;
        private GameObject currentHighlight;
        
        // Cache de aliados/enemigos del último evento
        private List<IEntidadCombate> cachedAliados;
        private List<IEntidadCombate> cachedEnemigos;
        
        // Habilidad pendiente de objetivo (se almacena al entrar en SelectingTarget)
        private HabilidadData _habilidadSeleccionada;
        
        public bool IsInCombat => isInCombat;
        public CombatUIState CurrentState => currentState;
        public EntityController CurrentTurnEntity => currentTurnEntity;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-wiring: si el campo no fue asignado manualmente busca en el mismo GO
            if (hudController == null)
                hudController = GetComponent<HUDController>();
        }
        
        void Start()
        {
            // Suscribirse a eventos de combate
            EventBus.Suscribir<EventoCombateIniciado>(OnCombatStarted);
            EventBus.Suscribir<EventoCombateFinalizado>(OnCombatEnded);
            EventBus.Suscribir<EventoEsperandoAccionJugador>(OnWaitingForPlayerAction);
            EventBus.Suscribir<EventoTurnoFinalizado>(OnTurnEnded);
            EventBus.Suscribir<EventoAccionSeleccionada>(OnActionSelected);
            
            // Suscribirse a input para cancelación y clicks en entidades
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.OnCancelAction += OnCancelInput;
                GameInputManager.Instance.OnEnemyClick  += OnEnemyClicked;
                GameInputManager.Instance.OnEntityClick += OnAllyClicked;
            }
            
            HideAllUI();
        }
        
        void OnDestroy()
        {
            EventBus.Desuscribir<EventoCombateIniciado>(OnCombatStarted);
            EventBus.Desuscribir<EventoCombateFinalizado>(OnCombatEnded);
            EventBus.Desuscribir<EventoEsperandoAccionJugador>(OnWaitingForPlayerAction);
            EventBus.Desuscribir<EventoTurnoFinalizado>(OnTurnEnded);
            EventBus.Desuscribir<EventoAccionSeleccionada>(OnActionSelected);
            
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.OnCancelAction -= OnCancelInput;
                GameInputManager.Instance.OnEnemyClick  -= OnEnemyClicked;
                GameInputManager.Instance.OnEntityClick -= OnAllyClicked;
            }
            
            if (Instance == this) Instance = null;
        }
        
        // =================== EVENTOS DE COMBATE ===================
        
        private void OnCombatStarted(EventoCombateIniciado evento)
        {
            isInCombat = true;
            // HUDController reacciona al mismo evento y se muestra solo.
            // NOTA: El cambio de InputContext ahora lo gestiona GameFlowController
            // a través de CombatFlowState.Enter()
            
            Debug.Log("[CombatUI] Combate iniciado");
        }
        
        private void OnCombatEnded(EventoCombateFinalizado evento)
        {
            isInCombat = false;
            HideAllUI();
            // NOTA: El cambio de InputContext ahora lo gestiona GameFlowController
            // a través de ExplorationFlowState.Enter() al hacer Pop()
            
            Debug.Log($"[CombatUI] Combate terminado - Victoria: {evento.Victoria}");
        }
        
        private void OnWaitingForPlayerAction(EventoEsperandoAccionJugador evento)
        {
            currentTurnEntity = evento.Entidad;
            
            // Guardar aliados/enemigos para el TargetSelector
            cachedAliados = evento.Aliados;
            cachedEnemigos = evento.Enemigos;
            
            // Mostrar highlight en el personaje activo
            ShowHighlight(evento.Entidad);
            
            // HUDController recibe el mismo evento y actualiza el panel de personaje.
            SetState(CombatUIState.ShowingCharacterPanel);
            
            Debug.Log($"[CombatUI] Esperando acción de: {evento.Entidad.Nombre_Entidad}");
        }
        
        private void OnTurnEnded(EventoTurnoFinalizado evento)
        {
            HideHighlight();
            HideTargetSelector();
            currentTurnEntity = null;
            cachedAliados = null;
            cachedEnemigos = null;
            SetState(CombatUIState.Hidden);
        }
        
        private void OnActionSelected(EventoAccionSeleccionada evento)
        {
            // Si se seleccionó una habilidad que requiere objetivo, mostrar selector
            if (evento.TipoAccion == CombatActionType.Atacar && evento.Habilidad != null)
            {
                // HUDController ya publicó el evento; ahora mostramos el selector
                if (evento.Habilidad.tipoObjetivo != Flags.TargetType.Self)
                {
                    ShowTargetSelector(evento.Habilidad);
                }
            }
        }
        
        private void OnCancelInput()
        {
            if (!isInCombat) return;
            
            switch (currentState)
            {
                case CombatUIState.SelectingTarget:
                    HideTargetSelector();
                    SetState(CombatUIState.ShowingCharacterPanel);
                    EventBus.Publicar(new EventoAccionCancelada { Entidad = currentTurnEntity });
                    break;
            }
        }
        
        // =================== MÉTODOS PÚBLICOS ===================
        
        /// <summary>
        /// Muestra el selector de objetivos para una habilidad.
        /// El TargetSelector legacy (uGUI) está bypaseado: la confirmación
        /// ocurre al clickear directamente el enemigo/aliado en escena.
        /// </summary>
        public void ShowTargetSelector(HabilidadData skill)
        {
            if (cachedAliados == null || cachedEnemigos == null) return;

            _habilidadSeleccionada = skill;
            SetState(CombatUIState.SelectingTarget);

            Debug.Log($"[CombatUI] Seleccionando objetivo para: {skill?.nombreHabilidad}. Click sobre el objetivo en escena.");
        }
        
        public void HideTargetSelector()
        {
            _habilidadSeleccionada = null;
            targetSelector?.Hide();
        }
        
        // =================== HIGHLIGHT ===================
        
        private void ShowHighlight(EntityController entity)
        {
            HideHighlight();
            
            if (playerTurnHighlightPrefab != null && entity != null)
            {
                currentHighlight = Instantiate(playerTurnHighlightPrefab, entity.transform);
                currentHighlight.transform.localPosition = Vector3.up * 0.1f;
            }
        }
        
        private void HideHighlight()
        {
            if (currentHighlight != null)
            {
                Destroy(currentHighlight);
                currentHighlight = null;
            }
        }
        
        private void HideAllUI()
        {
            HideTargetSelector();
            HideHighlight();

            // Fuerza ocultamiento del HUD como fallback (normalmente lo maneja el evento).
            hudController?.OcultarHUD();

            SetState(CombatUIState.Hidden);
        }
        
        private void SetState(CombatUIState newState)
        {
            currentState = newState;
        }

        // =================== CLICKS EN ENTIDADES (TARGET SELECTION) ===================

        /// <summary>
        /// Click sobre un enemigo en escena. Si estamos seleccionando objetivo,
        /// verifica que la habilidad apunte a enemigos y confirma el objetivo.
        /// </summary>
        private void OnEnemyClicked(EnemyController enemy)
        {
            if (!isInCombat || currentState != CombatUIState.SelectingTarget) return;
            if (_habilidadSeleccionada == null || currentTurnEntity == null) return;

            var tipo = _habilidadSeleccionada.tipoObjetivo;
            if (tipo != TargetType.EnemigoUnico && tipo != TargetType.EnemigoTodos) return;

            var objetivo = enemy.EnemigoLogica as IEntidadCombate;
            if (objetivo == null || !objetivo.EstaVivo()) return;
            if (!cachedEnemigos.Contains(objetivo)) return;

            ConfirmarObjetivo(objetivo);
        }

        /// <summary>
        /// Click sobre un aliado en escena. Si estamos seleccionando objetivo,
        /// verifica que la habilidad apunte a aliados y confirma el objetivo.
        /// </summary>
        private void OnAllyClicked(EntityController ally)
        {
            if (!isInCombat || currentState != CombatUIState.SelectingTarget) return;
            if (_habilidadSeleccionada == null || currentTurnEntity == null) return;

            var tipo = _habilidadSeleccionada.tipoObjetivo;
            if (tipo != TargetType.AliadoUnico && tipo != TargetType.AliadoTodos) return;

            var objetivo = ally.EntidadLogica as IEntidadCombate;
            if (objetivo == null || !objetivo.EstaVivo()) return;
            if (!cachedAliados.Contains(objetivo)) return;

            ConfirmarObjetivo(objetivo);
        }

        /// <summary>
        /// Publica EventoObjetivoSeleccionado y vuelve al estado de panel de personaje.
        /// </summary>
        private void ConfirmarObjetivo(IEntidadCombate objetivo)
        {
            Debug.Log($"[CombatUI] Objetivo confirmado: {objetivo.Nombre_Entidad}");

            EventBus.Publicar(new EventoObjetivoSeleccionado
            {
                Atacante  = currentTurnEntity,
                Objetivo  = objetivo,
                Habilidad = _habilidadSeleccionada
            });

            HideTargetSelector();
            SetState(CombatUIState.ShowingCharacterPanel);
        }
    }
    
    /// <summary>
    /// Estados de la UI de combate (simplificados).
    /// </summary>
    public enum CombatUIState
    {
        Hidden,                 // UI oculta
        ShowingCharacterPanel,  // Panel de personaje visible con acciones
        SelectingTarget         // Seleccionando objetivo para habilidad
    }
}

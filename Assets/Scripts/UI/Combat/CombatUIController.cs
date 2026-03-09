using UnityEngine;
using UnityEngine.UIElements;
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

        // =================== AUTO-SETUP ===================

        /// <summary>
        /// Garantiza que existe una instancia activa de CombatUIController.
        /// Si no la hay, busca el UIDocument con el HUD ("hud-root") y le
        /// agrega HUDController + CombatUIController automáticamente.
        /// </summary>
        public static CombatUIController EnsureInstance()
        {
            if (Instance != null) return Instance;

            // Buscar un UIDocument que contenga el elemento hud-root
            foreach (var doc in FindObjectsOfType<UIDocument>())
            {
                if (doc.rootVisualElement == null) continue;
                if (doc.rootVisualElement.Q("hud-root") == null) continue;

                Debug.Log($"[CombatUIController] Auto-setup: encontrado UIDocument con hud-root en '{doc.gameObject.name}'. Agregando componentes.");

                // HUDController primero (CombatUIController.Awake lo busca)
                if (doc.GetComponent<HUDController>() == null)
                    doc.gameObject.AddComponent<HUDController>();

                // Ahora CombatUIController
                if (doc.GetComponent<CombatUIController>() == null)
                    doc.gameObject.AddComponent<CombatUIController>();

                return Instance; // Awake() ya asignó Instance
            }

            Debug.LogError("[CombatUIController] No se encontró ningún UIDocument con 'hud-root'. " +
                           "Asegúrate de tener un UIDocument con HUD.uxml en la escena.");
            return null;
        }
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-wiring: busca primero en el mismo GO, luego en toda la escena
            if (hudController == null)
                hudController = GetComponent<HUDController>();
            if (hudController == null)
                hudController = FindObjectOfType<HUDController>();

            if (hudController == null)
                Debug.LogWarning("[CombatUIController] No se encontró HUDController. " +
                                 "Asigne la referencia en el Inspector o colóquelo en escena.");
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

            // Seguro de activación: EventoCombateIniciado se publica cuando el
            // combate está confirmado (IniciarSistemaDeTurnos lo emite).
            // La ruta primaria es CombatFlowState.Enter() → MostrarHUD(),
            // pero si algo falló en esa cadena (Instance null, timing) lo forzamos.
            // Es idempotente: display:Flex sobre Flex no tiene efecto.
            if (hudController != null)
            {
                hudController.MostrarHUD();
            }
            else
            {
                Debug.LogWarning("[CombatUI] ⚠️ hudController es null al recibir EventoCombateIniciado. " +
                                 "El HUD no se mostrará. Verifica que HUDController esté en escena.");
            }

            Debug.Log("[CombatUI] Combate iniciado (flag isInCombat=true)");
        }
        
        private void OnCombatEnded(EventoCombateFinalizado evento)
        {
            isInCombat = false;
            // Limpiar estado interno. La ocultación del HUD la maneja
            // GameFlowController → Pop() → CombatFlowState.Exit() → OcultarHUD().
            HideHighlight();
            HideTargetSelector();
            currentTurnEntity = null;
            cachedAliados = null;
            cachedEnemigos = null;
            
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
            // El HUD sigue visible entre turnos; solo volvemos a
            // ShowingCharacterPanel para que el próximo turno arranque limpio.
            SetState(CombatUIState.ShowingCharacterPanel);
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
        
        // =================== VISIBILIDAD PUBLICA (llamada desde CombatFlowState) ===================

        /// <summary>
        /// Muestra el HUD de combate. Lo llama CombatFlowState.Enter().
        /// El HUDController poblará los datos al recibir EventoCombateIniciado.
        /// </summary>
        public void MostrarHUD()
        {
            hudController?.MostrarHUD();
            // No forzamos ShowingCharacterPanel aquí porque aún no hay personaje.
            // El estado pasa a ShowingCharacterPanel cuando llega EventoEsperandoAccionJugador.
            if (currentState == CombatUIState.Hidden)
                SetState(CombatUIState.ShowingCharacterPanel);

            Debug.Log("[CombatUI] HUD mostrado via GameFlow");
        }

        /// <summary>
        /// Oculta toda la UI de combate. Lo llama CombatFlowState.Exit().
        /// </summary>
        public void OcultarHUD()
        {
            HideAllUI();
        }

        // =================== METODOS PUBLICOS ===================
        
        /// <summary>
        /// Muestra el selector de objetivos para una habilidad.
        /// Activa los indicadores 3D del TargetSelector sobre los objetivos válidos
        /// y espera que el jugador clickee sobre un enemigo/aliado en la escena.
        /// </summary>
        public void ShowTargetSelector(HabilidadData skill)
        {
            if (cachedAliados == null || cachedEnemigos == null) return;

            _habilidadSeleccionada = skill;
            SetState(CombatUIState.SelectingTarget);

            // Mostrar indicadores 3D sobre los objetivos válidos
            targetSelector?.Show(skill, cachedAliados, cachedEnemigos);

            // Mostrar instrucción en el HUD (reemplaza el panel TMP legacy)
            string instruccion = skill?.tipoObjetivo switch
            {
                TargetType.EnemigoUnico => "Selecciona un enemigo",
                TargetType.EnemigoTodos => "Selecciona un enemigo (afecta a todos)",
                TargetType.AliadoUnico  => "Selecciona un aliado",
                TargetType.AliadoTodos  => "Selecciona un aliado (afecta a todos)",
                _ => "Selecciona un objetivo"
            };
            hudController?.MostrarInstruccionObjetivo(instruccion);

            Debug.Log($"[CombatUI] Seleccionando objetivo para: {skill?.nombreHabilidad}. Click sobre el objetivo en escena.");
        }
        
        public void HideTargetSelector()
        {
            _habilidadSeleccionada = null;
            targetSelector?.Hide();

            // Notificar al HUD que salimos de selección de objetivo
            hudController?.MostrarInstruccionObjetivo(null);
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

            // Oculta el HUD. Llamado por OcultarHUD() (desde CombatFlowState.Exit())
            // y al inicializar en Start().
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

using System;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UnityEngine;

namespace GameFlow
{
    /// <summary>
    /// Orquestador central de modos globales del juego usando un stack.
    /// No contiene lógica de gameplay. Solo gestiona transiciones entre estados.
    /// 
    /// Uso:
    ///   GameFlowController.Instance.Push(new CombatFlowState());
    ///   GameFlowController.Instance.Pop();
    /// 
    /// Escucha eventos del EventBus para reaccionar a cambios de modo
    /// (ej: EventoEncounterIniciado → Push CombatFlowState).
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        private static GameFlowController _instance;
        public static GameFlowController Instance => _instance;

        private readonly Stack<IGameFlowState> _stateStack = new();

        /// <summary>Estado actual en el tope del stack (null si vacío).</summary>
        public IGameFlowState CurrentState => _stateStack.Count > 0 ? _stateStack.Peek() : null;

        /// <summary>Cantidad de estados en el stack.</summary>
        public int StackDepth => _stateStack.Count;

        /// <summary>Se dispara cada vez que cambia el estado activo.</summary>
        public event Action<IGameFlowState> OnStateChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Suscribirse a eventos que disparan transiciones
            EventBus.Suscribir<EventoEncounterIniciado>(OnEncounterStarted);
            EventBus.Suscribir<EventoCombateFinalizado>(OnCombatFinished);

            // Estado inicial según partida guardada
            if (_stateStack.Count == 0)
            {
                if (SaveSystem.ExisteGuardado("autosave"))
                {
                    PushInternal(new ExplorationFlowState(), skipValidation: true);
                    Debug.Log("[GameFlow] Estado inicial: Exploration (partida existente)");
                }
                else
                {
                    PushInternal(new CharacterSelectionFlowState(), skipValidation: true);
                    Debug.Log("[GameFlow] Estado inicial: CharacterSelection (nueva partida)");
                }
            }
        }

        private void OnDestroy()
        {
            EventBus.Desuscribir<EventoEncounterIniciado>(OnEncounterStarted);
            EventBus.Desuscribir<EventoCombateFinalizado>(OnCombatFinished);

            // Limpiar stack al destruir
            while (_stateStack.Count > 0)
            {
                var state = _stateStack.Pop();
                state.Exit();
            }

            if (_instance == this)
                _instance = null;
        }

        #endregion

        #region API Pública

        /// <summary>
        /// Apila un nuevo estado. Si BlocksLowerStates, el anterior recibe Exit().
        /// Valida que la transición esté permitida por el estado actual.
        /// </summary>
        public void Push(IGameFlowState state)
        {
            if (state == null)
            {
                Debug.LogWarning("[GameFlow] Push de estado null ignorado");
                return;
            }

            PushInternal(state, skipValidation: false);
        }

        /// <summary>
        /// Desapila el estado actual. El anterior (si existe) recibe Enter().
        /// No permite desapilar el último estado del stack.
        /// </summary>
        public void Pop()
        {
            if (_stateStack.Count <= 1)
            {
                Debug.LogWarning("[GameFlow] No se puede Pop: el stack tiene 0-1 estados");
                return;
            }

            var current = _stateStack.Pop();
            Debug.Log($"[GameFlow] Pop: {current.GetType().Name}");
            current.Exit();

            if (_stateStack.Count > 0)
            {
                var restored = _stateStack.Peek();
                Debug.Log($"[GameFlow] Restaurando: {restored.GetType().Name}");
                restored.Enter();
                NotifyStateChanged(restored);
            }
        }

        /// <summary>
        /// Reemplaza el estado actual por uno nuevo (Pop + Push atómico).
        /// Útil para transiciones directas como Exploration → Combat.
        /// </summary>
        public void Replace(IGameFlowState newState)
        {
            if (newState == null) return;

            if (_stateStack.Count > 0)
            {
                // Validar transición desde el estado actual
                if (!IsTransitionAllowed(newState))
                {
                    Debug.LogWarning($"[GameFlow] Replace a {newState.GetType().Name} bloqueado por {_stateStack.Peek().GetType().Name}");
                    return;
                }

                var current = _stateStack.Pop();
                Debug.Log($"[GameFlow] Replace: {current.GetType().Name} → {newState.GetType().Name}");
                current.Exit();
            }

            _stateStack.Push(newState);
            newState.Enter();
            NotifyStateChanged(newState);
        }

        /// <summary>
        /// Verifica si el estado actual es del tipo indicado.
        /// </summary>
        public bool IsInState<T>() where T : IGameFlowState
        {
            return _stateStack.Count > 0 && _stateStack.Peek() is T;
        }

        /// <summary>
        /// Verifica si un tipo de estado existe en algún lugar del stack.
        /// </summary>
        public bool HasStateInStack<T>() where T : IGameFlowState
        {
            return _stateStack.Any(s => s is T);
        }

        #endregion

        #region Lógica Interna

        private void PushInternal(IGameFlowState state, bool skipValidation)
        {
            if (!skipValidation && !IsTransitionAllowed(state))
            {
                Debug.LogWarning($"[GameFlow] Transición a {state.GetType().Name} bloqueada por {_stateStack.Peek().GetType().Name}");
                return;
            }

            if (_stateStack.Count > 0 && state.BlocksLowerStates)
            {
                _stateStack.Peek().Exit();
            }

            _stateStack.Push(state);
            state.Enter();
            Debug.Log($"[GameFlow] Push: {state.GetType().Name} (stack depth: {_stateStack.Count})");
            NotifyStateChanged(state);
        }

        private bool IsTransitionAllowed(IGameFlowState incoming)
        {
            if (_stateStack.Count == 0) return true;

            var current = _stateStack.Peek();
            var allowed = current.AllowedTransitions;

            if (allowed == null) return true;
            return allowed.Contains(incoming.GetType());
        }

        private void NotifyStateChanged(IGameFlowState newState)
        {
            OnStateChanged?.Invoke(newState);

            // Publicar evento en el EventBus para sistemas desacoplados
            EventBus.Publicar(new EventoGameFlowChanged
            {
                NuevoEstado = newState,
                TipoEstado = newState.GetType().Name
            });
        }

        #endregion

        #region Event Handlers

        private void OnEncounterStarted(EventoEncounterIniciado evento)
        {
            // Solo transicionar si no estamos ya en combate
            if (IsInState<CombatFlowState>())
            {
                Debug.Log("[GameFlow] Ya estamos en combate, ignorando nuevo encounter");
                return;
            }

            Push(new CombatFlowState());
        }

        private void OnCombatFinished(EventoCombateFinalizado evento)
        {
            if (!IsInState<CombatFlowState>())
            {
                Debug.LogWarning("[GameFlow] CombateFinalizado recibido pero no estamos en CombatFlowState");
                return;
            }

            Pop();
        }

        #endregion
    }
}

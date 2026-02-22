using UnityEngine;
using World.ChunkSystem;

namespace IA.Roaming
{
    /// <summary>
    /// Máquina de estados finita para el comportamiento de roaming de enemigos.
    /// Maneja estados: Idle, Patrolling, Resting, Alerted, Chasing.
    /// Se pausa cuando el enemigo entra en combate.
    /// </summary>
    public class EnemyRoamingFSM
    {
        // Referencias
        private EnemyController controller;
        private EnemySpawnConfig spawnConfig;
        private PlayerDetector playerDetector;
        
        // Estados
        private RoamingState currentState;
        private RoamingState previousState;
        
        // Estados disponibles
        private IdleState idleState;
        private PatrolState patrolState;
        private AlertedState alertedState;
        private ChasingState chasingState;
        private RestingState restingState;
        
        // Control
        private bool isActive = true;
        private bool showDebugLogs = false;
        
        // Propiedades públicas
        public RoamingState CurrentState => currentState;
        public bool IsActive => isActive;
        public PlayerDetector PlayerDetector => playerDetector;
        public EnemySpawnConfig SpawnConfig => spawnConfig;
        
        /// <summary>
        /// Constructor del FSM.
        /// </summary>
        public EnemyRoamingFSM(EnemyController controller, EnemySpawnConfig config, bool debugLogs = false)
        {
            this.controller = controller;
            this.spawnConfig = config;
            this.showDebugLogs = debugLogs;
            
            // Crear detector de jugador
            playerDetector = new PlayerDetector(controller, config);
            
            // Crear estados
            InitializeStates();
            
            // Establecer estado inicial basado en la configuración
            SetInitialState();
        }
        
        private void InitializeStates()
        {
            idleState = new IdleState();
            patrolState = new PatrolState();
            alertedState = new AlertedState();
            chasingState = new ChasingState();
            restingState = new RestingState();
            
            // Inicializar todos los estados
            idleState.Initialize(this, controller);
            patrolState.Initialize(this, controller);
            alertedState.Initialize(this, controller);
            chasingState.Initialize(this, controller);
            restingState.Initialize(this, controller);
        }
        
        private void SetInitialState()
        {
            RoamingState initialState = idleState;
            
            switch (spawnConfig.initialAIState)
            {
                case EnemyAIState.Idle:
                    initialState = idleState;
                    break;
                    
                case EnemyAIState.Patrolling:
                    initialState = patrolState;
                    break;
                    
                case EnemyAIState.Resting:
                    initialState = restingState;
                    break;
                    
                case EnemyAIState.Alerted:
                    initialState = alertedState;
                    break;
                    
                default:
                    initialState = idleState;
                    break;
            }
            
            ChangeState(initialState);
        }
        
        /// <summary>
        /// Actualiza el FSM. Llamar en Update().
        /// </summary>
        public void Update()
        {
            if (!isActive || currentState == null) return;
            
            // Actualizar detector de jugador
            playerDetector.Update();
            
            // Actualizar estado actual
            currentState.OnUpdate();
            
            // Verificar transiciones
            RoamingState nextState = currentState.CheckTransitions();
            if (nextState != null && nextState != currentState)
            {
                ChangeState(nextState);
            }
        }
        
        /// <summary>
        /// Actualiza física del FSM. Llamar en FixedUpdate().
        /// </summary>
        public void FixedUpdate()
        {
            if (!isActive || currentState == null) return;
            
            currentState.OnFixedUpdate();
        }
        
        /// <summary>
        /// Cambia al estado especificado.
        /// </summary>
        public void ChangeState(RoamingState newState)
        {
            if (newState == null || newState == currentState) return;
            
            // Salir del estado anterior
            if (currentState != null)
            {
                currentState.OnExit();
                previousState = currentState;
            }
            
            // Cambiar al nuevo estado
            currentState = newState;
            currentState.OnEnter();
            
            if (showDebugLogs)
            {
                Debug.Log($"[FSM {controller.name}] {previousState?.StateName ?? "None"} → {currentState.StateName}");
            }
        }
        
        /// <summary>
        /// Fuerza una transición a un estado específico por tipo.
        /// </summary>
        public void ForceState(EnemyAIState state)
        {
            RoamingState targetState = state switch
            {
                EnemyAIState.Idle => idleState,
                EnemyAIState.Patrolling => patrolState,
                EnemyAIState.Resting => restingState,
                EnemyAIState.Alerted => alertedState,
                EnemyAIState.Chasing => chasingState,
                _ => idleState
            };
            
            ChangeState(targetState);
        }
        
        /// <summary>
        /// Pausa el FSM (al entrar en combate).
        /// </summary>
        public void Pause()
        {
            if (!isActive) return;
            
            isActive = false;
            currentState?.OnExit();
            
            if (showDebugLogs)
            {
                Debug.Log($"[FSM {controller.name}] Pausado (entrando en combate)");
            }
        }
        
        /// <summary>
        /// Reanuda el FSM (al salir del combate).
        /// </summary>
        public void Resume()
        {
            if (isActive) return;
            
            isActive = true;
            
            // Volver al estado idle por defecto al salir de combate
            ChangeState(idleState);
            
            if (showDebugLogs)
            {
                Debug.Log($"[FSM {controller.name}] Reanudado (saliendo de combate)");
            }
        }
        
        /// <summary>
        /// Resetea el FSM al estado inicial.
        /// </summary>
        public void Reset()
        {
            playerDetector.Reset();
            SetInitialState();
        }
        
        /// <summary>
        /// Helper: Obtiene el estado Idle.
        /// </summary>
        public IdleState GetIdleState() => idleState;
        
        /// <summary>
        /// Helper: Obtiene el estado Patrol.
        /// </summary>
        public PatrolState GetPatrolState() => patrolState;
        
        /// <summary>
        /// Helper: Obtiene el estado Alerted.
        /// </summary>
        public AlertedState GetAlertedState() => alertedState;
        
        /// <summary>
        /// Helper: Obtiene el estado Chasing.
        /// </summary>
        public ChasingState GetChasingState() => chasingState;
    }
}

using UnityEngine;

namespace IA.Roaming
{
    /// <summary>
    /// Clase base para estados de roaming de enemigos.
    /// </summary>
    public abstract class RoamingState
    {
        protected EnemyRoamingFSM fsm;
        protected EnemyController controller;
        protected Transform transform;
        
        /// <summary>
        /// Configurar el estado con las referencias necesarias.
        /// </summary>
        public virtual void Initialize(EnemyRoamingFSM fsm, EnemyController controller)
        {
            this.fsm = fsm;
            this.controller = controller;
            this.transform = controller.transform;
        }
        
        /// <summary>
        /// Llamado al entrar al estado.
        /// </summary>
        public virtual void OnEnter()
        {
        }
        
        /// <summary>
        /// Llamado cada frame mientras el estado está activo.
        /// </summary>
        public virtual void OnUpdate()
        {
        }
        
        /// <summary>
        /// Llamado cada frame fijo mientras el estado está activo.
        /// </summary>
        public virtual void OnFixedUpdate()
        {
        }
        
        /// <summary>
        /// Llamado al salir del estado.
        /// </summary>
        public virtual void OnExit()
        {
        }
        
        /// <summary>
        /// Verifica si debe cambiar a otro estado.
        /// Override en estados que tengan transiciones específicas.
        /// </summary>
        public virtual RoamingState CheckTransitions()
        {
            return null; // null = no cambiar de estado
        }
        
        /// <summary>
        /// Nombre del estado para debugging.
        /// </summary>
        public virtual string StateName => GetType().Name;
    }
}

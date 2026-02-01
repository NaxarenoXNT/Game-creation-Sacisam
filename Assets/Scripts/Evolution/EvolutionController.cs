using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Evolution
{
    /// <summary>
    /// MonoBehaviour puente: gestiona el sistema de evolución.
    /// - Escucha eventos del EventBus para actualizar EvolutionState
    /// - Evalúa qué traits/evoluciones están disponibles
    /// - Genera ofertas de evolución
    /// - Aplica efectos cuando el jugador elige
    /// </summary>
    public class EvolutionController : MonoBehaviour
    {
        [Header("Datos")]
        [Tooltip("Todas las definiciones de evolución del juego")]
        public List<ClassEvolutionDefinition> evoluciones = new List<ClassEvolutionDefinition>();
        
        [Tooltip("Todos los traits disponibles")]
        public List<TraitDefinition> traits = new List<TraitDefinition>();
        
        [Tooltip("Árboles de evolución por clase")]
        public List<EvolutionBranch> branches = new List<EvolutionBranch>();

        [Header("Referencia al Jugador")]
        [Tooltip("Referencia al ClaseData actual del jugador")]
        public ClaseData claseActual;
        
        [Tooltip("Transform del jugador para buscar componentes")]
        public Transform jugadorTransform;

        [Header("Config")]
        public int seed;
        public int ofertaSize = 3;
        
        [Tooltip("Mostrar información de debug en consola")]
        public bool debugMode = true;

        // Estado y componentes internos
        private EvolutionState state;
        private EvolutionEvaluator evaluator;
        private EvolutionApplier applier;
        private EvolutionRoller roller;

        // Cache de jugador
        private Padres.Jugador jugadorCache;

        #region Unity Lifecycle

        private void Awake()
        {
            state = new EvolutionState { seed = seed };
            evaluator = new EvolutionEvaluator();
            applier = new EvolutionApplier();
            roller = new EvolutionRoller(seed);
        }

        private void Start()
        {
            SuscribirEventos();
            SincronizarConJugador();
        }

        private void OnDestroy()
        {
            DesuscribirEventos();
        }

        #endregion

        #region Integración con EventBus

        private void SuscribirEventos()
        {
            // Suscribirse a eventos del EventBus
            EventBus.Suscribir<EventoHabilidadUsada>(HandleHabilidadUsadaEvento);
            EventBus.Suscribir<EventoHabilidadDesbloqueada>(HandleHabilidadDesbloqueada);
            EventBus.Suscribir<EventoHabilidadRemovida>(HandleHabilidadRemovida);
            EventBus.Suscribir<EventoPasivaDesbloqueada>(HandlePasivaDesbloqueada);
            EventBus.Suscribir<EventoPasivaRemovida>(HandlePasivaRemovida);
            EventBus.Suscribir<EventoMuerte>(HandleMuerte);
            EventBus.Suscribir<EventoDanoRecibido>(HandleDano);
            EventBus.Suscribir<EventoCuracion>(HandleCuracion);
            
            // TODO: Agregar más suscripciones según necesites
            // EventBus.Suscribir<EventoMisionCompletada>(HandleMision);
        }

        private void DesuscribirEventos()
        {
            EventBus.Desuscribir<EventoHabilidadUsada>(HandleHabilidadUsadaEvento);
            EventBus.Desuscribir<EventoHabilidadDesbloqueada>(HandleHabilidadDesbloqueada);
            EventBus.Desuscribir<EventoHabilidadRemovida>(HandleHabilidadRemovida);
            EventBus.Desuscribir<EventoPasivaDesbloqueada>(HandlePasivaDesbloqueada);
            EventBus.Desuscribir<EventoPasivaRemovida>(HandlePasivaRemovida);
            EventBus.Desuscribir<EventoMuerte>(HandleMuerte);
            EventBus.Desuscribir<EventoDanoRecibido>(HandleDano);
            EventBus.Desuscribir<EventoCuracion>(HandleCuracion);
        }

        #endregion

        #region Handlers de Eventos del EventBus

        private void HandleHabilidadUsadaEvento(EventoHabilidadUsada evento)
        {
            if (evento.Habilidad != null)
            {
                state.RegistrarUsoHabilidad(evento.Habilidad.name);
                if (debugMode) Debug.Log($"[Evolution] Uso registrado: {evento.Habilidad.nombreHabilidad}");
            }
        }

        private void HandleHabilidadDesbloqueada(EventoHabilidadDesbloqueada evento)
        {
            if (evento.Habilidad != null)
            {
                state.RegistrarHabilidadDesbloqueada(evento.Habilidad.name);
                if (debugMode) Debug.Log($"[Evolution] Habilidad desbloqueada: {evento.Habilidad.nombreHabilidad}");
            }
        }

        private void HandleHabilidadRemovida(EventoHabilidadRemovida evento)
        {
            if (evento.Habilidad != null)
            {
                state.RemoverHabilidadDesbloqueada(evento.Habilidad.name);
                if (debugMode) Debug.Log($"[Evolution] Habilidad removida: {evento.Habilidad.nombreHabilidad}");
            }
        }

        private void HandlePasivaDesbloqueada(EventoPasivaDesbloqueada evento)
        {
            // Opcionalmente trackear pasivas también
            if (debugMode && evento.Pasiva != null)
                Debug.Log($"[Evolution] Pasiva desbloqueada: {evento.Pasiva.nombrePasiva}");
        }

        private void HandlePasivaRemovida(EventoPasivaRemovida evento)
        {
            if (debugMode && evento.Pasiva != null)
                Debug.Log($"[Evolution] Pasiva removida: {evento.Pasiva.nombrePasiva}");
        }

        private void HandleMuerte(EventoMuerte evento)
        {
            if (evento.Entidad != null && evento.Asesino != null)
            {
                // Si el jugador mató a algo
                state.RegistrarKill(evento.Entidad.TipoEntidad);
            }
        }

        private void HandleDano(EventoDanoRecibido evento)
        {
            if (evento.Atacante != null)
            {
                state.RegistrarDaño(evento.Cantidad, 0);
            }
        }

        private void HandleCuracion(EventoCuracion evento)
        {
            state.RegistrarCuracion(evento.Cantidad);
        }

        // Métodos públicos para llamar manualmente si es necesario
        public void HandleEnemigoDerrotado(Flags.TipoEntidades tipo)
        {
            state.RegistrarKill(tipo);
            if (debugMode) Debug.Log($"[Evolution] Kill registrado: {tipo}");
        }

        public void HandleMisionCompletada(string misionId)
        {
            state.RegistrarMision(misionId);
        }

        public void HandleKarmaModificado(float nuevoKarma)
        {
            state.karma = nuevoKarma;
        }

        public void HandleNivelSubido(int nuevoNivel)
        {
            state.nivelJugador = nuevoNivel;
        }

        public void HandleBiomaEntrado(string biomaId)
        {
            state.RegistrarBioma(biomaId);
        }

        public void HandleEstadoAplicado(string statusId)
        {
            state.RegistrarEstadoAplicado(statusId);
        }

        public void HandleDaño(int infligido, int recibido)
        {
            state.RegistrarDaño(infligido, recibido);
        }

        #endregion

        #region API Pública para Evoluciones

        /// <summary>
        /// Genera una oferta de evolución/traits disponibles para el jugador.
        /// </summary>
        public List<IEvolutionOption> GenerarOferta()
        {
            var disponibles = new List<IEvolutionOption>();
            
            // Filtrar evoluciones disponibles
            var evosDisp = evaluator.FiltrarEvolucionesDisponibles(evoluciones, state, claseActual);
            disponibles.AddRange(evosDisp.ConvertAll<IEvolutionOption>(Wrap));
            
            // Filtrar traits disponibles
            var traitsDisp = evaluator.FiltrarTraitsDisponibles(traits, state, claseActual);
            disponibles.AddRange(traitsDisp.ConvertAll<IEvolutionOption>(Wrap));

            if (debugMode)
            {
                Debug.Log($"[Evolution] Evoluciones disponibles: {evosDisp.Count}, Traits disponibles: {traitsDisp.Count}");
            }

            return roller.RolarOferta(disponibles, ofertaSize);
        }

        /// <summary>
        /// Aplica la opción de evolución/trait seleccionada.
        /// </summary>
        public void AplicarOpcion(IEvolutionOption opcion)
        {
            var jugador = ObtenerJugador();
            if (jugador == null)
            {
                Debug.LogError("[Evolution] No se pudo encontrar al jugador para aplicar efectos");
                return;
            }

            switch (opcion)
            {
                case EvolutionOptionWrapper wrapper when wrapper.Ref is ClassEvolutionDefinition evo:
                    AplicarEvolucion(evo, jugador);
                    break;
                    
                case EvolutionOptionWrapper wrapper when wrapper.Ref is TraitDefinition trait:
                    AplicarTrait(trait, jugador);
                    break;
            }
        }

        private void AplicarEvolucion(ClassEvolutionDefinition evo, Padres.Jugador jugador)
        {
            state.AñadirEvolucion(evo.id);
            
            // Cambiar clase si tiene destino definido
            if (evo.claseDestino != null)
            {
                claseActual = evo.claseDestino;
                // TODO: Llamar a tu sistema de cambio de clase
                // jugador.CambiarClase(evo.claseDestino);
            }
            
            // Aplicar efectos
            foreach (var eff in evo.efectos)
                applier.Aplicar(eff, jugador);

            if (debugMode)
                Debug.Log($"[Evolution] Evolución aplicada: {evo.nombreMostrar ?? evo.id}");
        }

        private void AplicarTrait(TraitDefinition trait, Padres.Jugador jugador)
        {
            state.AñadirTrait(trait.id);
            
            // Aplicar efectos
            foreach (var eff in trait.efectos)
                applier.Aplicar(eff, jugador);

            if (debugMode)
                Debug.Log($"[Evolution] Trait obtenido: {trait.nombreMostrar ?? trait.id} (x{state.traitStacks[trait.id]})");
        }

        /// <summary>
        /// Obtiene los traits que el jugador tiene actualmente.
        /// </summary>
        public List<(TraitDefinition trait, int stacks)> ObtenerTraitsActuales()
        {
            var resultado = new List<(TraitDefinition, int)>();
            foreach (var kvp in state.traitStacks)
            {
                var trait = traits.Find(t => t.id == kvp.Key);
                if (trait != null)
                    resultado.Add((trait, kvp.Value));
            }
            return resultado;
        }

        /// <summary>
        /// Obtiene las evoluciones que el jugador ha desbloqueado.
        /// </summary>
        public List<ClassEvolutionDefinition> ObtenerEvolucionesAplicadas()
        {
            var resultado = new List<ClassEvolutionDefinition>();
            foreach (var id in state.evolucionesAplicadas)
            {
                var evo = evoluciones.Find(e => e.id == id);
                if (evo != null)
                    resultado.Add(evo);
            }
            return resultado;
        }

        /// <summary>
        /// Verifica el progreso de una condición específica.
        /// Útil para mostrar en UI.
        /// </summary>
        public float ObtenerProgresoCondicion(EvolutionConditionSO cond)
        {
            if (cond == null) return 0f;
            return cond.GetProgreso(state);
        }

        #endregion

        #region Persistencia

        /// <summary>
        /// Obtiene el estado para guardado.
        /// </summary>
        public EvolutionState ObtenerEstado() => state;

        /// <summary>
        /// Carga un estado guardado.
        /// </summary>
        public void CargarEstado(EvolutionState estadoGuardado)
        {
            state = estadoGuardado ?? new EvolutionState { seed = seed };
            roller = new EvolutionRoller(state.seed);
        }

        #endregion

        #region Utilidades Privadas

        private void SincronizarConJugador()
        {
            var jugador = ObtenerJugador();
            if (jugador == null) return;
            
            // Jugador expone nivel como Nivel_Entidad (heredado de Entidad)
            state.nivelJugador = jugador.Nivel_Entidad;
            // TODO: Sincronizar más datos iniciales si es necesario
        }

        private Padres.Jugador ObtenerJugador()
        {
            if (jugadorCache != null) return jugadorCache;
            return jugadorCache;
        }

        /// <summary>
        /// Inyecta la instancia de Jugador creada por tu sistema de clases.
        /// Llamar después de instanciar la clase (ClaseData.CrearInstancia()).
        /// </summary>
        public void AsignarJugador(Padres.Jugador jugador)
        {
            jugadorCache = jugador;
        }

        private IEvolutionOption Wrap(ClassEvolutionDefinition def) => new EvolutionOptionWrapper(def);
        private IEvolutionOption Wrap(TraitDefinition def) => new EvolutionOptionWrapper(def);

        private class EvolutionOptionWrapper : IEvolutionOption
        {
            public string Id { get; }
            public float PesoOferta { get; }
            public object Ref { get; }
            
            public EvolutionOptionWrapper(ClassEvolutionDefinition def) 
            { 
                Id = def.id; 
                PesoOferta = def.pesoOferta; 
                Ref = def; 
            }
            
            public EvolutionOptionWrapper(TraitDefinition def) 
            { 
                Id = def.id; 
                PesoOferta = def.pesoOferta; 
                Ref = def; 
            }
        }

        #endregion
    }
}
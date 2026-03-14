using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Managers;
using Missions.Objectives;
using Evolution;

namespace Missions
{
    /// <summary>
    /// Orquestador global del sistema de misiones.
    /// 
    /// Responsabilidades:
    /// - Gestiona misiones GLOBALES (cualquier personaje contribuye).
    /// - Gestiona misiones PERSONALES por personaje (cada pj tiene las suyas).
    /// - Gestiona misiones EXCLUSIVAS (globales hasta aceptarse, luego personales).
    /// - Enruta eventos (kills, traits, etc.) al personaje correcto.
    /// - Previene duplicados y regula estados compartidos vía GlobalPlayerState.
    /// 
    /// Arquitectura:
    /// - 100% Event-Driven: NO usa Update/FixedUpdate.
    /// - Data-Driven: toda la info de misiones vive en ScriptableObjects.
    /// - Cada personaje se registra con su EvolutionState para evaluación individual.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        [Header("Datos")]
        [Tooltip("Todas las definiciones de misión del juego")]
        public List<MissionDefinitionSO> todasLasMisiones = new List<MissionDefinitionSO>();

        [Header("Debug")]
        public bool debugMode = true;

        // ========== Estado Global ==========
        private GlobalPlayerState globalState;

        // ========== Misiones Globales (compartidas) ==========
        private readonly Dictionary<string, MissionInstance> misionesGlobalesActivas = new();
        private readonly HashSet<string> misionesGlobalesDisponibles = new();

        // ========== Datos Per-Personaje ==========
        /// <summary>characterId → datos de misión del personaje.</summary>
        private readonly Dictionary<string, CharacterMissionData> datosPersonajes = new();

        /// <summary>characterId → EvolutionState del personaje (inyectado al registrar).</summary>
        private readonly Dictionary<string, EvolutionState> estadosPersonajes = new();

        // ========== Internos ==========
        private MissionEvaluator evaluator;

        #region Unity Lifecycle

        private void Awake()
        {
            evaluator = new MissionEvaluator();
        }

        private void Start()
        {
            SuscribirEventos();
        }

        private void OnDestroy()
        {
            DesuscribirEventos();
        }

        #endregion

        #region Inicialización

        /// <summary>
        /// Inicializa el orquestador con el estado global del jugador.
        /// Llamar antes de registrar personajes.
        /// </summary>
        public void Inicializar(GlobalPlayerState global)
        {
            globalState = global ?? new GlobalPlayerState();
        }

        /// <summary>
        /// Registra un personaje en el sistema de misiones.
        /// Cada personaje necesita su propio EvolutionState para evaluar condiciones individualmente.
        /// </summary>
        public void RegistrarPersonaje(string characterId, EvolutionState state)
        {
            if (string.IsNullOrEmpty(characterId) || state == null) return;

            estadosPersonajes[characterId] = state;

            if (!datosPersonajes.ContainsKey(characterId))
                datosPersonajes[characterId] = new CharacterMissionData(characterId);

            if (debugMode)
                Debug.Log($"[MissionManager] Personaje registrado: {characterId}");

            RevaluarMisionesPersonaje(characterId);
            RevaluarMisionesGlobales();
        }

        /// <summary>
        /// Desregistra un personaje (sale del party activo, stasis, etc.).
        /// Los datos de misiones se preservan para cuando vuelva.
        /// </summary>
        public void DesregistrarPersonaje(string characterId)
        {
            estadosPersonajes.Remove(characterId);
            // datosPersonajes NO se borra — las misiones persisten aunque el pj esté inactivo.
            if (debugMode)
                Debug.Log($"[MissionManager] Personaje desregistrado: {characterId}");
        }

        #endregion

        #region Suscripción a Eventos

        private void SuscribirEventos()
        {
            EventBus.Suscribir<EventoMuerte>(HandleMuerte);
            EventBus.Suscribir<EventoNivelSubido>(HandleNivelSubido);
            EventBus.Suscribir<EventoTraitObtenido>(HandleTraitObtenido);
            EventBus.Suscribir<EventoEvolucionAplicada>(HandleEvolucionAplicada);
            EventBus.Suscribir<EventoMisionCompletada>(HandleMisionCompletadaInterna);
        }

        private void DesuscribirEventos()
        {
            EventBus.Desuscribir<EventoMuerte>(HandleMuerte);
            EventBus.Desuscribir<EventoNivelSubido>(HandleNivelSubido);
            EventBus.Desuscribir<EventoTraitObtenido>(HandleTraitObtenido);
            EventBus.Desuscribir<EventoEvolucionAplicada>(HandleEvolucionAplicada);
            EventBus.Desuscribir<EventoMisionCompletada>(HandleMisionCompletadaInterna);
        }

        #endregion

        #region Handlers de Eventos

        private void HandleMuerte(EventoMuerte evento)
        {
            if (evento.Entidad == null || evento.Asesino == null) return;

            var tipoEntidad = evento.Entidad.TipoEntidad;

            // Obtener personajes participantes del combate.
            // Todos los personajes activos en party reciben crédito por kills en combate.
            var participantes = ObtenerParticipantesCombate();

            // Propagar kill a misiones PERSONALES de cada participante
            foreach (var charId in participantes)
            {
                if (!datosPersonajes.TryGetValue(charId, out var datos)) continue;

                foreach (var kvp in datos.misionesActivas)
                {
                    var instancia = kvp.Value;
                    if (instancia.status != MissionStatus.Active) continue;
                    PropaglarKillAInstancia(instancia, tipoEntidad);
                    VerificarCompletitudMision(instancia, charId);
                }
            }

            // Propagar kill a misiones GLOBALES activas
            foreach (var kvp in misionesGlobalesActivas)
            {
                var instancia = kvp.Value;
                if (instancia.status != MissionStatus.Active) continue;
                PropaglarKillAInstancia(instancia, tipoEntidad);
                VerificarCompletitudMisionGlobal(instancia);
            }

            RevaluarTodasLasMisiones();
        }

        private void HandleNivelSubido(EventoNivelSubido evento)
        {
            RevaluarTodasLasMisiones();
        }

        private void HandleTraitObtenido(EventoTraitObtenido evento)
        {
            // Un trait bloqueado globalmente se registra en GlobalPlayerState
            if (evento.EsGlobalmenteUnico && globalState != null)
            {
                globalState.BloquearTraitGlobal(evento.TraitId, evento.CharacterId);
            }

            // Re-evaluar misiones del personaje que obtuvo el trait
            if (!string.IsNullOrEmpty(evento.CharacterId))
                RevaluarMisionesPersonaje(evento.CharacterId);

            // Misiones globales también pueden depender de traits
            RevaluarMisionesGlobales();
        }

        private void HandleEvolucionAplicada(EventoEvolucionAplicada evento)
        {
            if (!string.IsNullOrEmpty(evento.CharacterId))
                RevaluarMisionesPersonaje(evento.CharacterId);

            RevaluarMisionesGlobales();
        }

        private void HandleMisionCompletadaInterna(EventoMisionCompletada evento)
        {
            // Completar una misión puede desbloquear otras (cadenas de misiones)
            RevaluarTodasLasMisiones();
        }

        #endregion

        #region Notificaciones Externas

        /// <summary>
        /// Notifica que un personaje llegó a una zona.
        /// Propaga a misiones personales del personaje y a misiones globales.
        /// </summary>
        public void NotificarZonaAlcanzada(string zonaId, string characterId)
        {
            // Misiones personales del personaje
            if (!string.IsNullOrEmpty(characterId) &&
                datosPersonajes.TryGetValue(characterId, out var datos))
            {
                foreach (var kvp in datos.misionesActivas)
                {
                    var instancia = kvp.Value;
                    if (instancia.status != MissionStatus.Active) continue;
                    PropaglarZonaAInstancia(instancia, zonaId);
                    VerificarCompletitudMision(instancia, characterId);
                }
            }

            // Misiones globales
            foreach (var kvp in misionesGlobalesActivas)
            {
                var instancia = kvp.Value;
                if (instancia.status != MissionStatus.Active) continue;
                PropaglarZonaAInstancia(instancia, zonaId);
                VerificarCompletitudMisionGlobal(instancia);
            }
        }

        /// <summary>
        /// Notifica que un personaje obtuvo un item.
        /// Propaga a misiones personales y globales.
        /// </summary>
        public void NotificarItemObtenido(string itemId, int cantidad, string characterId)
        {
            // Misiones personales
            if (!string.IsNullOrEmpty(characterId) &&
                datosPersonajes.TryGetValue(characterId, out var datos))
            {
                foreach (var kvp in datos.misionesActivas)
                {
                    var instancia = kvp.Value;
                    if (instancia.status != MissionStatus.Active) continue;
                    PropaglarItemAInstancia(instancia, itemId, cantidad);
                    VerificarCompletitudMision(instancia, characterId);
                }
            }

            // Misiones globales
            foreach (var kvp in misionesGlobalesActivas)
            {
                var instancia = kvp.Value;
                if (instancia.status != MissionStatus.Active) continue;
                PropaglarItemAInstancia(instancia, itemId, cantidad);
                VerificarCompletitudMisionGlobal(instancia);
            }
        }

        /// <summary>
        /// Fuerza revaluación de todas las misiones.
        /// Usar cuando algo cambia fuera de los eventos estándar.
        /// </summary>
        public void ForzarRevaluacion()
        {
            RevaluarTodasLasMisiones();
        }

        #endregion

        #region Evaluación de Disponibilidad

        private void RevaluarTodasLasMisiones()
        {
            RevaluarMisionesGlobales();
            // Copiar keys para evitar invalidar el enumerador si se modifica durante iteración
            var charIds = estadosPersonajes.Keys.ToList();
            foreach (var charId in charIds)
                RevaluarMisionesPersonaje(charId);
        }

        /// <summary>
        /// Evalúa misiones de scope Global y Exclusive (las que aún no están asignadas).
        /// Una misión global se desbloquea si ALGÚN personaje registrado cumple las condiciones.
        /// </summary>
        private void RevaluarMisionesGlobales()
        {
            if (globalState == null) return;

            foreach (var def in todasLasMisiones)
            {
                if (def == null) continue;
                if (def.scope == MissionScope.Personal) continue;

                string id = def.misionId;

                // Skip si ya completada, fallida o activa globalmente
                if (globalState.misionesGlobalesCompletadas.Contains(id)) continue;
                if (globalState.misionesGlobalesFallidas.Contains(id)) continue;
                if (misionesGlobalesActivas.ContainsKey(id)) continue;

                // Misiones exclusivas ya asignadas se manejan como personales
                if (def.scope == MissionScope.Exclusive &&
                    globalState.misionesExclusivasAsignadas.ContainsKey(id))
                    continue;

                // Verificar si ALGÚN personaje registrado cumple las condiciones
                bool disponible = false;
                foreach (var kvp in estadosPersonajes)
                {
                    if (def.CumpleCondicionesDesbloqueo(kvp.Value))
                    {
                        disponible = true;
                        break;
                    }
                }

                if (disponible && misionesGlobalesDisponibles.Add(id))
                {
                    if (debugMode)
                        Debug.Log($"[MissionManager] Misión {def.scope} disponible: {def.nombreMostrar ?? id}");

                    EventBus.Publicar(new EventoMisionDisponible { Mision = def });

                    if (def.autoAceptar && def.scope == MissionScope.Global)
                        AceptarMisionGlobal(id);
                }
            }
        }

        /// <summary>
        /// Evalúa misiones de scope Personal para un personaje específico.
        /// También evalúa misiones Exclusive ya asignadas a este personaje.
        /// </summary>
        private void RevaluarMisionesPersonaje(string characterId)
        {
            if (!estadosPersonajes.TryGetValue(characterId, out var state)) return;
            if (!datosPersonajes.TryGetValue(characterId, out var datos)) return;

            foreach (var def in todasLasMisiones)
            {
                if (def == null) continue;

                string id = def.misionId;

                // Misiones personales: evaluar solo para este personaje
                if (def.scope == MissionScope.Personal)
                {
                    if (datos.misionesCompletadas.Contains(id)) continue;
                    if (datos.misionesActivas.ContainsKey(id)) continue;

                    if (def.CumpleCondicionesDesbloqueo(state))
                    {
                        if (datos.misionesDisponibles.Add(id))
                        {
                            if (debugMode)
                                Debug.Log($"[MissionManager] Misión personal disponible para {characterId}: {def.nombreMostrar ?? id}");

                            EventBus.Publicar(new EventoMisionDisponible { Mision = def });

                            if (def.autoAceptar)
                                AceptarMisionPersonal(id, characterId);
                        }
                    }
                }
                // Misiones exclusivas asignadas a este personaje: tratar como personal
                else if (def.scope == MissionScope.Exclusive)
                {
                    var dueño = globalState?.GetDueñoMisionExclusiva(id);
                    if (dueño != characterId) continue;

                    if (datos.misionesCompletadas.Contains(id)) continue;
                    if (datos.misionesActivas.ContainsKey(id)) continue;

                    // Ya está asignada, el personaje puede progresar
                    datos.misionesDisponibles.Add(id);
                }
            }
        }

        #endregion

        #region Aceptar Misiones

        /// <summary>
        /// Acepta una misión GLOBAL. Cualquier personaje puede contribuir progreso.
        /// </summary>
        public bool AceptarMisionGlobal(string misionId)
        {
            if (string.IsNullOrEmpty(misionId)) return false;
            if (misionesGlobalesActivas.ContainsKey(misionId)) return false;
            if (!misionesGlobalesDisponibles.Contains(misionId)) return false;

            var def = BuscarDefinicion(misionId);
            if (def == null || def.scope == MissionScope.Personal) return false;

            var instancia = new MissionInstance(def);
            misionesGlobalesActivas[misionId] = instancia;

            if (debugMode)
                Debug.Log($"[MissionManager] Misión global aceptada: {def.nombreMostrar ?? misionId}");

            EventBus.Publicar(new EventoMisionAceptada { Instancia = instancia });
            VerificarCompletitudMisionGlobal(instancia);
            return true;
        }

        /// <summary>
        /// Acepta una misión PERSONAL para un personaje específico.
        /// Solo ese personaje puede progresar y completar la misión.
        /// </summary>
        public bool AceptarMisionPersonal(string misionId, string characterId)
        {
            if (string.IsNullOrEmpty(misionId) || string.IsNullOrEmpty(characterId)) return false;
            if (!datosPersonajes.TryGetValue(characterId, out var datos)) return false;
            if (datos.misionesActivas.ContainsKey(misionId)) return false;
            if (!datos.misionesDisponibles.Contains(misionId)) return false;

            var def = BuscarDefinicion(misionId);
            if (def == null) return false;

            var instancia = new MissionInstance(def);
            datos.misionesActivas[misionId] = instancia;

            if (debugMode)
                Debug.Log($"[MissionManager] Misión personal aceptada por {characterId}: {def.nombreMostrar ?? misionId}");

            EventBus.Publicar(new EventoMisionAceptada { Instancia = instancia });
            VerificarCompletitudMision(instancia, characterId);
            return true;
        }

        /// <summary>
        /// Acepta una misión EXCLUSIVE con un personaje específico.
        /// La misión se bloquea a ese personaje permanentemente.
        /// </summary>
        public bool AceptarMisionExclusiva(string misionId, string characterId)
        {
            if (string.IsNullOrEmpty(misionId) || string.IsNullOrEmpty(characterId)) return false;
            if (globalState == null) return false;

            // Verificar que no esté ya asignada a otro personaje
            var dueñoActual = globalState.GetDueñoMisionExclusiva(misionId);
            if (dueñoActual != null && dueñoActual != characterId) return false;

            if (!misionesGlobalesDisponibles.Contains(misionId)) return false;

            var def = BuscarDefinicion(misionId);
            if (def == null || def.scope != MissionScope.Exclusive) return false;

            // Asignar exclusivamente a este personaje
            globalState.AsignarMisionExclusiva(misionId, characterId);
            misionesGlobalesDisponibles.Remove(misionId);

            // Crear datos de personaje si no existen
            if (!datosPersonajes.ContainsKey(characterId))
                datosPersonajes[characterId] = new CharacterMissionData(characterId);

            var datos = datosPersonajes[characterId];
            var instancia = new MissionInstance(def);
            datos.misionesActivas[misionId] = instancia;

            if (debugMode)
                Debug.Log($"[MissionManager] Misión exclusiva asignada a {characterId}: {def.nombreMostrar ?? misionId}");

            EventBus.Publicar(new EventoMisionAceptada { Instancia = instancia });
            VerificarCompletitudMision(instancia, characterId);
            return true;
        }

        #endregion

        #region Completar / Fallar Misiones

        private void VerificarCompletitudMision(MissionInstance instancia, string characterId)
        {
            if (instancia.status != MissionStatus.Active) return;
            if (!instancia.TodosObjetivosObligatoriosCompletos()) return;

            CompletarMisionPersonal(instancia, characterId);
        }

        private void VerificarCompletitudMisionGlobal(MissionInstance instancia)
        {
            if (instancia.status != MissionStatus.Active) return;
            if (!instancia.TodosObjetivosObligatoriosCompletos()) return;

            CompletarMisionGlobal(instancia);
        }

        private void CompletarMisionPersonal(MissionInstance instancia, string characterId)
        {
            instancia.status = MissionStatus.Completed;
            string id = instancia.definition.misionId;

            if (datosPersonajes.TryGetValue(characterId, out var datos))
            {
                datos.misionesActivas.Remove(id);
                datos.misionesCompletadas.Add(id);
                datos.misionesDisponibles.Remove(id);
            }

            // Registrar en el EvolutionState del personaje para que MisionConditionSO funcione
            if (estadosPersonajes.TryGetValue(characterId, out var state))
                state.RegistrarMision(id);

            if (debugMode)
                Debug.Log($"[MissionManager] Misión personal completada por {characterId}: " +
                          $"{instancia.definition.nombreMostrar ?? id}");

            EventBus.Publicar(new EventoMisionCompletada
            {
                Instancia = instancia,
                Recompensas = instancia.definition.recompensas
            });
        }

        private void CompletarMisionGlobal(MissionInstance instancia)
        {
            instancia.status = MissionStatus.Completed;
            string id = instancia.definition.misionId;

            misionesGlobalesActivas.Remove(id);
            misionesGlobalesDisponibles.Remove(id);
            globalState?.RegistrarMisionGlobalCompletada(id);

            // Registrar en EvolutionState de TODOS los personajes registrados
            // para que MisionConditionSO (cadenas) funcione sin importar qué pj se evalúe
            foreach (var kvp in estadosPersonajes)
                kvp.Value.RegistrarMision(id);

            if (debugMode)
                Debug.Log($"[MissionManager] Misión global completada: " +
                          $"{instancia.definition.nombreMostrar ?? id}");

            EventBus.Publicar(new EventoMisionCompletada
            {
                Instancia = instancia,
                Recompensas = instancia.definition.recompensas
            });
        }

        /// <summary>Falla una misión personal de un personaje.</summary>
        public bool FallarMision(string misionId, string characterId, string razon = null)
        {
            if (!string.IsNullOrEmpty(characterId) &&
                datosPersonajes.TryGetValue(characterId, out var datos) &&
                datos.misionesActivas.TryGetValue(misionId, out var instancia))
            {
                instancia.status = MissionStatus.Failed;
                datos.misionesActivas.Remove(misionId);
                datos.misionesFallidas.Add(misionId);
                datos.misionesDisponibles.Remove(misionId);

                EventBus.Publicar(new EventoMisionFallida { Instancia = instancia, Razon = razon });
                return true;
            }
            return false;
        }

        /// <summary>Falla una misión global.</summary>
        public bool FallarMisionGlobal(string misionId, string razon = null)
        {
            if (misionesGlobalesActivas.TryGetValue(misionId, out var instancia))
            {
                instancia.status = MissionStatus.Failed;
                misionesGlobalesActivas.Remove(misionId);
                misionesGlobalesDisponibles.Remove(misionId);
                globalState?.RegistrarMisionGlobalFallida(misionId);

                EventBus.Publicar(new EventoMisionFallida { Instancia = instancia, Razon = razon });
                return true;
            }
            return false;
        }

        #endregion

        #region API de Consulta

        /// <summary>Obtiene las misiones activas de un personaje (personales + exclusivas asignadas).</summary>
        public IReadOnlyDictionary<string, MissionInstance> GetMisionesActivasPersonaje(string characterId)
        {
            return datosPersonajes.TryGetValue(characterId, out var datos) ? datos.misionesActivas : null;
        }

        /// <summary>Obtiene las misiones globales activas.</summary>
        public IReadOnlyDictionary<string, MissionInstance> GetMisionesGlobalesActivas() => misionesGlobalesActivas;

        /// <summary>Obtiene IDs de misiones disponibles para un personaje (personales).</summary>
        public IReadOnlyCollection<string> GetMisionesDisponiblesPersonaje(string characterId)
        {
            return datosPersonajes.TryGetValue(characterId, out var datos) ? datos.misionesDisponibles : null;
        }

        /// <summary>Obtiene IDs de misiones globales/exclusivas disponibles.</summary>
        public IReadOnlyCollection<string> GetMisionesGlobalesDisponibles() => misionesGlobalesDisponibles;

        /// <summary>IDs de misiones completadas por un personaje.</summary>
        public IReadOnlyCollection<string> GetMisionesCompletadasPersonaje(string characterId)
        {
            return datosPersonajes.TryGetValue(characterId, out var datos) ? datos.misionesCompletadas : null;
        }

        /// <summary>IDs de misiones globales completadas.</summary>
        public IReadOnlyCollection<string> GetMisionesGlobalesCompletadas()
        {
            return globalState?.misionesGlobalesCompletadas;
        }

        /// <summary>Busca una definición de misión por ID.</summary>
        public MissionDefinitionSO BuscarDefinicion(string misionId)
        {
            return todasLasMisiones.Find(m => m != null && m.misionId == misionId);
        }

        /// <summary>Verifica si una misión está completada (global o por un personaje específico).</summary>
        public bool EsMisionCompletada(string misionId, string characterId = null)
        {
            // Verificar completación global
            if (globalState != null && globalState.EsMisionGlobalCompletada(misionId))
                return true;

            // Verificar completación personal
            if (!string.IsNullOrEmpty(characterId) &&
                datosPersonajes.TryGetValue(characterId, out var datos))
                return datos.CompletoMision(misionId);

            return false;
        }

        /// <summary>Obtiene todos los IDs de personajes registrados.</summary>
        public IReadOnlyCollection<string> GetPersonajesRegistrados() => estadosPersonajes.Keys.ToList();

        #endregion

        #region Persistencia

        public MissionSaveData ObtenerDatosGuardado()
        {
            var data = new MissionSaveData();

            // Globales
            if (globalState != null)
            {
                data.globalesCompletadas = new List<string>(globalState.misionesGlobalesCompletadas);
                data.globalesFallidas = new List<string>(globalState.misionesGlobalesFallidas);
                data.exclusivasAsignadas = new List<MissionExclusiveAssignment>();
                foreach (var kvp in globalState.misionesExclusivasAsignadas)
                    data.exclusivasAsignadas.Add(new MissionExclusiveAssignment
                        { misionId = kvp.Key, characterId = kvp.Value });
            }

            // Globales activas
            foreach (var kvp in misionesGlobalesActivas)
            {
                data.globalesActivas.Add(new MissionActiveSaveData
                {
                    misionId = kvp.Key,
                    tiempoAceptada = kvp.Value.tiempoAceptada
                });
            }

            // Per-personaje
            foreach (var kvp in datosPersonajes)
            {
                var charData = new CharacterMissionSaveData { characterId = kvp.Key };
                charData.completadas = new List<string>(kvp.Value.misionesCompletadas);
                charData.fallidas = new List<string>(kvp.Value.misionesFallidas);

                foreach (var activa in kvp.Value.misionesActivas)
                {
                    charData.activas.Add(new MissionActiveSaveData
                    {
                        misionId = activa.Key,
                        tiempoAceptada = activa.Value.tiempoAceptada
                    });
                }

                data.datosPersonajes.Add(charData);
            }

            return data;
        }

        public void CargarDatosGuardado(MissionSaveData data, GlobalPlayerState global,
            Dictionary<string, EvolutionState> estados)
        {
            globalState = global ?? new GlobalPlayerState();
            estadosPersonajes.Clear();
            datosPersonajes.Clear();
            misionesGlobalesActivas.Clear();
            misionesGlobalesDisponibles.Clear();

            if (data == null) return;

            // Restaurar estados de personajes
            foreach (var kvp in estados)
                estadosPersonajes[kvp.Key] = kvp.Value;

            // Restaurar globales
            if (data.globalesCompletadas != null)
                foreach (var id in data.globalesCompletadas)
                    globalState.misionesGlobalesCompletadas.Add(id);

            if (data.globalesFallidas != null)
                foreach (var id in data.globalesFallidas)
                    globalState.misionesGlobalesFallidas.Add(id);

            if (data.exclusivasAsignadas != null)
                foreach (var asign in data.exclusivasAsignadas)
                    globalState.misionesExclusivasAsignadas[asign.misionId] = asign.characterId;

            // Restaurar globales activas
            foreach (var activa in data.globalesActivas)
            {
                var def = BuscarDefinicion(activa.misionId);
                if (def != null)
                {
                    var instancia = new MissionInstance(def) { tiempoAceptada = activa.tiempoAceptada };
                    misionesGlobalesActivas[activa.misionId] = instancia;
                }
            }

            // Restaurar per-personaje
            if (data.datosPersonajes != null)
            {
                foreach (var charData in data.datosPersonajes)
                {
                    var datos = new CharacterMissionData(charData.characterId);

                    if (charData.completadas != null)
                        foreach (var id in charData.completadas)
                            datos.misionesCompletadas.Add(id);

                    if (charData.fallidas != null)
                        foreach (var id in charData.fallidas)
                            datos.misionesFallidas.Add(id);

                    foreach (var activa in charData.activas)
                    {
                        var def = BuscarDefinicion(activa.misionId);
                        if (def != null)
                        {
                            var instancia = new MissionInstance(def) { tiempoAceptada = activa.tiempoAceptada };
                            datos.misionesActivas[activa.misionId] = instancia;
                        }
                    }

                    datosPersonajes[charData.characterId] = datos;
                }
            }

            RevaluarTodasLasMisiones();
        }

        #endregion

        #region Propagación de Progreso (interna)

        private void PropaglarKillAInstancia(MissionInstance instancia, Flags.TipoEntidades tipoEntidad)
        {
            for (int i = 0; i < instancia.objetivos.Count; i++)
            {
                if (instancia.objetivos[i] is KillObjectiveInstance killObj)
                {
                    float progAnterior = killObj.GetProgreso();
                    bool recienCompleto = killObj.RegistrarKill(tipoEntidad);
                    float progNuevo = killObj.GetProgreso();

                    if (progNuevo > progAnterior)
                        PublicarProgreso(instancia, i, progAnterior, progNuevo);

                    if (recienCompleto)
                        PublicarObjetivoCompletado(instancia, i);
                }
            }
        }

        private void PropaglarZonaAInstancia(MissionInstance instancia, string zonaId)
        {
            for (int i = 0; i < instancia.objetivos.Count; i++)
            {
                if (instancia.objetivos[i] is ReachZoneObjectiveInstance zoneObj)
                {
                    float progAnterior = zoneObj.GetProgreso();
                    bool recienCompleto = zoneObj.RegistrarZona(zonaId);
                    float progNuevo = zoneObj.GetProgreso();

                    if (progNuevo > progAnterior)
                        PublicarProgreso(instancia, i, progAnterior, progNuevo);

                    if (recienCompleto)
                        PublicarObjetivoCompletado(instancia, i);
                }
            }
        }

        private void PropaglarItemAInstancia(MissionInstance instancia, string itemId, int cantidad)
        {
            for (int i = 0; i < instancia.objetivos.Count; i++)
            {
                if (instancia.objetivos[i] is CollectItemObjectiveInstance collectObj)
                {
                    float progAnterior = collectObj.GetProgreso();
                    bool recienCompleto = collectObj.RegistrarItem(itemId, cantidad);
                    float progNuevo = collectObj.GetProgreso();

                    if (progNuevo > progAnterior)
                        PublicarProgreso(instancia, i, progAnterior, progNuevo);

                    if (recienCompleto)
                        PublicarObjetivoCompletado(instancia, i);
                }
            }
        }

        #endregion

        #region Participantes de Combate

        /// <summary>
        /// Obtiene los characterIds de los personajes que participan en el combate actual.
        /// Todos los del party activo reciben crédito.
        /// 
        /// NOTA: Si tu CombateManager tiene un tracking más específico de participantes,
        /// inyectá esa lógica aquí o reemplazá este método.
        /// </summary>
        private List<string> ObtenerParticipantesCombate()
        {
            var participantes = new List<string>();
            var partyManager = PlayerPartyManager.Instance;
            if (partyManager == null) return participantes;

            // El main siempre participa
            if (partyManager.MainCharacter != null)
                participantes.Add(partyManager.MainCharacter.CharacterId);

            // Todos los miembros del party activo participan
            foreach (var miembro in partyManager.ActiveParty)
            {
                if (miembro != null && !participantes.Contains(miembro.CharacterId))
                    participantes.Add(miembro.CharacterId);
            }

            return participantes;
        }

        #endregion

        #region Helpers de Eventos

        private void PublicarProgreso(MissionInstance instancia, int indice, float anterior, float nuevo)
        {
            EventBus.Publicar(new EventoMisionProgreso
            {
                Instancia = instancia,
                IndiceObjetivo = indice,
                ProgresoAnterior = anterior,
                ProgresoNuevo = nuevo
            });
        }

        private void PublicarObjetivoCompletado(MissionInstance instancia, int indice)
        {
            if (debugMode)
                Debug.Log($"[MissionManager] Objetivo {indice} completado en misión: " +
                          $"{instancia.definition.nombreMostrar ?? instancia.definition.misionId}");

            EventBus.Publicar(new EventoObjetivoCompletado
            {
                Instancia = instancia,
                IndiceObjetivo = indice
            });
        }

        #endregion
    }
}

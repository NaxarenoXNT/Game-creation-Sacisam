using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Habilidades;

namespace Managers
{
    public class CombateManager : MonoBehaviour
    {
        [Header("Modo de Operación")]
        [Tooltip("Si true, usa las referencias manuales. Si false, espera al EncounterManager.")]
        [SerializeField] private bool useLegacyMode = false;
        
        [Tooltip("Si true, espera input de UI para jugadores. Si false, usa IA para todos.")]
        [SerializeField] private bool usePlayerUIInput = true;
        
        [Header("Referencias Manuales (Legacy Mode)")]
        [Tooltip("EntityController del jugador que debe estar en la escena")]
        [SerializeField] private EntityController jugadorController;
        
        [Tooltip("Lista de EnemyControllers que deben estar en la escena")]
        [SerializeField] private List<EnemyController> enemigosControllers = new List<EnemyController>();
        
        [Header("Estado")]
        [SerializeField] private bool combateActivo = false;
        
        private TurnManager turnManager;
        private List<IEntidadCombate> todasLasEntidades = new List<IEntidadCombate>();
        
        // Referencias para el nuevo sistema
        private List<EntityController> partyControllers = new List<EntityController>();
        
        // Estado de espera de input del jugador
        private bool esperandoInputJugador = false;
        private EntityController entidadEsperandoInput;
        private List<IEntidadCombate> aliadosActuales;
        private List<IEntidadCombate> enemigosActuales;
        private int numeroTurno = 0;
        
        /// <summary>Si hay un combate en progreso.</summary>
        public bool CombateActivo => combateActivo;
        
        /// <summary>Si está esperando input del jugador.</summary>
        public bool EsperandoInputJugador => esperandoInputJugador;

        
        void Start()
        {
            // Suscribirse a eventos de UI
            EventBus.Suscribir<EventoObjetivoSeleccionado>(OnObjetivoSeleccionado);
            
            // Solo iniciar automáticamente en modo legacy
            if (useLegacyMode)
            {
                IniciarCombate();
            }
        }
        
        void OnDestroy()
        {
            EventBus.Desuscribir<EventoObjetivoSeleccionado>(OnObjetivoSeleccionado);
        }
        
        void IniciarCombate()
        {
            Debug.Log("╔════════════════════════════════════╗");
            Debug.Log("║     INICIANDO COMBATE (LEGACY)     ║");
            Debug.Log("╚════════════════════════════════════╝");
            
            // ========== VALIDAR JUGADOR ==========
            if (jugadorController == null)
            {
                // Intentar buscar en escena
                jugadorController = FindFirstObjectByType<EntityController>();
            }
            
            if (jugadorController == null || jugadorController.EntidadLogica == null)
            {
                Debug.LogError("❌ ERROR: No se encontró EntityController del jugador!");
                Debug.LogError("   Asigna el jugador en el Inspector o asegúrate de que exista en escena.");
                return;
            }
            
            // ========== AGREGAR JUGADOR ==========
            IEntidadCombate jugador = jugadorController.EntidadLogica;
            todasLasEntidades.Add(jugador);
            partyControllers.Add(jugadorController);
            
            Debug.Log($"\n⚔️  JUGADOR: {jugador.Nombre_Entidad} [Nv.{jugador.Nivel_Entidad}]");
            Debug.Log($"   HP: {jugador.VidaActual_Entidad}/{jugador.Vida_Entidad} | ATK: {jugador.PuntosDeAtaque_Entidad} | DEF: {jugador.PuntosDeDefensa_Entidad} | VEL: {jugador.Velocidad}");
            
            // Mostrar elementos activos
            if (jugadorController.EntityStats != null && jugadorController.EntityStats.activeStatuses.Count > 0)
            {
                Debug.Log($"   🔥 Elementos activos: {jugadorController.EntityStats.activeStatuses.Count}");
                foreach (var status in jugadorController.EntityStats.activeStatuses)
                {
                    Debug.Log($"      • {status.definition.elementName} [Nv.{status.level}]");
                }
            }
            else
            {
                Debug.Log($"   ⚪ Sin elementos activos");
            }
            
            // ========== VALIDAR ENEMIGOS ==========
            if (enemigosControllers == null || enemigosControllers.Count == 0)
            {
                // Intentar buscar en escena
                enemigosControllers = new List<EnemyController>(FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
            }
            
            if (enemigosControllers.Count == 0)
            {
                Debug.LogError("❌ ERROR: No se encontraron enemigos en la escena!");
                Debug.LogError("   Asegúrate de tener GameObjects con EnemyController configurados.");
                return;
            }
            
            // ========== AGREGAR ENEMIGOS ==========
            AgregarEnemigosInterno(enemigosControllers);
            
            // ========== VERIFICAR QUE HAYA ENTIDADES ==========
            if (todasLasEntidades.Count <= 1)
            {
                Debug.LogError("❌ ERROR: No hay suficientes entidades para el combate (legacy).");
                EventBus.Publicar(new EventoCombateFinalizado
                {
                    Victoria = false,
                    XPGanada = 0,
                    OroGanado = 0
                });
                return;
            }
            
            // ========== INICIALIZAR SISTEMA DE TURNOS ==========
            IniciarSistemaDeTurnos();
        }
        
        /// <summary>
        /// Inicia un combate con entidades proporcionadas por el EncounterManager.
        /// </summary>
        public void IniciarCombateConEntidades(List<EntityController> party, List<EnemyController> enemigos)
        {
            if (combateActivo)
            {
                Debug.LogWarning("[CombateManager] Ya hay un combate activo! Publicando fin para recuperar estado.");
                // Publicar fin para que el EncounterManager y GameFlow se recuperen
                EventBus.Publicar(new EventoCombateFinalizado
                {
                    Victoria = false,
                    XPGanada = 0,
                    OroGanado = 0
                });
                return;
            }
            
            Debug.Log("╔════════════════════════════════════╗");
            Debug.Log("║     INICIANDO COMBATE (ENCOUNTER)  ║");
            Debug.Log("╚════════════════════════════════════╝");
            
            // Limpiar estado anterior
            todasLasEntidades.Clear();
            partyControllers.Clear();
            enemigosControllers.Clear();
            
            // ========== AGREGAR PARTY ==========
            Debug.Log($"\n⚔️  PARTY ({party.Count} miembros):");
            
            foreach (var member in party)
            {
                if (member == null || member.EntidadLogica == null)
                {
                    Debug.LogWarning("⚠️ Miembro del party no inicializado, saltando...");
                    continue;
                }
                
                todasLasEntidades.Add(member.EntidadLogica);
                partyControllers.Add(member);
                
                Debug.Log($"   • {member.Nombre_Entidad} [Nv.{member.Nivel_Entidad}]");
                Debug.Log($"     HP: {member.VidaActual_Entidad}/{member.Vida_Entidad} | ATK: {member.PuntosDeAtaque_Entidad}");
            }
            
            // ========== AGREGAR ENEMIGOS ==========
            AgregarEnemigosInterno(enemigos);
            
            // ========== VERIFICAR QUE HAYA ENTIDADES ==========
            if (todasLasEntidades.Count <= 1)
            {
                Debug.LogError("❌ ERROR: No hay suficientes entidades para el combate. " +
                               $"(entidades={todasLasEntidades.Count}, party={party.Count}, enemigos={enemigos.Count})");

                // Publicar EventoCombateFinalizado para que GameFlowController
                // haga Pop() y regrese a ExplorationFlowState. Sin esto el juego
                // queda atrapado en CombatFlowState sin turnos ni forma de salir.
                EventBus.Publicar(new EventoCombateFinalizado
                {
                    Victoria = false,
                    XPGanada = 0,
                    OroGanado = 0
                });

                todasLasEntidades.Clear();
                partyControllers.Clear();
                enemigosControllers.Clear();
                return;
            }
            
            // ========== INICIALIZAR SISTEMA DE TURNOS ==========
            IniciarSistemaDeTurnos();
        }
        
        /// <summary>
        /// Agrega enemigos al combate actual (para refuerzos).
        /// </summary>
        public void AgregarEnemigosAlCombate(List<EnemyController> nuevosEnemigos)
        {
            if (!combateActivo)
            {
                Debug.LogWarning("[CombateManager] No hay combate activo para agregar enemigos.");
                return;
            }
            
            Debug.Log($"\n👹 +{nuevosEnemigos.Count} REFUERZOS ENEMIGOS:");
            
            foreach (var enemyController in nuevosEnemigos)
            {
                if (enemyController == null || enemyController.EnemigoLogica == null)
                {
                    Debug.LogWarning($"⚠️ EnemyController no está inicializado, saltando...");
                    continue;
                }
                
                // Verificar que no esté ya en combate
                if (enemigosControllers.Contains(enemyController))
                {
                    Debug.LogWarning($"⚠️ {enemyController.Nombre_Entidad} ya está en combate.");
                    continue;
                }
                
                IEntidadCombate enemigo = enemyController.EnemigoLogica;
                todasLasEntidades.Add(enemigo);
                enemigosControllers.Add(enemyController);
                
                // Agregar al sistema de turnos
                turnManager?.AgregarEntidad(enemigo);
                
                Debug.Log($"   + {enemigo.Nombre_Entidad} [Nv.{enemigo.Nivel_Entidad}]");
                Debug.Log($"     HP: {enemigo.VidaActual_Entidad}/{enemigo.Vida_Entidad} | ATK: {enemigo.PuntosDeAtaque_Entidad}");
            }
        }
        
        /// <summary>
        /// Agrega un aliado (refuerzo) al combate actual.
        /// </summary>
        public void AgregarAliadoAlCombate(EntityController aliado)
        {
            if (!combateActivo)
            {
                Debug.LogWarning("[CombateManager] No hay combate activo para agregar aliados.");
                return;
            }
            
            if (aliado == null || aliado.EntidadLogica == null)
            {
                Debug.LogWarning("[CombateManager] Aliado no válido.");
                return;
            }
            
            // Verificar que no esté ya en combate
            if (partyControllers.Contains(aliado))
            {
                Debug.LogWarning($"[CombateManager] {aliado.Nombre_Entidad} ya está en combate.");
                return;
            }
            
            Debug.Log($"\n🛡️ +REFUERZO ALIADO:");
            
            IEntidadCombate jugador = aliado.EntidadLogica;
            todasLasEntidades.Add(jugador);
            partyControllers.Add(aliado);
            
            // Agregar al sistema de turnos
            turnManager?.AgregarEntidad(jugador);
            
            Debug.Log($"   + {jugador.Nombre_Entidad} [Nv.{jugador.Nivel_Entidad}]");
            Debug.Log($"     HP: {jugador.VidaActual_Entidad}/{jugador.Vida_Entidad} | ATK: {jugador.PuntosDeAtaque_Entidad}");
            
            // Publicar evento de refuerzo llegado
            EventBus.Publicar(new EventoRefuerzoLlegado { Refuerzo = aliado });
        }
        
        /// <summary>
        /// Agrega enemigos a las listas internas.
        /// </summary>
        private void AgregarEnemigosInterno(List<EnemyController> enemigos)
        {
            Debug.Log($"\n👹 ENEMIGOS ({enemigos.Count}):");
            
            int enemigoIndex = 1;
            foreach (var enemyController in enemigos)
            {
                if (enemyController == null || enemyController.EnemigoLogica == null)
                {
                    Debug.LogWarning($"⚠️ EnemyController {enemigoIndex} no está inicializado, saltando...");
                    enemigoIndex++;
                    continue;
                }
                
                IEntidadCombate enemigo = enemyController.EnemigoLogica;
                todasLasEntidades.Add(enemigo);
                enemigosControllers.Add(enemyController);
                
                Debug.Log($"   {enemigoIndex}. {enemigo.Nombre_Entidad} [Nv.{enemigo.Nivel_Entidad}]");
                Debug.Log($"      HP: {enemigo.VidaActual_Entidad}/{enemigo.Vida_Entidad} | ATK: {enemigo.PuntosDeAtaque_Entidad} | DEF: {enemigo.PuntosDeDefensa_Entidad} | VEL: {enemigo.Velocidad}");
                
                // Mostrar elementos si tiene
                if (enemyController.EntityStats != null && enemyController.EntityStats.activeStatuses.Count > 0)
                {
                    Debug.Log($"      🔥 Elementos: {string.Join(", ", enemyController.EntityStats.activeStatuses.Select(s => s.definition.elementName))}");
                }
                
                enemigoIndex++;
            }
        }
        
        /// <summary>
        /// Inicializa el sistema de turnos y comienza el combate.
        /// </summary>
        private void IniciarSistemaDeTurnos()
        {
            combateActivo = true;
            
            turnManager = new TurnManager();
            turnManager.InicializarTurnos(todasLasEntidades);
            
            MostrarOrdenTurnos();
            
            // Publicar evento de combate iniciado
            EventBus.Publicar(new EventoCombateIniciado
            {
                Jugadores = todasLasEntidades.Where(e => e is IJugadorProgresion).ToList(),
                Enemigos = todasLasEntidades.Where(e => !(e is IJugadorProgresion)).ToList()
            });
            
            ProcesarTurno();
        }
        
        void MostrarOrdenTurnos()
        {
            Debug.Log("\n╔════════════════════════════════════╗");
            Debug.Log("║       ORDEN DE TURNOS              ║");
            Debug.Log("╚════════════════════════════════════╝");
            var orden = turnManager.ObtenerOrdenActual();
            for (int i = 0; i < orden.Count; i++)
            {
                string icono = orden[i] is IJugadorProgresion ? "⚔️" : "👹";
                Debug.Log($"{i + 1}. {icono} {orden[i].Nombre_Entidad} (VEL: {orden[i].Velocidad})");
            }
        }
        
        void ProcesarTurno()
        {
            if (!VerificarCombateActivo()) return;
            
            numeroTurno++;
            IEntidadCombate entidadRaw = turnManager.EntidadActual;
            bool esJugador = entidadRaw is IJugadorProgresion;
            
            Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"🎯 TURNO {numeroTurno}: {entidadRaw.Nombre_Entidad}");
            
            // Publicar evento de turno iniciado
            EventBus.Publicar(new EventoTurnoIniciado
            {
                Entidad = entidadRaw,
                NumeroTurno = numeroTurno,
                EsJugador = esJugador
            });
            
            // 0. Procesar estados al inicio del turno (veneno, quemado, etc.)
            if (entidadRaw is Padres.Entidad entidad)
            {
                bool puedeActuar = entidad.ProcesarEstadosInicioTurno();
                
                // Verificar si murió por daño de estados
                if (!entidadRaw.EstaVivo())
                {
                    ManejarMuerte(entidadRaw);
                    FinalizarTurno();
                    return;
                }
                
                // Si está incapacitado (aturdido, congelado), saltar turno
                if (!puedeActuar)
                {
                    Debug.Log($"⏭️ {entidadRaw.Nombre_Entidad} pierde su turno por estado.");
                    FinalizarTurno();
                    return;
                }
            }
            
            // 1. Obtener las listas de aliados y enemigos para el targeting
            aliadosActuales = todasLasEntidades.Where(e => e.EsTipoEntidad(entidadRaw.TipoEntidad)).ToList();
            enemigosActuales = todasLasEntidades.Where(e => !e.EsTipoEntidad(entidadRaw.TipoEntidad)).ToList(); 

            // 2. ¿Es un jugador y queremos usar UI?
            if (esJugador && usePlayerUIInput)
            {
                // Buscar el EntityController correspondiente
                EntityController controller = ObtenerControllerDeEntidad(entidadRaw);
                
                if (controller != null)
                {
                    // Esperar input de la UI
                    esperandoInputJugador = true;
                    entidadEsperandoInput = controller;
                    
                    EventBus.Publicar(new EventoEsperandoAccionJugador
                    {
                        Entidad = controller,
                        Aliados = aliadosActuales,
                        Enemigos = enemigosActuales
                    });
                    
                    Debug.Log($"⏳ Esperando input del jugador para {entidadRaw.Nombre_Entidad}...");
                    return; // No continuar, esperar callback
                }
            }
            
            // 3. Si es enemigo o no hay UI, usar IA
            if (entidadRaw is Interfaces.IEntidadActuable actuable) 
            {
                (IHabilidadesCommand comando, IEntidadCombate objetivo) = actuable.ObtenerAccionElegida(aliadosActuales, enemigosActuales);

                if (comando != null && objetivo != null)
                {
                    EjecutarHabilidad(comando, entidadRaw, objetivo, aliadosActuales, enemigosActuales); 
                }
                else
                {
                    Debug.Log($"⏭️ {entidadRaw.Nombre_Entidad} no realizó ninguna acción.");
                }
            }
            else
            {
                Debug.LogError($"❌ ERROR: {entidadRaw.Nombre_Entidad} no implementa la capacidad de actuar (IEntidadActuable).");
            }
            
            FinalizarTurno();
        }
        
        /// <summary>
        /// Callback cuando la UI selecciona un objetivo.
        /// </summary>
        private void OnObjetivoSeleccionado(EventoObjetivoSeleccionado evento)
        {
            if (!esperandoInputJugador) return;
            if (evento.Atacante != entidadEsperandoInput) return;
            
            esperandoInputJugador = false;
            
            IEntidadCombate invocador = evento.Atacante.EntidadLogica;
            
            // Caso: Ceder turno (objetivo null)
            if (evento.Objetivo == null && evento.Habilidad == null)
            {
                Debug.Log($"⏭️ {invocador.Nombre_Entidad} cede su turno.");
                FinalizarTurno();
                return;
            }
            
            // Caso: Defender (objetivo es self, habilidad null)
            if (evento.Objetivo == invocador && evento.Habilidad == null)
            {
                Debug.Log($"🛡️ {invocador.Nombre_Entidad} se defiende.");
                // TODO: Aplicar buff de defensa temporal
                FinalizarTurno();
                return;
            }
            
            // Caso: Usar habilidad
            if (evento.Habilidad != null && evento.Objetivo != null)
            {
                // Iniciar cooldown
                if (evento.Atacante.Cooldowns != null)
                {
                    evento.Atacante.Cooldowns.IniciarCooldown(evento.Habilidad);
                }
                
                // Ejecutar habilidad
                EjecutarHabilidad(evento.Habilidad, invocador, evento.Objetivo, aliadosActuales, enemigosActuales);
                
                // Publicar evento de habilidad usada
                EventBus.Publicar(new EventoHabilidadUsada
                {
                    Invocador = invocador,
                    Objetivo = evento.Objetivo,
                    Habilidad = evento.Habilidad
                });
            }
            
            FinalizarTurno();
        }
        
        /// <summary>
        /// Obtiene el EntityController de una entidad de combate.
        /// </summary>
        private EntityController ObtenerControllerDeEntidad(IEntidadCombate entidad)
        {
            foreach (var controller in partyControllers)
            {
                if (controller.EntidadLogica == entidad)
                {
                    return controller;
                }
            }
            return null;
        }

        // Nuevo método para ejecutar el comando y manejar el post-efecto
        void EjecutarHabilidad(
            IHabilidadesCommand habilidadComando, 
            IEntidadCombate invocador, 
            IEntidadCombate objetivo,
            List<IEntidadCombate> aliados, 
            List<IEntidadCombate> enemigos
        )
        {
            // El HabilidadData contiene la lógica Ejecutar() que llama a RecibirDaño en el objetivo.
            habilidadComando.Ejecutar(invocador, objetivo, aliados, enemigos); 
            
            // Lógica de Orquestación: Si el objetivo murió, el Manager lo maneja.
            if (objetivo != null && !objetivo.EstaVivo())
            {
                ManejarMuerte(objetivo);
            }
            
            // Aquí iría el manejo de costes de Maná/Stamina, etc.
        }
        
       
        
                
        void ManejarMuerte(IEntidadCombate entidad)
        {
            Debug.Log($"\n☠️  {entidad.Nombre_Entidad} [Nv.{entidad.Nivel_Entidad}] ha sido derrotado!");
            
            // Si era enemigo, dar recompensas a jugadores vivos
            if (!(entidad is IJugadorProgresion) && entidad is Padres.Enemigos enemigo)
            {
                Debug.Log($"💰 XP otorgada: {enemigo.XPOtorgada} (80% jugador, 20% elementos)");
                
                var jugadoresVivos = todasLasEntidades
                    .OfType<IJugadorProgresion>()
                    .Where(j => (j as IEntidadCombate).EstaVivo());
                
                foreach (var jugador in jugadoresVivos)
                {
                    var jugadorEntidad = jugador as IEntidadCombate;
                    float xpTotal = enemigo.XPOtorgada;
                    float xpJugador = xpTotal * 0.8f;
                    float xpElementos = xpTotal * 0.2f;
                    
                    Debug.Log($"\n📊 {jugadorEntidad.Nombre_Entidad} recibe experiencia:");
                    Debug.Log($"   XP Jugador: +{xpJugador:F1} XP");
                    
                    // Contar elementos activos (si tiene EntityStats vinculado)
                    var jugadorPadre = jugador as Padres.Jugador;
                    if (jugadorPadre?.entityStats != null && jugadorPadre.entityStats.activeStatuses.Count > 0)
                    {
                        int cantidadElementos = jugadorPadre.entityStats.activeStatuses.Count;
                        float xpPorElemento = xpElementos / cantidadElementos;
                        Debug.Log($"   XP Elementos: +{xpElementos:F1} XP (÷{cantidadElementos} elementos = {xpPorElemento:F1} XP c/u)");
                    }
                    else
                    {
                        Debug.Log($"   XP Elementos: +{xpElementos:F1} XP (sin elementos activos)");
                    }
                    
                    // ANTES de dar XP, mostrar estado actual
                    Debug.Log($"   Estado antes: Nv.{jugadorEntidad.Nivel_Entidad} | {jugador.Experiencia_Actual:F1}/{jugador.Experiencia_Progreso:F1} XP");
                    
                    jugador.RecibirXP(xpTotal);
                    
                    // DESPUÉS de dar XP, mostrar nuevo estado
                    Debug.Log($"   Estado después: Nv.{jugadorEntidad.Nivel_Entidad} | {jugador.Experiencia_Actual:F1}/{jugador.Experiencia_Progreso:F1} XP");
                }
            }
            
            turnManager.EliminarEntidad(entidad);
        }
        
        void FinalizarTurno()
        {
            if (!VerificarCombateActivo()) return;
            
            // Publicar evento de turno finalizado
            if (turnManager?.EntidadActual != null)
            {
                EventBus.Publicar(new EventoTurnoFinalizado
                {
                    Entidad = turnManager.EntidadActual
                });
            }
            
            // Reducir cooldowns del jugador actual
            if (entidadEsperandoInput != null)
            {
                entidadEsperandoInput.Cooldowns?.ProcesarInicioTurno();
            }
            
            // Limpiar estado
            esperandoInputJugador = false;
            entidadEsperandoInput = null;
            
            Debug.Log("⏭️  Finalizando turno...\n");
            Invoke(nameof(SiguienteTurno), 1f);
        }
        
        void SiguienteTurno()
        {
            turnManager.SiguienteTurno();
            ProcesarTurno();
        }
        
        bool VerificarCombateActivo()
        {
            var jugadoresVivos = todasLasEntidades
                .Where(e => e.EstaVivo() && e is IJugadorProgresion)
                .ToList();
            
            var enemigosVivos = todasLasEntidades
                .Where(e => e.EstaVivo() && !(e is IJugadorProgresion))
                .ToList();
            
            if (jugadoresVivos.Count == 0)
            {
                Debug.Log("\n╔════════════════════════════════════╗");
                Debug.Log("║          💀 DERROTA                ║");
                Debug.Log("║   Todos los jugadores han caído    ║");
                Debug.Log("╚════════════════════════════════════╝");
                
                FinalizarCombate(false);
                return false;
            }
            
            if (enemigosVivos.Count == 0)
            {
                Debug.Log("\n╔════════════════════════════════════╗");
                Debug.Log("║          🏆 VICTORIA               ║");
                Debug.Log("║  Todos los enemigos derrotados     ║");
                Debug.Log("╚════════════════════════════════════╝");
                
                FinalizarCombate(true);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Finaliza el combate y publica el evento correspondiente.
        /// </summary>
        private void FinalizarCombate(bool victoria)
        {
            combateActivo = false;
            
            // Calcular XP total ganada (aproximado)
            int xpTotal = 0;
            // TODO: Calcular XP total de enemigos derrotados
            
            // Publicar evento de fin de combate
            EventBus.Publicar(new EventoCombateFinalizado
            {
                Victoria = victoria,
                XPGanada = xpTotal,
                OroGanado = 0 // TODO: Implementar sistema de oro
            });
            
            // Limpiar listas
            todasLasEntidades.Clear();
            partyControllers.Clear();
            enemigosControllers.Clear();
        }
        
        // ========== MÉTODOS DE DEBUG ==========
        
        [ContextMenu("Debug: Buscar Entidades en Escena")]
        private void DebugBuscarEntidades()
        {
            jugadorController = FindFirstObjectByType<EntityController>();
            enemigosControllers = new List<EnemyController>(FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
            
            Debug.Log($"Encontrados: {(jugadorController != null ? "1" : "0")} jugador, {enemigosControllers.Count} enemigos");
        }
        
        [ContextMenu("Debug: Mostrar Estado Combate")]
        private void DebugMostrarEstado()
        {
            Debug.Log("=== ESTADO DEL COMBATE ===");
            Debug.Log($"Entidades totales: {todasLasEntidades.Count}");
            
            foreach (var entidad in todasLasEntidades)
            {
                string tipo = entidad is IJugadorProgresion ? "JUGADOR" : "ENEMIGO";
                string estado = entidad.EstaVivo() ? "VIVO" : "MUERTO";
                Debug.Log($"[{tipo}] {entidad.Nombre_Entidad}: {estado} - HP: {entidad.VidaActual_Entidad}/{entidad.Vida_Entidad}");
            }
        }
    }
}
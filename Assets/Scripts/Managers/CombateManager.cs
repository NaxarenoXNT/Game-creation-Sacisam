using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Habilidades;

namespace Managers
{
    public class CombateManager : MonoBehaviour
    {
        [Header("Referencias en Escena")]
        [Tooltip("EntityController del jugador que debe estar en la escena")]
        [SerializeField] private EntityController jugadorController;
        
        [Tooltip("Lista de EnemyControllers que deben estar en la escena")]
        [SerializeField] private List<EnemyController> enemigosControllers;
        
        private TurnManager turnManager;
        private List<IEntidadCombate> todasLasEntidades = new List<IEntidadCombate>();

        
        void Start()
        {
            IniciarCombate();
        }
        
        void IniciarCombate()
        {
            Debug.Log("╔════════════════════════════════════╗");
            Debug.Log("║     INICIANDO COMBATE              ║");
            Debug.Log("╚════════════════════════════════════╝");
            
            // ========== VALIDAR JUGADOR ==========
            if (jugadorController == null)
            {
                // Intentar buscar en escena
                jugadorController = FindObjectOfType<EntityController>();
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
                enemigosControllers = new List<EnemyController>(FindObjectsOfType<EnemyController>());
            }
            
            if (enemigosControllers.Count == 0)
            {
                Debug.LogError("❌ ERROR: No se encontraron enemigos en la escena!");
                Debug.LogError("   Asegúrate de tener GameObjects con EnemyController configurados.");
                return;
            }
            
            // ========== AGREGAR ENEMIGOS ==========
            Debug.Log($"\n👹 ENEMIGOS ({enemigosControllers.Count}):");
            
            int enemigoIndex = 1;
            foreach (var enemyController in enemigosControllers)
            {
                if (enemyController == null || enemyController.EnemigoLogica == null)
                {
                    Debug.LogWarning($"⚠️ EnemyController {enemigoIndex} no está inicializado, saltando...");
                    enemigoIndex++;
                    continue;
                }
                
                IEntidadCombate enemigo = enemyController.EnemigoLogica;
                todasLasEntidades.Add(enemigo);
                
                Debug.Log($"   {enemigoIndex}. {enemigo.Nombre_Entidad} [Nv.{enemigo.Nivel_Entidad}]");
                Debug.Log($"      HP: {enemigo.VidaActual_Entidad}/{enemigo.Vida_Entidad} | ATK: {enemigo.PuntosDeAtaque_Entidad} | DEF: {enemigo.PuntosDeDefensa_Entidad} | VEL: {enemigo.Velocidad}");
                
                // Mostrar elementos si tiene
                if (enemyController.EntityStats != null && enemyController.EntityStats.activeStatuses.Count > 0)
                {
                    Debug.Log($"      🔥 Elementos: {string.Join(", ", enemyController.EntityStats.activeStatuses.Select(s => s.definition.elementName))}");
                }
                
                enemigoIndex++;
            }
            
            // ========== VERIFICAR QUE HAYA ENTIDADES ==========
            if (todasLasEntidades.Count <= 1)
            {
                Debug.LogError("❌ ERROR: No hay suficientes entidades para el combate.");
                return;
            }
            
            // ========== INICIALIZAR SISTEMA DE TURNOS ==========
            turnManager = new TurnManager();
            turnManager.InicializarTurnos(todasLasEntidades);
            
            MostrarOrdenTurnos();
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
            
            IEntidadCombate entidadRaw = turnManager.EntidadActual;
            
            // 1. Obtener las listas de aliados y enemigos para el targeting
            List<IEntidadCombate> aliados = todasLasEntidades.Where(e => e.EsTipoEntidad(entidadRaw.TipoEntidad)).ToList();
            // ENEMIGOS son los que NO son del mismo tipo de entidad
            List<IEntidadCombate> enemigos = todasLasEntidades.Where(e => !e.EsTipoEntidad(entidadRaw.TipoEntidad)).ToList(); 
            
            Debug.Log($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"🎯 TURNO: {entidadRaw.Nombre_Entidad}");

            // 2. Pedir la Acción (El paso crucial que integra IEntidadActuable)
            // Intentamos castear la entidad actual a IEntidadActuable
            if (entidadRaw is Interfaces.IEntidadActuable actuable) 
            {
                // El controlador decide el comando y el objetivo
                (IHabilidadesCommad comando, IEntidadCombate objetivo) = actuable.ObtenerAccionElegida(aliados, enemigos);

                if (comando != null && objetivo != null)
                {
                    // 3. Ejecutar el Comando (El Manager ejecuta lo que le dieron)
                    EjecutarHabilidad(comando, entidadRaw, objetivo, aliados, enemigos); 
                }
                else
                {
                    Debug.Log($"⏭️ {entidadRaw.Nombre_Entidad} no realizó ninguna acción (no viable o esperando input).");
                }
            }
            else
            {
                Debug.LogError($"❌ ERROR: {entidadRaw.Nombre_Entidad} no implementa la capacidad de actuar (IEntidadActuable).");
            }
            
            FinalizarTurno();
        }

        // Nuevo método para ejecutar el comando y manejar el post-efecto
        void EjecutarHabilidad(
            IHabilidadesCommad habilidadComando, 
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
                return false;
            }
            
            if (enemigosVivos.Count == 0)
            {
                Debug.Log("\n╔════════════════════════════════════╗");
                Debug.Log("║          🏆 VICTORIA               ║");
                Debug.Log("║  Todos los enemigos derrotados     ║");
                Debug.Log("╚════════════════════════════════════╝");
                return false;
            }
            
            return true;
        }
        
        // ========== MÉTODOS DE DEBUG ==========
        
        [ContextMenu("Debug: Buscar Entidades en Escena")]
        private void DebugBuscarEntidades()
        {
            jugadorController = FindObjectOfType<EntityController>();
            enemigosControllers = new List<EnemyController>(FindObjectsOfType<EnemyController>());
            
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
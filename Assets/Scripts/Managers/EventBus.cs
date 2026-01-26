using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Sistema de eventos global para comunicacion desacoplada entre sistemas.
    /// Permite publicar y suscribirse a eventos sin referencias directas.
    /// </summary>
    public static class EventBus
    {
        // Diccionario de suscriptores por tipo de evento
        private static Dictionary<Type, List<Delegate>> suscriptores = new Dictionary<Type, List<Delegate>>();
        
        // Cola de eventos para procesamiento diferido
        private static Queue<Action> colaEventos = new Queue<Action>();
        private static bool procesandoCola = false;
        
        /// <summary>
        /// Suscribe un metodo a un tipo de evento.
        /// </summary>
        public static void Suscribir<T>(Action<T> callback) where T : IEvento
        {
            Type tipo = typeof(T);
            
            if (!suscriptores.ContainsKey(tipo))
            {
                suscriptores[tipo] = new List<Delegate>();
            }
            
            if (!suscriptores[tipo].Contains(callback))
            {
                suscriptores[tipo].Add(callback);
            }
        }
        
        /// <summary>
        /// Desuscribe un metodo de un tipo de evento.
        /// </summary>
        public static void Desuscribir<T>(Action<T> callback) where T : IEvento
        {
            Type tipo = typeof(T);
            
            if (suscriptores.ContainsKey(tipo))
            {
                suscriptores[tipo].Remove(callback);
            }
        }
        
        /// <summary>
        /// Publica un evento inmediatamente a todos los suscriptores.
        /// </summary>
        public static void Publicar<T>(T evento) where T : IEvento
        {
            Type tipo = typeof(T);
            
            if (!suscriptores.ContainsKey(tipo)) return;
            
            // Crear copia para evitar modificaciones durante iteracion
            var lista = new List<Delegate>(suscriptores[tipo]);
            
            foreach (var suscriptor in lista)
            {
                try
                {
                    ((Action<T>)suscriptor)?.Invoke(evento);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error en EventBus al procesar " + tipo.Name + ": " + ex.Message);
                }
            }
        }
        
        /// <summary>
        /// Encola un evento para procesamiento diferido.
        /// Util para evitar problemas de concurrencia.
        /// </summary>
        public static void PublicarDiferido<T>(T evento) where T : IEvento
        {
            colaEventos.Enqueue(() => Publicar(evento));
        }
        
        /// <summary>
        /// Procesa todos los eventos en cola.
        /// Llamar desde un MonoBehaviour en Update o LateUpdate.
        /// </summary>
        public static void ProcesarCola()
        {
            if (procesandoCola) return;
            
            procesandoCola = true;
            
            while (colaEventos.Count > 0)
            {
                var accion = colaEventos.Dequeue();
                try
                {
                    accion?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error procesando cola de eventos: " + ex.Message);
                }
            }
            
            procesandoCola = false;
        }
        
        /// <summary>
        /// Limpia todos los suscriptores. Usar con cuidado.
        /// </summary>
        public static void LimpiarTodo()
        {
            suscriptores.Clear();
            colaEventos.Clear();
        }
        
        /// <summary>
        /// Limpia suscriptores de un tipo especifico.
        /// </summary>
        public static void Limpiar<T>() where T : IEvento
        {
            Type tipo = typeof(T);
            if (suscriptores.ContainsKey(tipo))
            {
                suscriptores[tipo].Clear();
            }
        }
        
        /// <summary>
        /// Obtiene la cantidad de suscriptores para un tipo de evento.
        /// </summary>
        public static int ObtenerCantidadSuscriptores<T>() where T : IEvento
        {
            Type tipo = typeof(T);
            if (suscriptores.ContainsKey(tipo))
            {
                return suscriptores[tipo].Count;
            }
            return 0;
        }
    }
    
    /// <summary>
    /// Interfaz base para todos los eventos.
    /// </summary>
    public interface IEvento { }
    
    // =================================================================
    // =================== EVENTOS DE COMBATE ==========================
    // =================================================================
    
    public struct EventoCombateIniciado : IEvento
    {
        public List<Interfaces.IEntidadCombate> Jugadores;
        public List<Interfaces.IEntidadCombate> Enemigos;
    }
    
    public struct EventoCombateFinalizado : IEvento
    {
        public bool Victoria;
        public int XPGanada;
        public int OroGanado;
    }
    
    public struct EventoTurnoIniciado : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public int NumeroTurno;
    }
    
    public struct EventoTurnoFinalizado : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
    }
    
    public struct EventoHabilidadUsada : IEvento
    {
        public Interfaces.IEntidadCombate Invocador;
        public Interfaces.IEntidadCombate Objetivo;
        public HabilidadData Habilidad;
    }
    
    // =================================================================
    // =================== EVENTOS DE ENTIDAD ==========================
    // =================================================================
    
    public struct EventoDanoRecibido : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public int Cantidad;
        public Flags.ElementAttribute TipoDano;
        public Interfaces.IEntidadCombate Atacante;
    }
    
    public struct EventoCuracion : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public int Cantidad;
    }
    
    public struct EventoMuerte : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public Interfaces.IEntidadCombate Asesino;
    }
    
    public struct EventoEstadoAplicado : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public Flags.StatusFlag Estado;
        public int Duracion;
    }
    
    public struct EventoEstadoRemovido : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public Flags.StatusFlag Estado;
    }
    
    // =================================================================
    // =================== EVENTOS DE PROGRESION =======================
    // =================================================================
    
    public struct EventoNivelSubido : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public int NuevoNivel;
    }
    
    public struct EventoXPGanada : IEvento
    {
        public Interfaces.IEntidadCombate Entidad;
        public float Cantidad;
        public float Total;
        public float Necesaria;
    }
    
    // =================================================================
    // =================== EVENTOS DE UI ===============================
    // =================================================================
    
    public struct EventoMostrarMensaje : IEvento
    {
        public string Mensaje;
        public float Duracion;
        public Color? ColorTexto;
    }
    
    public struct EventoActualizarUI : IEvento
    {
        public string PanelId;
    }
}

using System.Collections.Generic;
using UnityEngine;
using Interfaces;

namespace IA
{
    /// <summary>
    /// Selecciona al jugador con menos vida.
    /// </summary>
    public class AccionAtacarDebil : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            IEntidadCombate objetivo = null;
            int menorVida = int.MaxValue;
            
            foreach (var jugador in jugadores)
            {
                if (!jugador.EstaVivo()) continue;
                
                if (jugador.VidaActual_Entidad < menorVida)
                {
                    menorVida = jugador.VidaActual_Entidad;
                    objetivo = jugador;
                }
            }
            
            if (objetivo == null) return EstadoNodo.Fallo;
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = objetivo,
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Selecciona al jugador con mas vida (tank).
    /// </summary>
    public class AccionAtacarTank : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            IEntidadCombate objetivo = null;
            int mayorVida = 0;
            
            foreach (var jugador in jugadores)
            {
                if (!jugador.EstaVivo()) continue;
                
                if (jugador.VidaActual_Entidad > mayorVida)
                {
                    mayorVida = jugador.VidaActual_Entidad;
                    objetivo = jugador;
                }
            }
            
            if (objetivo == null) return EstadoNodo.Fallo;
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = objetivo,
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Selecciona un jugador aleatorio.
    /// </summary>
    public class AccionAtacarAleatorio : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            var vivos = new List<IEntidadCombate>();
            foreach (var j in jugadores)
            {
                if (j.EstaVivo()) vivos.Add(j);
            }
            
            if (vivos.Count == 0) return EstadoNodo.Fallo;
            
            int indice = Random.Range(0, vivos.Count);
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = vivos[indice],
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Intenta curarse si tiene vida baja.
    /// </summary>
    public class AccionCurarse : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo = null, // Se curara a si mismo
                TipoAccion = ResultadoIA.TipoAccionIA.Curar
            };
            
            return EstadoNodo.Exito;
        }
    }
}

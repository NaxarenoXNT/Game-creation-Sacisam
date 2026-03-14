using System.Collections.Generic;
using UnityEngine;
using Interfaces;
using Flags;

namespace IA
{
    // ================================================================
    //  NODOS DE ATAQUE — seleccionan objetivo + habilidad ofensiva
    // ================================================================

    /// <summary>
    /// Ataca al jugador con menos vida (% HP más bajo).
    /// Prioriza habilidades de Ataque, luego Debuff, luego default.
    /// </summary>
    public class AccionAtacarDebil : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            IEntidadCombate objetivo = null;
            float menorPorcentaje = float.MaxValue;
            
            foreach (var jugador in jugadores)
            {
                if (!jugador.EstaVivo()) continue;
                float pct = (float)jugador.VidaActual_Entidad / jugador.Vida_Entidad;
                if (pct < menorPorcentaje) { menorPorcentaje = pct; objetivo = jugador; }
            }
            
            if (objetivo == null) return EstadoNodo.Fallo;

            var habilidad = SeleccionarHabilidadOfensiva(objetivo,
                CategoriaHabilidad.Ataque,
                CategoriaHabilidad.Debuff,
                CategoriaHabilidad.Control);

            if (habilidad == null) return EstadoNodo.Fallo;
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo   = objetivo,
                Habilidad  = habilidad,
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Ataca al jugador con más vida (el tanque).
    /// Prioriza habilidades de Ataque.
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

            var habilidad = SeleccionarHabilidadOfensiva(objetivo,
                CategoriaHabilidad.Ataque,
                CategoriaHabilidad.Debuff);

            if (habilidad == null) return EstadoNodo.Fallo;
            
            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo   = objetivo,
                Habilidad  = habilidad,
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            return EstadoNodo.Exito;
        }
    }
    
    /// <summary>
    /// Ataca a un jugador aleatorio vivo.
    /// Prioriza habilidades de Ataque, usa default como fallback.
    /// </summary>
    public class AccionAtacarAleatorio : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            var vivos = new List<IEntidadCombate>();
            foreach (var j in jugadores) { if (j.EstaVivo()) vivos.Add(j); }
            if (vivos.Count == 0) return EstadoNodo.Fallo;

            var objetivo = vivos[Random.Range(0, vivos.Count)];
            var habilidad = SeleccionarHabilidadOfensiva(objetivo,
                CategoriaHabilidad.Ataque,
                CategoriaHabilidad.Debuff,
                CategoriaHabilidad.Control);

            if (habilidad == null) return EstadoNodo.Fallo;

            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo   = objetivo,
                Habilidad  = habilidad,
                TipoAccion = ResultadoIA.TipoAccionIA.Atacar
            };
            return EstadoNodo.Exito;
        }
    }

    /// <summary>
    /// Prioriza habilidades de Control (stun, root…) sobre el jugador más débil.
    /// </summary>
    public class AccionControlarObjetivo : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            IEntidadCombate objetivo = null;
            float menorPorcentaje = float.MaxValue;
            foreach (var j in jugadores)
            {
                if (!j.EstaVivo()) continue;
                float pct = (float)j.VidaActual_Entidad / j.Vida_Entidad;
                if (pct < menorPorcentaje) { menorPorcentaje = pct; objetivo = j; }
            }
            if (objetivo == null) return EstadoNodo.Fallo;

            var habilidad = SeleccionarHabilidadOfensiva(objetivo,
                CategoriaHabilidad.Control,
                CategoriaHabilidad.Debuff,
                CategoriaHabilidad.Ataque);

            if (habilidad == null) return EstadoNodo.Fallo;

            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo   = objetivo,
                Habilidad  = habilidad,
                TipoAccion = ResultadoIA.TipoAccionIA.Control
            };
            return EstadoNodo.Exito;
        }
    }

    // ================================================================
    //  NODOS DE SOPORTE — seleccionan objetivo aliado + habilidad soporte
    // ================================================================

    /// <summary>
    /// Cura al aliado (o a sí mismo) con menor % de vida.
    /// Solo evalúa bien si hay una habilidad de Curación disponible.
    /// </summary>
    public class AccionCurarAliado : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            var objetivo = AliadoMasHerido();
            if (objetivo == null) return EstadoNodo.Fallo;

            var habilidad = SeleccionarHabilidadSoporte(objetivo,
                CategoriaHabilidad.Curacion);

            if (habilidad == null) return EstadoNodo.Fallo;

            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo   = objetivo,
                Habilidad  = habilidad,
                TipoAccion = ResultadoIA.TipoAccionIA.Curar
            };
            return EstadoNodo.Exito;
        }
    }

    /// <summary>
    /// Aplica un Buff al aliado más fuerte (o a sí mismo si no hay aliados).
    /// </summary>
    public class AccionBuffearAliado : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            var objetivo = AliadoMasFuerte() ?? (IEntidadCombate)enemigo;
            if (objetivo == null) return EstadoNodo.Fallo;

            var habilidad = SeleccionarHabilidadSoporte(objetivo,
                CategoriaHabilidad.Buff,
                CategoriaHabilidad.Curacion);

            if (habilidad == null) return EstadoNodo.Fallo;

            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo   = objetivo,
                Habilidad  = habilidad,
                TipoAccion = ResultadoIA.TipoAccionIA.Buff
            };
            return EstadoNodo.Exito;
        }
    }

    /// <summary>
    /// Se cura a sí mismo. Mantiene compatibilidad con árboles existentes.
    /// </summary>
    public class AccionCurarse : NodoIA
    {
        public override EstadoNodo Evaluar()
        {
            if (enemigo == null) return EstadoNodo.Fallo;

            var habilidad = SeleccionarHabilidadSoporte(enemigo,
                CategoriaHabilidad.Curacion,
                CategoriaHabilidad.Buff);

            if (habilidad == null) return EstadoNodo.Fallo;

            ContextoIA.UltimoResultado = new ResultadoIA
            {
                Objetivo   = enemigo,
                Habilidad  = habilidad,
                TipoAccion = ResultadoIA.TipoAccionIA.Curar
            };
            return EstadoNodo.Exito;
        }
    }
}

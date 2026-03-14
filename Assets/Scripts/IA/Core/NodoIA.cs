using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Padres;
using Flags;

namespace IA
{
    /// <summary>
    /// Clase base para nodos del árbol de comportamiento de IA.
    /// </summary>
    public abstract class NodoIA
    {
        public enum EstadoNodo { Exito, Fallo, Ejecutando }
        
        protected Enemigos enemigo;
        protected List<IEntidadCombate> jugadores;
        protected List<IEntidadCombate> aliados;
        
        public void Configurar(Enemigos enemigo, List<IEntidadCombate> jugadores, List<IEntidadCombate> aliados)
        {
            this.enemigo = enemigo;
            this.jugadores = jugadores;
            this.aliados = aliados;
        }
        
        public abstract EstadoNodo Evaluar();
        public virtual void Resetear() { }

        // ---------------------------------------------------------------
        // Helpers de selección de habilidades (disponibles en todos los nodos)
        // ---------------------------------------------------------------

        /// <summary>
        /// Busca la primera habilidad disponible de alguna de las categorías indicadas
        /// contra el objetivo dado. Respeta cooldowns y EsViable.
        /// Usa HabilidadPorDefecto como fallback si no hay ninguna del tipo correcto.
        /// </summary>
        protected HabilidadData SeleccionarHabilidadOfensiva(
            IEntidadCombate objetivo,
            params CategoriaHabilidad[] prioridades)
        {
            if (enemigo?.GestorHabilidades == null)
                return enemigo?.HabilidadPorDefecto;

            var disponibles = enemigo.GestorHabilidades
                .ObtenerDisponibles(objetivo, aliados, jugadores);

            foreach (var cat in prioridades)
            {
                var h = disponibles.FirstOrDefault(x => x.categoria == cat);
                if (h != null) return h;
            }

            // Fallback: habilidad por defecto si es viable
            var def = enemigo.HabilidadPorDefecto;
            if (def != null && def.EsViable(enemigo, objetivo, aliados, jugadores))
                return def;

            return null;
        }

        /// <summary>
        /// Busca la primera habilidad de curación/buff disponible para usar sobre un aliado.
        /// Pasa el aliado como objetivo para que EsViable evalúe TargetType correcto.
        /// </summary>
        protected HabilidadData SeleccionarHabilidadSoporte(
            IEntidadCombate objetivoAliado,
            params CategoriaHabilidad[] prioridades)
        {
            if (enemigo?.GestorHabilidades == null) return null;

            // Para habilidades de soporte, los "aliados" son los mismos aliados y
            // los "enemigos" son los jugadores — misma perspectiva que en ataque.
            var disponibles = enemigo.GestorHabilidades
                .ObtenerDisponibles(objetivoAliado, aliados, jugadores);

            foreach (var cat in prioridades)
            {
                var h = disponibles.FirstOrDefault(x => x.categoria == cat);
                if (h != null) return h;
            }

            return null;
        }

        /// <summary>
        /// Devuelve el aliado vivo con menor porcentaje de vida. Null si no hay.
        /// </summary>
        protected IEntidadCombate AliadoMasHerido()
        {
            IEntidadCombate masHerido = null;
            float menorPorcentaje = float.MaxValue;
            foreach (var a in aliados)
            {
                if (!a.EstaVivo()) continue;
                float pct = (float)a.VidaActual_Entidad / a.Vida_Entidad;
                if (pct < menorPorcentaje) { menorPorcentaje = pct; masHerido = a; }
            }
            // También considerar al propio enemigo como aliado
            if (enemigo != null && enemigo.EstaVivo())
            {
                float selfPct = (float)enemigo.VidaActual_Entidad / enemigo.Vida_Entidad;
                if (selfPct < menorPorcentaje) masHerido = enemigo;
            }
            return masHerido;
        }

        /// <summary>
        /// Devuelve el aliado vivo con MÁS vida (para buffear al tanque).
        /// </summary>
        protected IEntidadCombate AliadoMasFuerte()
        {
            IEntidadCombate masFuerte = null;
            int mayorVida = -1;
            foreach (var a in aliados)
            {
                if (!a.EstaVivo()) continue;
                if (a.VidaActual_Entidad > mayorVida) { mayorVida = a.VidaActual_Entidad; masFuerte = a; }
            }
            return masFuerte;
        }
    }
    
    /// <summary>
    /// Resultado de una decisión de IA.
    /// </summary>
    public class ResultadoIA
    {
        public IEntidadCombate Objetivo { get; set; }
        public HabilidadData Habilidad { get; set; }
        public TipoAccionIA TipoAccion { get; set; }
        
        public enum TipoAccionIA { Atacar, Defender, Huir, Curar, Buff, Debuff, Control, Especial }
    }
    
    /// <summary>
    /// Contexto compartido para nodos de IA.
    /// </summary>
    public static class ContextoIA
    {
        public static ResultadoIA UltimoResultado { get; set; }
    }
}

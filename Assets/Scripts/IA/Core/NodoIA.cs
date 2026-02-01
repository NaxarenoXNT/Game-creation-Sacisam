using System.Collections.Generic;
using Interfaces;
using Padres;

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
    }
    
    /// <summary>
    /// Resultado de una decision de IA.
    /// </summary>
    public class ResultadoIA
    {
        public IEntidadCombate Objetivo { get; set; }
        public HabilidadData Habilidad { get; set; }
        public TipoAccionIA TipoAccion { get; set; }
        
        public enum TipoAccionIA { Atacar, Defender, Huir, Curar, Especial }
    }
    
    /// <summary>
    /// Contexto compartido para nodos de IA.
    /// </summary>
    public static class ContextoIA
    {
        public static ResultadoIA UltimoResultado { get; set; }
    }
}

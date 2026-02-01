using UnityEngine;

namespace IA
{
    /// <summary>
    /// Verifica si la vida del enemigo esta por debajo de un porcentaje.
    /// </summary>
    public class CondicionVidaBaja : NodoIA
    {
        private float porcentaje;
        
        public CondicionVidaBaja(float porcentaje = 0.3f) => this.porcentaje = porcentaje;
        
        public override EstadoNodo Evaluar()
        {
            float vidaPorcentaje = (float)enemigo.VidaActual_Entidad / enemigo.Vida_Entidad;
            return vidaPorcentaje <= porcentaje ? EstadoNodo.Exito : EstadoNodo.Fallo;
        }
    }
    
    /// <summary>
    /// Verifica si hay jugadores con vida baja.
    /// </summary>
    public class CondicionJugadorDebil : NodoIA
    {
        private float porcentaje;
        
        public CondicionJugadorDebil(float porcentaje = 0.3f) => this.porcentaje = porcentaje;
        
        public override EstadoNodo Evaluar()
        {
            foreach (var jugador in jugadores)
            {
                if (!jugador.EstaVivo()) continue;
                
                float vidaPorcentaje = (float)jugador.VidaActual_Entidad / jugador.Vida_Entidad;
                if (vidaPorcentaje <= porcentaje)
                    return EstadoNodo.Exito;
            }
            return EstadoNodo.Fallo;
        }
    }
    
    /// <summary>
    /// Verifica si hay aliados caidos.
    /// </summary>
    public class CondicionAliadosCaidos : NodoIA
    {
        private int cantidadMinima;
        
        public CondicionAliadosCaidos(int cantidadMinima = 1) => this.cantidadMinima = cantidadMinima;
        
        public override EstadoNodo Evaluar()
        {
            int caidos = 0;
            foreach (var aliado in aliados)
            {
                if (!aliado.EstaVivo()) caidos++;
            }
            return caidos >= cantidadMinima ? EstadoNodo.Exito : EstadoNodo.Fallo;
        }
    }
    
    /// <summary>
    /// Ejecuta con probabilidad aleatoria.
    /// </summary>
    public class CondicionProbabilidad : NodoIA
    {
        private float probabilidad;
        
        public CondicionProbabilidad(float probabilidad = 0.5f) => this.probabilidad = probabilidad;
        
        public override EstadoNodo Evaluar()
        {
            return Random.value <= probabilidad ? EstadoNodo.Exito : EstadoNodo.Fallo;
        }
    }
}

using Padres;
using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Subclases
{
    /// <summary>
    /// Dragon: Enemigo poderoso con alto escalado. Prioriza objetivos con más vida.
    /// Tiene 30% de probabilidad de crítico (x2 daño).
    /// </summary>
    public class Dragon : Enemigos
    {
        // Escalado específico del Dragon (stats muy altos)
        private static readonly EscaladoEnemigo EscaladoDragon = new EscaladoEnemigo(
            vida: 300,
            ataque: 25,
            defensa: 15f,
            velocidad: 2
        );
        
        private const float PROBABILIDAD_CRITICO = 0.3f;
        private const int MULTIPLICADOR_CRITICO = 2;
        
        public Dragon(EnemigoData datos) 
            : base(
                datos.nombreEnemigo,
                datos.vidaBase,
                datos.ataqueBase,
                datos.defensaBase,
                datos.nivelBase,
                datos.velocidadBase,
                (int)datos.xpOtorgada,
                datos.atributos,
                datos.tipoEntidad,
                datos.estiloCombate,
                EscaladoDragon    // Pasar escalado específico
            )
        {
        }
        
        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Dragon ataca al jugador con MAS vida (mas amenazante)
            var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
            if (jugadoresVivos.Count == 0) return null;
            
            return jugadoresVivos.OrderByDescending(j => j.VidaActual_Entidad).First();
        }
        
        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            int danoBase = PuntosDeAtaque_Entidad;
            
            if (UnityEngine.Random.value < PROBABILIDAD_CRITICO)
            {
                UnityEngine.Debug.Log(Nombre_Entidad + " hace un ataque critico!");
                return danoBase * MULTIPLICADOR_CRITICO;
            }
            
            return danoBase;
        }
    }
}
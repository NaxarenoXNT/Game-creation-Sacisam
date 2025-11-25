using Padres;
using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Subclases
{
    public class Dragon : Enemigos
    {
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
                datos.estiloCombate
            )
        {
        }
        
        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Dragon ataca al jugador con MÁS vida (más amenazante)
            var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
            if (jugadoresVivos.Count == 0) return null;
            
            return jugadoresVivos.OrderByDescending(j => j.VidaActual_Entidad).First();
        }
        
        public override int CalcularDañoContra(IEntidadCombate objetivo)
        {
            // Dragon tiene chance de crítico
            int dañoBase = PuntosDeAtaque_Entidad;
            
            if (UnityEngine.Random.value < 0.3f) // 30% chance
            {
                UnityEngine.Debug.Log($"¡{Nombre_Entidad} hace un ataque crítico!");
                return dañoBase * 2;
            }
            
            return dañoBase;
        }
    }
}
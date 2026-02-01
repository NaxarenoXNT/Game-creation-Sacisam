using Padres;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Combate;

namespace Subclases
{
    /// <summary>
    /// Dragon: Enemigo poderoso con alto escalado. Prioriza objetivos con más vida.
    /// Tiene 30% de probabilidad de crítico (x2 daño) usando el sistema de CombatStats.
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
            // Inicializar habilidades y pasivas desde EnemigoData
            InicializarDesdeEnemigoData(datos);
            
            // Configurar stats de combate especiales del Dragon
            CombatStats.critChance = 0.30f;      // 30% crítico
            CombatStats.critMultiplier = 2.0f;    // x2 daño crítico
            CombatStats.elementoAtaque = datos.atributos;
        }
        
        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Dragon ataca al jugador con MAS vida (mas amenazante)
            var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
            if (jugadoresVivos.Count == 0) return null;
            
            return jugadoresVivos.OrderByDescending(j => j.VidaActual_Entidad).First();
        }
        
        // Usa el CalcularDanoContra de la clase base (Entidad) que ahora usa el sistema completo
        // El crítico se maneja automáticamente via CombatStats
    }
}
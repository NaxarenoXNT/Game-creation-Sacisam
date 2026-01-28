using Padres;
using Interfaces;

namespace Subclases
{
        public class Arquero : Jugador
    {
        // Escalado específico del Arquero (velocidad y ataque balanceados)
        private static readonly EscaladoJugador EscaladoArquero = new EscaladoJugador(
            vida: 100,
            ataque: 14,
            defensa: 4f,
            mana: 10,
            velocidad: 4
        );
        
        public Arquero(ClaseData datos)
            : base(
                datos.nombreClase,
                datos.vidaBase,
                datos.ataqueBase,
                datos.defensaBase,
                1,  // Nivel inicial
                datos.manaBase,
                datos.velocidadBase,
                datos.atributos,
                datos.tipoEntidad,
                datos.estiloCombate,
                EscaladoArquero  // Pasar escalado específico
            )
        {
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            // Arqueros hacen daño basado en ataque con bonus por velocidad
            float bonusVelocidad = 1f + (Velocidad * 0.02f);
            return (int)(PuntosDeAtaque_Entidad * bonusVelocidad);
        }
    }
}

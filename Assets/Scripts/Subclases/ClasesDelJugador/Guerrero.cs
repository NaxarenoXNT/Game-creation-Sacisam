using Padres;
using Interfaces;

namespace Subclases
{
    /// <summary>
    /// Guerrero: Clase tanque con alto crecimiento de vida y defensa.
    /// Especializado en combate cuerpo a cuerpo.
    /// </summary>
    public class Guerrero : Jugador
    {
        // Escalado específico del Guerrero (vida y defensa altas)
        private static readonly EscaladoJugador EscaladoGuerrero = new EscaladoJugador(
            vida: 150,
            ataque: 12,
            defensa: 8f,
            mana: 5,
            velocidad: 1
        );
        
        public Guerrero(ClaseData datos)
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
                EscaladoGuerrero  // Pasar escalado específico
            )
        {
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            // Guerreros hacen dano estandar
            return PuntosDeAtaque_Entidad;
        }
    }
}
using Padres;
using Interfaces;

namespace Subclases
{
    
    public class Mago : Jugador
    {
        // Escalado específico del Mago (mana y ataque altos, vida baja)
        private static readonly EscaladoJugador EscaladoMago = new EscaladoJugador(
            vida: 80,
            ataque: 15,
            defensa: 3f,
            mana: 25,
            velocidad: 2
        );
        
        public Mago(ClaseData datos)
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
                EscaladoMago  // Pasar escalado específico
            )
        {
            // Inicializar habilidades y pasivas desde ClaseData
            InicializarDesdeClaseData(datos);
        }

        public override int CalcularDanoContra(IEntidadCombate objetivo)
        {
            // Magos hacen daño basado en ataque con bonus por mana restante
            float bonusMana = ManaActual_jugador > 0 ? 1.2f : 1f;
            return (int)(PuntosDeAtaque_Entidad * bonusMana);
        }
    }
}

using Padres;
using Interfaces;

namespace Subclases
{
    /// <summary>
    /// Mago: Clase ofensiva mágica con alto mana y ataque.
    /// El escalado por nivel se configura en el ClaseData SO.
    /// </summary>
    public class Mago : Jugador
    {
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
                datos.escalado  // Escalado desde el SO
            )
        {
            // Inicializar habilidades y pasivas desde ClaseData
            InicializarDesdeClaseData(datos);
        }

        // ── Mecánica única: distribución de XP 60% personaje / 40% elementos ──
        // El Mago sube de nivel más lento porque invierte más XP en sus elementos.

        protected override float PropXPJugador   => 0.6f;
        protected override float PropXPElementos => 0.4f;
    }
}

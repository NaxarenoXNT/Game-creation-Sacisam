using Padres;
using Interfaces;

namespace Subclases
{
    /// <summary>
    /// Guerrero: Clase tanque con alto crecimiento de vida y defensa.
    /// Especializado en combate cuerpo a cuerpo.
    /// El escalado por nivel se configura en el ClaseData SO.
    /// </summary>
    public class Guerrero : Jugador
    {
        /// <summary>
        /// Bono adicional de defensa que recibe el Guerrero sobre cualquier ganancia de defensa.
        /// 0.15 = +15% sobre el valor base recibido.
        /// </summary>
        private const float BonusDefensaPorcentaje = 0.15f;

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
                datos.escalado  // Escalado desde el SO
            )
        {
            // Inicializar habilidades y pasivas desde ClaseData
            InicializarDesdeClaseData(datos);
        }

        // ── Mecánica única: +15% a toda ganancia de defensa ─────────────────

        /// <summary>
        /// Al subir de nivel, la defensa ganada se incrementa un 15% adicional.
        /// </summary>
        protected override void AplicarEscaladoNivel()
        {
            float defensaBase = escalado?.defensaPorNivel ?? 0f;

            // Aplicar el escalado normal (incluye defensaPorNivel original)
            base.AplicarEscaladoNivel();

            // Añadir el 15% encima de lo que se acaba de ganar
            float bonus = defensaBase * BonusDefensaPorcentaje;
            PuntosDeDefensa_Entidad += bonus;
        }

        /// <summary>
        /// Cualquier ganancia de defensa externa (traits, pasivas, evoluciones)
        /// se incrementa un 15%. Las pérdidas de defensa no se ven afectadas.
        /// </summary>
        public override void ModificarDefensa(float cantidad)
        {
            if (cantidad > 0f)
                base.ModificarDefensa(cantidad * (1f + BonusDefensaPorcentaje));
            else
                base.ModificarDefensa(cantidad);
        }
    }
}
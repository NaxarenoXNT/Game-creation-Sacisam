using Padres;
using Interfaces;
using Combate;

namespace Subclases
{
    /// <summary>
    /// Arquero/Asesino: Clase ágil con velocidad y ataque balanceados.
    /// Mecánica única: mientras está en sigilo, cada ataque es crítico garantizado.
    /// Sale del sigilo al atacar, a menos que el objetivo muera de ese golpe.
    /// El escalado por nivel se configura en el ClaseData SO.
    /// </summary>
    public class Arquero : Jugador
    {
        // ── Mecánica única: Sigilo ────────────────────────────────────────────

        /// <summary>Indica si el Arquero está actualmente en estado de sigilo.</summary>
        public bool EstaInvisible { get; private set; }

        /// <summary>
        /// Activa el estado de sigilo.
        /// Llamar desde habilidades, items o eventos del mundo.
        /// </summary>
        public void EntrarEnSigilo()
        {
            EstaInvisible = true;
        }

        /// <summary>
        /// Desactiva el estado de sigilo manualmente o tras un ataque sin kill.
        /// </summary>
        public void SalirDeSigilo()
        {
            EstaInvisible = false;
        }

        /// <summary>
        /// Si está en sigilo, fuerza crítico garantizado en el próximo ataque.
        /// </summary>
        public override bool ForzarCritico() => EstaInvisible;

        /// <summary>
        /// Tras realizar un ataque:
        /// - Sale del sigilo si el objetivo sobrevivió.
        /// - Permanece en sigilo si el objetivo murió del golpe.
        /// </summary>
        protected internal override void PostAtaqueConContexto(DamageContext ctx, bool objetivoMurio)
        {
            if (EstaInvisible && !objetivoMurio)
                SalirDeSigilo();
        }

        // ── Constructor ────────────────────────────────────────────────────────

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
                datos.escalado  // Escalado desde el SO
            )
        {
            // Inicializar habilidades y pasivas desde ClaseData
            InicializarDesdeClaseData(datos);
        }
    }
}

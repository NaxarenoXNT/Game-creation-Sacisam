using Padres;
using Interfaces;
using Unity.VisualScripting;

namespace Subclases
{
    public class Guerrero : Jugador
    {
        private ClaseData datosClase;
        public Guerrero(ClaseData datos)
        : base(
            datos.nombreClase,
            datos.vidaBase,
            datos.ataqueBase,
            datos.defensaBase,
            1,
            datos.manaBase,
            datos.velocidadBase,
            datos.atributos,
            datos.tipoEntidad,
            datos.estiloCombate
            )
        {
            datosClase = datos;
        }


        
        public override int CalcularDañoContra(IEntidadCombate objetivo)
        {
            int dañoBase = PuntosDeAtaque_Entidad;
            return dañoBase;
        }
        

        public override void SubirNivel()
        {
            base.SubirNivel();
            Vida_Entidad += 1000;
            PuntosDeAtaque_Entidad += 100;
            PuntosDeDefensa_Entidad += 20;
            Mana_jugador += 40;

            VidaActual_Entidad = Vida_Entidad;
            ManaActual_jugador = Mana_jugador;

            //OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);
        }
    }
}
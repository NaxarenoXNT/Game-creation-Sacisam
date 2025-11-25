using Padres;
using Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Subclases
{
    public class Goblin : Enemigos
    {
        private EnemigoData datosClase;
        
        public Goblin(EnemigoData datos)
        : base(
            datos.nombreEnemigo,
            datos.vidaBase,
            datos.ataqueBase,
            datos.defensaBase,
            1,
            datos.velocidadBase,
            (int)datos.xpOtorgada,
            datos.atributos,
            datos.tipoEntidad,
            datos.estiloCombate
            )
        {
            datosClase = datos;
        }


        

        public override IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores)
        {
            // Goblin ataca aleatoriamente
            var jugadoresVivos = jugadores.Where(j => j.EstaVivo()).ToList();
            if (jugadoresVivos.Count == 0) return null;

            int indice = UnityEngine.Random.Range(0, jugadoresVivos.Count);
            return jugadoresVivos[indice];
        }

        public override int CalcularDañoContra(IEntidadCombate objetivo)
        {
            // Goblins hacen menos daño pero atacan rápido
            return (int)(PuntosDeAtaque_Entidad * 0.8f);
        }
        public override void SubirNivel()
        {
            base.SubirNivel();
            Vida_Entidad += 500;
            PuntosDeAtaque_Entidad += 20;
            PuntosDeDefensa_Entidad += 5;

            VidaActual_Entidad = Vida_Entidad;

            //OnNivelSubido?.Invoke(Nivel_Entidad);
            //OnVidaCambiada?.Invoke(VidaActual_Entidad, Vida_Entidad);
        }
    }
}
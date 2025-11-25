using Flasgs;
using Interfaces;
using System;
using System.Collections.Generic;



namespace Padres
{
    public abstract class Enemigos : Entidad
    {
        public int XPOtorgada { get; protected set; }

        private ElementAttribute _atributos;
        private TipoEntidades _tipoDeEnemigo;
        private CombatStyle _estiloDeCombate;
        public override TipoEntidades TipoEntidad => _tipoDeEnemigo;
        public override ElementAttribute AtributosEntidad => _atributos;

        public event Action<int> OnNivelSubido;


        public Enemigos(string nombre, int vida, int ataque, float defensa, int nivel, int velocidad, int xp, ElementAttribute atributos, TipoEntidades tipoDeEnemigo, CombatStyle estiloDeCombate)
        {
            Nombre_Entidad = nombre;
            Vida_Entidad = vida;
            VidaActual_Entidad = vida;
            PuntosDeAtaque_Entidad = ataque;
            PuntosDeDefensa_Entidad = defensa;
            Nivel_Entidad = nivel;
            Velocidad = velocidad;

            XPOtorgada = xp;

            _atributos = atributos;
            _tipoDeEnemigo = tipoDeEnemigo;
            _estiloDeCombate = estiloDeCombate;

            EsDerrotado = false;
            EstaMuerto = false;
        }

        public virtual void SubirNivel()
        {
            Nivel_Entidad++;
            OnNivelSubido?.Invoke(Nivel_Entidad);
        }



        // temporales para probar
        
        public override bool EsTipoEntidad(TipoEntidades tipo)
        {
            return (_tipoDeEnemigo & tipo) == tipo;
        }
        public override bool UsaEstiloDeCombate(CombatStyle estilo)
        {
            return (_estiloDeCombate & estilo) == estilo;
        }


        public abstract IEntidadCombate DecidirObjetivo(List<IEntidadCombate> jugadores);
    }
}